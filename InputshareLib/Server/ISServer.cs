using InputshareLib.Clipboard.DataTypes;
using InputshareLib.DragDrop;
using InputshareLib.Input;
using InputshareLib.Input.Hotkeys;
using InputshareLib.Net;
using InputshareLib.Output;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using static InputshareLib.Displays.DisplayManagerBase;
namespace InputshareLib.Server
{
    public sealed class ISServer
    {

        //API events
        public event EventHandler Started;
        public event EventHandler Stopped;
        public event EventHandler<ClientInfo> ClientConnected;
        public event EventHandler<ClientInfo> ClientDisconnected;
        public event EventHandler<ClientInfo> ClientDisplayConfigChanged; // TODO
        public event EventHandler<ClientInfo> InputClientSwitched;

        public bool Running { get; private set; }
        public bool LocalInput { get; private set; }
        public ClientInfo CurrentInputClient { get => GetCurrentInputClient(); }
        public IPEndPoint BoundAddress { get; private set; }


        private readonly Displays.DisplayManagerBase displayMan;
        private readonly InputManagerBase inputMan;
        private readonly Cursor.CursorMonitorBase curMon;
        private readonly IDragDropManager dragDropMan;
        private readonly IOutputManager outMan;

        private ISClientListener clientListener;
        private ClientManager clientMan;
        /// <summary>
        /// Timer used to prevent switching back and forth between clients insantly
        /// </summary>
        private readonly Stopwatch clientSwitchTimer = new Stopwatch();

        private ISServerSocket inputClient = ISServerSocket.Localhost;

        private GlobalClipboardController cbController;
        private FileAccessController fileController;
        private GlobalDragDropControllerV2 ddControllerV2;



        public ISServer(ISServerDependencies dependencies)
        {
            displayMan = dependencies.DisplayManager;
            inputMan = dependencies.InputManager;
            curMon = dependencies.CursorMonitor;
            dragDropMan = dependencies.DragDropManager;
            outMan = dependencies.OutputManager;
        }

        #region init

        private bool firstRun = true;
        public void Start(int port)
        {
            if (Running)
                throw new InvalidOperationException("Server already running");

            clientSwitchTimer.Restart();
            fileController = new FileAccessController();
            clientMan = new ClientManager(12);
            ISLogger.Write("Starting server...");

            ddControllerV2 = new GlobalDragDropControllerV2(clientMan, dragDropMan, fileController);
            cbController = new GlobalClipboardController(clientMan, fileController, SetClipboardData);

            StartClientListener(new IPEndPoint(IPAddress.Any, port));
            StartInputManager();
            StartDisplayManager();
            StartCursorMonitor();
            AssignEvents();

            Running = true;
            clientMan.AddClient(ISServerSocket.Localhost);
            firstRun = false;
            ISLogger.Write("Inputshare server started");
            Started?.Invoke(this, null);
        }

        private void SetClipboardData(ClipboardDataBase cbData)
        {
            inputMan.SetClipboardData(cbData);
        }

        private void StartClientListener(IPEndPoint bind)
        {
            clientListener = new ISClientListener(bind.Port, bind.Address);
            BoundAddress = clientListener.BoundAddress;
            
        }

        private void StartInputManager()
        {
            inputMan.Start();

            HotkeyModifiers mods = HotkeyModifiers.Alt | HotkeyModifiers.Ctrl | HotkeyModifiers.Shift;
            inputMan.AddUpdateFunctionHotkey(new Input.Hotkeys.FunctionHotkey(WindowsVirtualKey.Q, mods, Input.Hotkeys.Hotkeyfunction.StopServer));
            inputMan.AddUpdateClientHotkey(new ClientHotkey(WindowsVirtualKey.Z, HotkeyModifiers.Shift, Guid.Empty));
            ISServerSocket.Localhost.CurrentHotkey = new ClientHotkey(WindowsVirtualKey.Z, HotkeyModifiers.Shift, Guid.Empty);
        }

        private void StartCursorMonitor()
        {
            curMon.StartMonitoring(displayMan.CurrentConfig.VirtualBounds);
        }

        public void SetMouseInputMode(MouseInputMode mode, int interval = 0)
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            inputMan.SetMouseInputMode(mode, interval);
        }

        private void StartDisplayManager()
        {
            displayMan.UpdateConfigManual();
            ISServerSocket.Localhost.DisplayConfiguration = displayMan.CurrentConfig;
            displayMan.StartMonitoring();
        }

        private void AssignEvents()
        {
            if (!firstRun)
                return;

            displayMan.DisplayConfigChanged += DisplayMan_DisplayConfigChanged;
            curMon.EdgeHit += CurMon_EdgeHit;
            inputMan.InputReceived += InputMan_InputReceived;
            inputMan.ClipboardDataChanged += cbController.OnLocalClipboardDataCopied;
            inputMan.ClientHotkeyPressed += InputMan_ClientHotkeyPressed;
            inputMan.FunctionHotkeyPressed += InputMan_FunctionHotkeyPressed;
            clientListener.ClientConnected += ClientListener_ClientConnected;
        }

        #endregion

        /// <summary>
        /// Stops the inputshare server
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the server is not running</exception>"
        public void Stop()
        {
           // if (!Running)
               // throw new InvalidOperationException("Server not running");

            ISLogger.Write("Stopping server...");


            try
            {
                foreach (var client in clientMan.AllClients)
                {
                    try
                    {
                        client.Close(NetworkSocket.CloseNotifyMode.ServerStopped);
                    }
                    catch (Exception) { }
                }

                if (clientListener != null && clientListener.Listening)
                    clientListener.Stop();

                if (inputMan.Running)
                    inputMan.Stop();

                if (displayMan.Running)
                    displayMan.StopMonitoring();

                if (curMon.Monitoring)
                    curMon.StopMonitoring();

                if (dragDropMan.Running)
                    dragDropMan.Stop();

                ISLogger.Write("server stopped.");
                
            }catch(Exception ex)
            {
                ISLogger.Write("An error occurred while stopping server: " + ex.Message);
                ISLogger.Write(ex.StackTrace);
            }
            finally
            {
                Running = false;
                Stopped?.Invoke(this, null);
            }
           
        }

        private void SwitchToClientInput(Guid targetClient)
        {
            if (clientSwitchTimer.ElapsedMilliseconds < 200)
                return;

            ISServerSocket client = clientMan.GetClientById(targetClient);

            if(client == null)
            {
                ISLogger.Write("Failed to switch to client: client not found");
                return;
            }

            ISServerSocket oldClient = inputClient;

            if (curMon.Monitoring)
                curMon.StopMonitoring();

            client.NotifyActiveClient(true);
            inputClient = client;
            inputMan.SetInputBlocked(true);

            clientSwitchTimer.Restart();
            ddControllerV2.HandleClientSwitch(inputClient, oldClient);
            InputClientSwitched?.Invoke(this, GenerateClientInfo(client));

        }

        private void SwitchToLocalInput()
        {
            if (clientSwitchTimer.ElapsedMilliseconds < 200)
                return;


            if (inputClient != null && inputClient.IsConnected)
                inputClient.NotifyActiveClient(false);

            ISServerSocket oldClient = inputClient;
            inputClient = ISServerSocket.Localhost;
            inputMan.SetInputBlocked(false);

            if (!curMon.Monitoring)
                curMon.StartMonitoring(displayMan.CurrentConfig.VirtualBounds);


            clientSwitchTimer.Restart();
            ddControllerV2.HandleClientSwitch(inputClient, oldClient);
            outMan.ResetKeyStates();
            InputClientSwitched?.Invoke(this, GenerateLocalhostInfo());
        }

        private void SetClientEdge(ISServerSocket clientA, Edge sideof, ISServerSocket clientB)
        {
            if (clientA == null || clientB == null)
                throw new ArgumentNullException("client was null");

            if(clientA.ClientName == clientB.ClientName)
            {
                throw new ArgumentException("Cannot set X sideof X");
            }

            clientB.SetClientAtEdge(sideof, clientA);
            clientA.SetClientAtEdge(sideof.Opposite(), clientB);
            ISLogger.Write("Set {0} {1}of {2}", clientA.ClientName, sideof, clientB.ClientName);
            clientA.SendClientEdgesUpdate();
            clientB.SendClientEdgesUpdate();
        }

        #region localhost events

        private void InputMan_InputReceived(object sender, ISInputData input)
        {
            if(inputClient != ISServerSocket.Localhost)
            {
                if (inputClient.IsConnected)
                {
                    inputClient.SendInputData(input.ToBytes());
                }
            }
        }

        private void CurMon_EdgeHit(object sender, Edge e)
        {
            if (inputClient != ISServerSocket.Localhost)
                return;

            ISServerSocket target = ISServerSocket.Localhost.GetClientAtEdge(e);

            if (target == null || !target.IsConnected)
            {
                ISServerSocket.Localhost.SetClientAtEdge(e, null);
                return;
            }
            
            if (target != null && target.IsConnected)
                SwitchToClientInput(target.ClientId);
            else
                ISServerSocket.Localhost.SetClientAtEdge(e, null);

        }

        private void InputMan_FunctionHotkeyPressed(object sender, Input.Hotkeys.Hotkeyfunction e)
        {
            if (e == Hotkeyfunction.StopServer)
            {
                Stop();
            }
        }

        private void InputMan_ClientHotkeyPressed(object sender, Guid targetClient)
        {
            if (targetClient == Guid.Empty)
                SwitchToLocalInput();
            else
                SwitchToClientInput(targetClient);
        }

        private void DisplayMan_DisplayConfigChanged(object sender, DisplayConfig conf)
        {
            ISServerSocket.Localhost.DisplayConfiguration = conf;
        }


        #endregion

        #region client events

        /// <summary>
        /// Fired by the ISClientListener when a client has connected and sent initial info.
        /// We call client.AcceptClient() here to let the client know that it has properly connected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClientListener_ClientConnected(object sender, ISClientListener.ClientConnectedArgs e)
        {
            ISServerSocket client = e.Socket;

            try
            {
                clientMan.AddClient(client);
            }
            catch (ClientManager.DuplicateGuidException)
            {
                ISLogger.Write("client {0} declined: {1}", client.ClientName, "Duplicate GUID");
                client.DeclineClient(ISServerSocket.ClientDeclinedReason.DuplicateGuid);
                client.Dispose();
                return;
            }
            catch (ClientManager.DuplicateNameException)
            {
                ISLogger.Write("client {0} declined: {1}", client.ClientName, "Duplicate name");
                client.DeclineClient(ISServerSocket.ClientDeclinedReason.DuplicateName);
                client.Dispose();
                return;
            }
            catch (ClientManager.ClientLimitException)
            {
                ISLogger.Write("client {0} declined: {1}", client.ClientName, "client limit reached");
                client.DeclineClient(ISServerSocket.ClientDeclinedReason.MaxClientsReached);
                client.Dispose();
                return;
            }

            ISLogger.Write("{1} connected as {0}", e.ClientName, client.ClientEndpoint);
            CreateClientEventHandlers(client);
            client.DisplayConfiguration = new DisplayConfig(e.DisplayConfig);
            client.AcceptClient();
            ClientConnected?.Invoke(this, GenerateClientInfo(client));
        }

        private void CreateClientEventHandlers(ISServerSocket socket)
        {
            socket.ClientDisplayConfigUpdated += Client_ClientDisplayConfigUpdated;
            socket.ConnectionError += Client_ConnectionError;
            socket.ClipboardDataReceived += cbController.OnClientClipboardDataReceived;
            socket.EdgeHit += Socket_EdgeHit;
            socket.RequestedStreamRead += Socket_RequestedStreamRead;
            socket.RequestedCloseStream += Socket_RequestedCloseStream;
            socket.RequestedFileToken += Socket_RequestedFileToken;
            socket.DragDropDataReceived += ddControllerV2.Client_DataDropped;
            socket.DragDropCancelled += ddControllerV2.Client_DragDropCancelled;
            socket.DragDropSuccess += ddControllerV2.Client_DragDropSuccess;
            socket.DragDropOperationComplete += ddControllerV2.Client_DragDropComplete;
        }

        private void Socket_RequestedStreamRead(object sender, NetworkSocket.RequestStreamReadArgs args)
        {
            ISServerSocket client = sender as ISServerSocket;

            try
            {

                //We need to check if this token is associated with the drag drop operation file token.
                //If it is, and localhost is not the host of the operation, then we need to request the data from the host
                if (args.Token == ddControllerV2.currentOperation?.RemoteFileAccessToken)
                {
                    if(ddControllerV2.currentOperation?.ReceiverClient != client)
                    {
                        ISLogger.Write("Client {0} attempted to access dragdrop operation files when they are not the drop target", client.ClientName);
                        client.SendStreamReadErrorResponse(args.NetworkMessageId, "Data has been dropped by another client");
                        return;
                    }


                    //if localhost is not the host of the dragdrop operation, we need to get data from whichever client has the files
                    if (!ddControllerV2.currentOperation.Host.IsLocalhost)
                    {
                        ReplyWithExternalFileData(client, args.NetworkMessageId , args.Token, args.File, args.ReadLen);
                        return;
                    }
                }else if(args.Token == cbController.currentOperation?.HostFileAccessToken)
                {
                    if(cbController.currentOperation?.DataType == ClipboardDataType.File && !cbController.currentOperation.Host.IsLocalhost)
                    {
                        ISLogger.Write("{0} request clipboard file data", client.ClientName);
                        ReplyWithExternalFileData(client, args.NetworkMessageId, args.Token, args.File, args.ReadLen);
                        return;
                    }
                }

                byte[] data = new byte[args.ReadLen];
                int readLen = fileController.ReadStream(args.Token, args.File, data, 0, args.ReadLen);
                //resize the buffer so we don't send a buffer that ends with empty data.
                byte[] resizedBuffer = new byte[readLen];
                Buffer.BlockCopy(data, 0, resizedBuffer, 0, readLen);
                client.SendReadRequestResponse(args.NetworkMessageId, resizedBuffer);
            }
            catch(Exception ex)
            {
                client.SendStreamReadErrorResponse(args.NetworkMessageId, ex.Message);
            }
        }


        private async void ReplyWithExternalFileData(ISServerSocket client, Guid networkMessageId, Guid token, Guid fileId, int readLen)
        {
            try
            {
                byte[] fileData = await ddControllerV2.currentOperation.Host.RequestReadStreamAsync(token, fileId, readLen);
                client.SendReadRequestResponse(networkMessageId, fileData);
            }catch(Exception ex)
            {
                //let the client know that the read failed
                client.SendStreamReadErrorResponse(networkMessageId, ex.Message);
            }
        }

        private void Socket_RequestedCloseStream(object sender, NetworkSocket.RequestCloseStreamArgs args)
        {
            fileController.CloseStream(args.Token, args.File);
        }

        private void Socket_RequestedFileToken(object sender, NetworkSocket.FileTokenRequestArgs args)
        {
            ISServerSocket client = sender as ISServerSocket;

            if(args.FileGroupId == ddControllerV2.currentOperation.OperationId)
            {
                if(ddControllerV2.currentOperation != null && ddControllerV2.currentOperation.Host != null && ddControllerV2.currentOperation.Host.IsLocalhost)
                {
                    ISLogger.Write("{0} requested access to local stored dragdrop files", client.ClientName);
                    client.SendTokenRequestReponse(args.NetworkMessageId, ddControllerV2.currentOperation.RemoteFileAccessToken);
                    ISLogger.Write("Sent {0} access token {1}", client.ClientName, ddControllerV2.currentOperation.RemoteFileAccessToken);
                    return;
                }else if(ddControllerV2.currentOperation != null && ddControllerV2.currentOperation.Host != null && !ddControllerV2.currentOperation.Host.IsLocalhost)
                {
                    ISLogger.Write("{0} requested access to external stored dragdrop files", client.ClientName);
                    client.SendTokenRequestReponse(args.NetworkMessageId, ddControllerV2.currentOperation.RemoteFileAccessToken);
                    ISLogger.Write("Sent {0} access token {1}", client.ClientName, ddControllerV2.currentOperation.RemoteFileAccessToken);
                } 
            }
            else
            {
                ISLogger.Write("Denied {0} request to access files that are not part of an operation");
            }
        }

        private void Socket_EdgeHit(object sender, Edge edge)
        {
            ISServerSocket client = sender as ISServerSocket;

            if (client != inputClient)
                return;

            ISServerSocket target = client.GetClientAtEdge(edge);

            if (target == ISServerSocket.Localhost)
            {
                SwitchToLocalInput();
            }
            else if (target != null && target.IsConnected)
            {
                SwitchToClientInput(target.ClientId);
            }
            else
            {
                client.SetClientAtEdge(edge, null);
            }
        }

        private void Client_ConnectionError(object sender, string error)
        {
            ISServerSocket client = sender as ISServerSocket;

            if (client == inputClient)
                SwitchToLocalInput();

            try
            {
                clientMan.RemoveClient(client);
            }
            catch (Exception) { }

            
            ISLogger.Write("Connection error on {0}: {1}", client.ClientName, error);

            if (inputMan.GetClientHotkey(client.ClientId) != null)
                inputMan.RemoveClientHotkey(client.ClientId);
            RemoveClientFromEdges(client);
            ClientDisconnected?.Invoke(this, GenerateClientInfo(client));
        }

        private void RemoveClientFromEdges(ISServerSocket client)
        {
            foreach(var c in clientMan.AllClients.ToArray())
            {
                if (c == client)
                    continue;

                if (c.RightClient == client)
                    c.RightClient = null;
                if (c.LeftClient == client)
                    c.LeftClient = null;
                if (c.TopClient == client)
                    c.TopClient = null;
                if (c.BottomClient == client)
                    c.BottomClient = null;
            }
        }

        private void RemoveEdgeFromClient(ISServerSocket client, Edge edge)
        {
            switch (edge)
            {
                case Edge.Bottom:
                    client.BottomClient = null;
                    break;
                case Edge.Top:
                    client.TopClient = null;
                    break;
                case Edge.Left:
                    client.LeftClient = null;
                    break;
                case Edge.Right:
                    client.RightClient = null;
                    break;

            }
        }

        private void Client_ClientDisplayConfigUpdated(object sender, DisplayConfig config)
        {
            ISServerSocket client = sender as ISServerSocket;
            ISLogger.Write("Display config changed for {0}", client.ClientName);
        }

        #endregion

        #region API
        public ClientInfo[] GetAllClients()
        {
            ClientInfo[] info = new ClientInfo[clientMan.ClientCount];
            int index = 0;
            foreach(var client in clientMan.AllClients) {
                info[index] = GenerateClientInfo(client, true);
                index++;
            }
            return info;
        }

        /// <summary>
        /// Sets a client to the edge of another client
        /// </summary>
        /// <param name="clientA"></param>
        /// <param name="sideOf"></param>
        /// <param name="clientB"></param>
        /// <exception cref="ArgumentException">The client cannot be found</exception>
        public void SetClientEdge(ClientInfo clientA, Edge sideOf, ClientInfo clientB)
        {
            ISServerSocket cA = clientMan.GetClientFromInfo(clientA);
            ISServerSocket cB = clientMan.GetClientFromInfo(clientB);

            if (cA == null)
                throw new ArgumentException("Invalid clientA");
            if (cB == null)
                throw new ArgumentException("Invalid clientB");

            SetClientEdge(cA, sideOf, cB);
        }
        public void RemoveClientEdge(ClientInfo client, Edge side)
        {
            ISServerSocket cA = clientMan.GetClientFromInfo(client);
            if (cA == null)
                throw new ArgumentException("Invalid client");

            RemoveEdgeFromClient(cA, side);
        }

        public Hotkey GetHotkeyForFunction(Hotkeyfunction function)
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            return inputMan.GetFunctionHotkey(function);
        }

        public Hotkey GetHotkeyForClient(ClientInfo client)
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            return inputMan.GetClientHotkey(client.Id);
        }

        public void SetHotkeyForClient(ClientInfo client, Hotkey key)
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            inputMan.AddUpdateClientHotkey(new ClientHotkey(key.Key, key.Modifiers, client.Id));

            clientMan.GetClientById(client.Id).CurrentHotkey = key;

        }

        public void SetHotkeyForFunction(Hotkey key, Hotkeyfunction function)
        {
            if (!Running)
                throw new InvalidOperationException("Server not running");

            inputMan.AddUpdateFunctionHotkey(new FunctionHotkey(key.Key, key.Modifiers, function));
        }

        private ClientInfo GenerateClientInfo(ISServerSocket client, bool includeEdges = false)
        {
            if(client == ISServerSocket.Localhost)
            {
                return GenerateLocalhostInfo();
            }

            ClientHotkey hk = inputMan.GetClientHotkey(client.ClientId);
            DisplayConfig dConf = client.DisplayConfiguration;

            ClientInfo info = new ClientInfo(client.ClientName, client.ClientId, dConf, hk, client.ClientEndpoint);
            info.InputClient = inputClient == client;

            if (includeEdges)
            {
                AssignClientEdges(info, client);
            }

            return info;
        }
        
        private ClientInfo GenerateLocalhostInfo()
        {
            ISServerSocket local = ISServerSocket.Localhost;
            ClientInfo info = new ClientInfo(local.ClientName, local.ClientId, local.DisplayConfiguration,
                inputMan.GetClientHotkey(local.ClientId), new IPEndPoint(IPAddress.Parse("127.0.0.1"), BoundAddress.Port));

            info.InputClient = inputClient == local;

            AssignClientEdges(info, local);
            return info;
        }

        private void AssignClientEdges(ClientInfo info, ISServerSocket client)
        {
            if (client.BottomClient != null)
                info.BottomClient = GenerateClientInfo(client.BottomClient);
            if (client.TopClient != null)
                info.TopClient = GenerateClientInfo(client.TopClient);
            if (client.RightClient != null)
                info.RightClient = GenerateClientInfo(client.RightClient);
            if (client.LeftClient != null)
                info.LeftClient = GenerateClientInfo(client.LeftClient);
        }

        private ClientInfo GetCurrentInputClient()
        {
            return GenerateClientInfo(inputClient, true);
        }

        #endregion
    }
}
