using InputshareLib.Clipboard;
using InputshareLib.Net;
using InputshareLib.Output;
using System;
using System.Net;
using InputshareLib.Clipboard.DataTypes;
using InputshareLib.DragDrop;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InputshareLib.Client
{
    public sealed class ISClient
    {
        private struct ClientEdges
        {
            public bool Left;
            public bool Right;
            public bool Top;
            public bool Bottom;
        }

        public bool IsConnected
        {
            get
            {
                if (socket == null)
                    return false;
                else
                    return socket.IsConnected;
            }
        }

        public bool ActiveClient { get; protected set; }
        public IPEndPoint ServerAddress { get => socket.ServerAddress; }

        public event EventHandler<bool> ActiveClientChanged;
        public event EventHandler<IPEndPoint> Connected;
        public event EventHandler<string> ConnectionFailed;
        public event EventHandler<string> ConnectionError;
        public event EventHandler Disconnected;
        public event EventHandler ClipboardDataCopied;

        private ClientEdges edges;
        private readonly IOutputManager outMan;
        private readonly ClipboardManagerBase clipboardMan;
        private  ISClientSocket socket;
        private readonly Displays.DisplayManagerBase displayMan;
        private readonly Cursor.CursorMonitorBase curMon;
        private readonly IDragDropManager dragDropMan;

        public string ClientName { get; private set; } = Environment.MachineName;
        public Guid ClientId { get; private set; } = Guid.NewGuid();

        struct DataOperation
        {
            public DataOperation(Guid operationId, ClipboardDataBase data)
            {
                OperationId = operationId;
                Data = data;
                AssociatedAccessTokens = new List<Guid>();
                Completed = false;
            }
            public Guid OperationId { get; }
            public ClipboardDataBase Data { get; }

            public bool Completed { get; set; } 

            public List<Guid> AssociatedAccessTokens { get; set; }
        }

        private DataOperation currentClipboardOperation = new DataOperation();
        private DataOperation currentDragDropOperation = new DataOperation();

        private Dictionary<Guid, DataOperation> previousOperations = new Dictionary<Guid, DataOperation>();

        private FileAccessController fileController = new FileAccessController();

        public ISClient(ClientDependencies dependencies)
        {
            displayMan = dependencies.displayManager;
            curMon = dependencies.cursorMonitor;
            outMan = dependencies.outputManager;
            clipboardMan = dependencies.clipboardManager;
            dragDropMan = dependencies.dragDropManager;
            Init();
        }

        public void Stop()
        {
            if (displayMan.Running)
                displayMan.StopMonitoring();
            if (curMon.Monitoring)
                curMon.StopMonitoring();
            if (clipboardMan.Running)
                clipboardMan.Stop();

            socket?.Close();
        }

        public void Disconnect()
        {
            if (!IsConnected)
                throw new InvalidOperationException("not connected");

            socket.Disconnect(true);
            Disconnected?.Invoke(this, null);
        }

        private void Init()
        {
            displayMan.StartMonitoring();
            displayMan.DisplayConfigChanged += OnLocalDisplayConfigChange;
            displayMan.UpdateConfigManual();
            curMon.EdgeHit += OnLocalEdgeHit;
            clipboardMan.Start();
            clipboardMan.ClipboardContentChanged += OnLocalClipboardChange;
            dragDropMan.Start();
            dragDropMan.DataDropped += DragDropMan_DataDropped;
            dragDropMan.DragDropSuccess += DragDropMan_DragDropSuccess;
            dragDropMan.DragDropCancelled += DragDropMan_DragDropCancelled;
            dragDropMan.DragDropComplete += DragDropMan_DragDropComplete;
        }

        private void DragDropMan_DragDropComplete(object sender, Guid operationId)
        {
            socket.SendDragDropComplete(operationId);
        }

        private void DragDropMan_DragDropCancelled(object sender, EventArgs e)
        {
            socket.NotifyDragDropSuccess(currentDragDropOperation.OperationId, false);
        }

        private void DragDropMan_DragDropSuccess(object sender, EventArgs e)
        {
            ISLogger.Write("Sending dragdrop success!");
            socket.NotifyDragDropSuccess(currentDragDropOperation.OperationId, true);
        }

        private void DragDropMan_DataDropped(object sender, ClipboardDataBase data)
        {
            ISLogger.Write("DragDropMan_DataDropped");
            if (!previousOperations.ContainsKey(currentDragDropOperation.OperationId))
            {
                previousOperations.Add(currentDragDropOperation.OperationId, currentDragDropOperation);
            }

            ISLogger.Write("object dropped");
            if (socket.IsConnected)
            {
                Guid opId = Guid.NewGuid();
                currentDragDropOperation = new DataOperation(opId, data);
                ISLogger.Write("Started dragdrop operation " + opId);
                socket.SendDragDropData(data.ToBytes(), opId);
            }
        }


        private void OnLocalClipboardChange(object sender, ClipboardDataBase data)
        {
            if (socket.IsConnected)
            {
                //create GUID and file tokens
                Guid operationId = Guid.NewGuid();
                if (currentDragDropOperation.OperationId != Guid.Empty)
                    previousOperations.Add(currentClipboardOperation.OperationId, currentClipboardOperation);

                currentClipboardOperation = new DataOperation(operationId, data);
                socket.SendClipboardData(data.ToBytes(), operationId);
                ISLogger.Write("Created clipboard operation " + operationId);
            }
        }

        private void OnLocalEdgeHit(object sender, Edge edge)
        {
            if (socket.IsConnected)
            {
                socket.SendEdgeHit(edge);

                if (dragDropMan.LeftMouseState)
                {
                    switch (edge)
                    {
                        case Edge.Bottom: if (edges.Bottom) dragDropMan.CheckForDrop(); break;
                        case Edge.Left: if (edges.Left) dragDropMan.CheckForDrop(); break;
                        case Edge.Right: if (edges.Right) dragDropMan.CheckForDrop(); break;
                        case Edge.Top: if (edges.Top) dragDropMan.CheckForDrop(); break;
                    }
                }
                
            }
                
        }

        private void OnLocalDisplayConfigChange(object sender, Displays.DisplayManagerBase.DisplayConfig config)
        {
            if (socket.IsConnected)
            {
                socket.SendDisplayConfig(config.ToBytes());
            }
        }

        public void Connect(string address, int port, string name, Guid id = new Guid())
        {
            if (!IPAddress.TryParse(address, out _))
                throw new ArgumentException("Invalid address");

            if (port < 0 || port > 65535)
                throw new ArgumentException("Invalid port");

            if (socket != null)
                socket.Close();

            if (id == Guid.Empty)
                id = Guid.NewGuid();

            socket = new ISClientSocket();
            CreateSocketEvents();
            ClientName = name;
            ClientId = id;
            socket.Connect(address, port, new ISClientSocket.ConnectionInfo(ClientName, ClientId, displayMan.CurrentConfig.ToBytes()));
        }

        private void CreateSocketEvents()
        {
            socket.ClipboardDataReceived += OnClipboardDataReceived;
            socket.Connected += OnConnected;
            socket.ConnectionError += OnConnectionError;
            socket.ConnectionFailed += OnConnectionFailed;
            socket.InputDataReceived += OnInputReceived;
            socket.ActiveClientChanged += OnActiveClientChange;
            socket.DragDropDataReceived += Socket_DragDropDataReceived;
            socket.EdgesChanged += Socket_EdgesChanged;
            socket.DragDropCancelled += Socket_DragDropCancelled;
            socket.RequestedFileToken += Socket_FileTokenRequested;
            socket.RequestedStreamRead += Socket_RequestStreamRead;
            socket.RequestedCloseStream += Socket_RequestedCloseStream;
            socket.CancelAnyDragDrop += Socket_CancelAnyDragDrop;
            socket.DragDropOperationComplete += Socket_DragDropOperationComplete;
        }

        private void Socket_DragDropOperationComplete(object sender, Guid operationId)
        {
            if(operationId == currentDragDropOperation.OperationId)
            {
                //Dragdrop complete can be returned multiple times by clients, so we only need to do something if the
                //operation is not marked as complete yet
                if (currentDragDropOperation.Completed)
                    return;

                ISLogger.Write("Server marked dragdrop operation as complete... closing streams");

                currentDragDropOperation.Completed = true;

                foreach(var id in currentDragDropOperation.AssociatedAccessTokens)
                {
                    fileController.DeleteToken(id);
                }

                //Store it as a previous operation if not already stored
                if (!previousOperations.ContainsKey(currentDragDropOperation.OperationId))
                    previousOperations.Add(currentDragDropOperation.OperationId, currentDragDropOperation);
            }
            else
            {
                if (previousOperations.ContainsKey(operationId))
                {
                    if(previousOperations.TryGetValue(operationId, out DataOperation operation)){
                        ISLogger.Write("Server marked previous dragdrop operation complete... closing streams");

                        foreach (var id in operation.AssociatedAccessTokens)
                        {
                            fileController.DeleteToken(id);
                        }

                        operation.Completed = true;
                    }
                }
                else
                {
                    ISLogger.Write("Failed to mark dragdrop operation as complete: Could not find operation ID");
                }
            }
        }

        private void Socket_CancelAnyDragDrop(object sender, EventArgs e)
        {
            dragDropMan.CancelDrop();
        }

        private void Socket_RequestedCloseStream(object sender, NetworkSocket.RequestCloseStreamArgs e)
        {
            fileController.CloseStream(e.Token, e.File);
        }


        private void Socket_RequestStreamRead(object sender, NetworkSocket.RequestStreamReadArgs e)
        {
            try
            {
                byte[] data = new byte[e.ReadLen];
                int readLen = fileController.ReadStream(e.Token, e.File, data, 0, e.ReadLen);
                //resize the buffer so we don't send a buffer that ends with empty data.
                byte[] resizedBuffer = new byte[readLen];
                Buffer.BlockCopy(data, 0, resizedBuffer, 0, readLen);
                socket.SendReadRequestResponse(e.NetworkMessageId, resizedBuffer);
            }
            catch(FileAccessController.TokenNotFoundException)
            {
                ISLogger.Write("Token not found");
                socket.SendStreamReadErrorResponse(e.NetworkMessageId, "Token not found");
            }catch(Exception ex)
            {
                ISLogger.Write("Responding with: Read error - " + ex.Message);
                socket.SendStreamReadErrorResponse(e.NetworkMessageId, ex.Message);
            }
        }

        private void Socket_FileTokenRequested(object sender, NetworkSocket.FileTokenRequestArgs args)
        {
            if(args.FileGroupId == Guid.Empty)
            {
                ISLogger.Write("Debug: server requested access to a blank file group ID");
                return;
            }

            DataOperation operation;
            if(args.FileGroupId == currentClipboardOperation.OperationId)
                operation = currentClipboardOperation;
            else if(args.FileGroupId == currentDragDropOperation.OperationId)
                operation = currentDragDropOperation;
            else
                //TODO - return error to client
                return;

            try
            {
                Guid token = CreateTokensForOperation(operation);

                //we need to keep track of which tokens are assoicated with which transfer 
                currentDragDropOperation.AssociatedAccessTokens.Add(token);
                ISLogger.Write("added associated access token " + token);
                socket.SendTokenRequestReponse(args.NetworkMessageId, token);
            }catch(Exception ex)
            {
                ISLogger.Write("Failed to create access token for operation: " + ex.Message);
                //TODO - respond with error
                return;
            }
            
        }

        private Guid CreateTokensForOperation(DataOperation operation)
        {
            ClipboardVirtualFileData fd = operation.Data as ClipboardVirtualFileData;

            Guid[] ids = new Guid[fd.AllFiles.Count];
            string[] sources = new string[fd.AllFiles.Count];
            for (int i = 0; i < fd.AllFiles.Count; i++)
            {
                ids[i] = fd.AllFiles[i].FileRequestId;
                sources[i] = fd.AllFiles[i].FullPath;
            }
            return fileController.CreateFileReadTokenForGroup(new FileAccessController.FileAccessInfo(ids, sources));
        }

        private void Socket_DragDropCancelled(object sender, Guid _)
        {
            if (dragDropMan.Running)
                dragDropMan.CancelDrop();
        }

        private void Socket_EdgesChanged(object sender, ISClientSocket.BoundEdges e)
        {
            edges.Bottom = e.Bottom;
            edges.Left = e.Left;
            edges.Right = e.Right;
            edges.Top = e.Top;
        }

        private void Socket_DragDropDataReceived(object sender, NetworkSocket.DragDropDataReceivedArgs args)
        {
            BeginDragDropOperation(args);
        }

        private async void BeginDragDropOperation(NetworkSocket.DragDropDataReceivedArgs args)
        {
            if (!previousOperations.ContainsKey(currentDragDropOperation.OperationId))
            {
                previousOperations.Add(currentDragDropOperation.OperationId, currentDragDropOperation);
            }

            if (currentDragDropOperation.OperationId == args.OperationId)
            {
                ISLogger.Write("Re-performing previous dragdrop operation");
                dragDropMan.DoDragDrop(currentDragDropOperation.Data);
                outMan.ResetKeyStates();
                return;
            }

            ClipboardDataBase data = ClipboardDataBase.FromBytes(args.RawData);
            ISLogger.Write("Received dragdrop operation " + args.OperationId);
            currentDragDropOperation = new DataOperation(args.OperationId, data);

            if (data.DataType == ClipboardDataType.File)
            {
                Guid token = await socket.RequestFileTokenAsync(args.OperationId);
                ClipboardVirtualFileData fd = data as ClipboardVirtualFileData;
                foreach (var file in fd.AllFiles)
                {

                    file.RemoteAccessToken = token;
                    file.ReadDelegate = File_RequestDataAsync;
                    file.ReadComplete += File_ReadComplete;
                    file.FileOperationId = args.OperationId;
                }

            }

            dragDropMan.DoDragDrop(data);
        }
        private void File_ReadComplete(object sender, EventArgs e)
        {
            ClipboardVirtualFileData.FileAttributes file = sender as ClipboardVirtualFileData.FileAttributes;
            socket.RequestCloseStream(file.RemoteAccessToken, file.FileRequestId);
        }

        private async Task<byte[]> File_RequestDataAsync(Guid token, Guid fileId, int readLen)
        {
            return await socket.RequestReadStreamAsync(token, fileId, readLen);
        }

        private void OnActiveClientChange(object sender, bool active)
        {
            ActiveClient = active;
            if (active)
            {
                if (!curMon.Monitoring)
                    curMon.StartMonitoring(displayMan.CurrentConfig.VirtualBounds);

                outMan.ResetKeyStates();
            }
            else
            {
                if (curMon.Monitoring)
                    curMon.StopMonitoring();

                outMan.ResetKeyStates();
            }
            ActiveClientChanged?.Invoke(this, ActiveClient);
        }

        private void OnInputReceived(object sender, byte[] data)
        {
            outMan.Send(new Input.ISInputData(data));
        }

        private void OnConnectionFailed(object sender, string reason)
        {
            ISLogger.Write("Connection failed: " + reason);
            ConnectionFailed?.Invoke(this, reason);
        }

        private void OnConnectionError(object sender, string reason)
        {
            if (curMon.Monitoring)
                curMon.StopMonitoring();

            ISLogger.Write("Connection error: " + reason);
            ConnectionError?.Invoke(this, reason);
            socket.Dispose();
        }

        private void OnConnected(object sender, EventArgs e)
        {
            ISLogger.Write("Connected");
            Connected?.Invoke(this, socket.ServerAddress);
        }

        private void OnClipboardDataReceived(object sender, NetworkSocket.ClipboardDataReceivedArgs args)
        {
            try
            {
                ISLogger.Write("Got clipboard operation " + args.OperationId);
                ClipboardDataBase cbData = ClipboardDataBase.FromBytes(args.rawData);
                currentClipboardOperation = new DataOperation(args.OperationId, cbData);
                clipboardMan.SetClipboardData(cbData);
            }
            catch(Exception ex)
            {
                ISLogger.Write("Failed to set clipboard data: " + ex.Message);
            }

            ClipboardDataCopied?.Invoke(this, null);
        }
    }
}
