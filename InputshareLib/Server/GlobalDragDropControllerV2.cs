using InputshareLib.Clipboard.DataTypes;
using InputshareLib.DragDrop;
using InputshareLib.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib.Server
{

    /// <summary>
    /// Controls global drag/drop operations between clients/server
    /// </summary>
    class GlobalDragDropControllerV2
    {
        private ClientManager clientMan;
        private IDragDropManager ddManager;
        private FileAccessController fileController;

        private Dictionary<Guid, DragDropOperation> previousOperationIds = new Dictionary<Guid, DragDropOperation>();
        public DragDropOperation currentOperation { get; private set; }
        private ISServerSocket currentInputClient = ISServerSocket.Localhost;
        public GlobalDragDropControllerV2(ClientManager clientManager, IDragDropManager dropManager, FileAccessController fileAccessController)
        {
            clientMan = clientManager;
            fileController = fileAccessController;
            ddManager = dropManager;

            if (!ddManager.Running)
                ddManager.Start();

            ddManager.DataDropped += DdManager_DataDropped;
            ddManager.DragDropCancelled += DdManager_DragDropCancelled;
            ddManager.DragDropComplete += DdManager_DragDropComplete;
            ddManager.DragDropSuccess += DdManager_DragDropSuccess;
        }

        private void DdManager_DragDropSuccess(object sender, EventArgs e)
        {
            ISLogger.Write("DragDropController: Localhost drop success");
            //TODO - should this just be current operation ID?
            OnDropSuccess(ISServerSocket.Localhost, currentOperation.OperationId);
        }

        private void DdManager_DragDropComplete(object sender, Guid operationId)
        {
            ISLogger.Write("DragDropController: Localhost drop complete");
            OnDropComplete(ISServerSocket.Localhost, operationId);
        }

        private void DdManager_DragDropCancelled(object sender, EventArgs e)
        {
            ISLogger.Write("DragDropController: Localhost drop cancelled");
            OnDropCancel(ISServerSocket.Localhost, currentOperation.OperationId);
        }

        private void DdManager_DataDropped(object sender, ClipboardDataBase cbData)
        {
            ISLogger.Write("DragDropController: Localhost data dropped");
            BeginOperation(ISServerSocket.Localhost, cbData, Guid.NewGuid());
        }

        public void Client_DataDropped(object sender, NetworkSocket.DragDropDataReceivedArgs args)
        {
            ISServerSocket client = sender as ISServerSocket;
            ISLogger.Write("DragDropController: {0} data dropped", client.ClientName);
            BeginOperation(client, ClipboardDataBase.FromBytes(args.RawData), args.OperationId);
        }

        public void Client_DragDropCancelled(object sender, Guid operationId)
        {
            ISServerSocket client = sender as ISServerSocket;
            ISLogger.Write("DragDropController: {0} drop cancelled", client.ClientName);
            OnDropCancel(client, operationId);
        }

        public void Client_DragDropSuccess(object sender, Guid operationId)
        {
            ISServerSocket client = sender as ISServerSocket;
            ISLogger.Write("DragDropController: {0} drop success", client.ClientName);
            OnDropSuccess(client, operationId);
        }

        public void Client_DragDropComplete(object sender, Guid operationId)
        {
            ISServerSocket client = sender as ISServerSocket;
            ISLogger.Write("DragDropController: {0} drop complete", client.ClientName);
            OnDropComplete(client, operationId);
        }

        private void BeginOperation(ISServerSocket sender, ClipboardDataBase cbData, Guid operationId)
        {
            if(cbData == null)
            {
                ISLogger.Write("DragDropController: Cannot begin operation: Data was null");
                return;
            }

            if(currentOperation != null && currentOperation.State == DragDropState.Dragging)
            {
                ISLogger.Write("DragDropController: Cannot begin operation: another operation is currently dragging");
                return;
            }

            if (cbData.DataType == ClipboardDataType.File)
            {
                Task.Run(() => { BeginFileOperation(cbData as ClipboardVirtualFileData, sender, operationId); OnOperationChanged(); });
                return;
            }
            else
            {
                BeginTextOrImageOperation(cbData);
                OnOperationChanged();
            }
        }

        /// <summary>
        /// Begins a file dragdrop operation, and assigns an access token to the operation
        /// </summary>
        /// <param name="cbFiles"></param>
        /// <param name="host"></param>
        /// <param name="operationId"></param>
        private async void BeginFileOperation(ClipboardVirtualFileData cbFiles, ISServerSocket host, Guid operationId)
        {
            Guid fileAccesstoken = Guid.Empty;

            DragDropOperation newOperation = new DragDropOperation(cbFiles, host, operationId);

            try
            {
                fileAccesstoken = await CreateAccessTokenForOperation(newOperation);
            }catch(Exception ex)
            {
                ISLogger.Write("DragDropController: Cancelling operation, could not generate file access token: " + ex.Message);
                return;
            }
            newOperation.RemoteFileAccessToken = fileAccesstoken;

            //Create events, incase the files are dropped on localhost
            if (!newOperation.Host.IsLocalhost)
            {
                for(int i =0; i < cbFiles.AllFiles.Count; i++)
                {
                    cbFiles.AllFiles[i].RemoteAccessToken = fileAccesstoken;
                    cbFiles.AllFiles[i].ReadDelegate = File_RequestDataAsync;
                    cbFiles.AllFiles[i].ReadComplete += VirtualFile_ReadComplete;
                    cbFiles.AllFiles[i].FileOperationId = newOperation.OperationId;
                }
            }

            //If the previous dragdrop operation is still transfering files, store it so that the files can keep being transfered
            if (currentOperation != null && currentOperation.State == DragDropState.TransferingFiles)
                previousOperationIds.Add(currentOperation.OperationId, currentOperation);

            //Sets the new operation, which is automatically set to dragging state
            currentOperation = newOperation;

            if (currentInputClient.IsLocalhost)
            {
                ddManager.DoDragDrop(currentOperation.OperationData);
            }
            else
            {
                currentInputClient.SendDragDropData(currentOperation.OperationData.ToBytes(), currentOperation.OperationId);
            }
        }

        /// <summary>
        /// If localhost drops files, each virtualfile will be given this delegate to allow it to read data 
        /// from the operation host. The virtual file will contain the token that it should use to access the files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VirtualFile_ReadComplete(object sender, EventArgs e)
        {
            ClipboardVirtualFileData.FileAttributes file = sender as ClipboardVirtualFileData.FileAttributes;
            ISLogger.Write("DragDropController: File {0} read", file.FileName);
            
            if(currentOperation.RemoteFileAccessToken != file.RemoteAccessToken)
            {
                if (previousOperationIds.ContainsKey(file.FileOperationId))
                {
                    previousOperationIds.TryGetValue(file.FileOperationId, out DragDropOperation operation);

                    if(operation == null)
                    {
                        ISLogger.Write("DragDropController: Error sending stream close for file {0}: cannot find dragdrop operation", file.FileName);
                        return;
                    }

                    operation.Host.RequestCloseStream(file.RemoteAccessToken, file.FileRequestId);
                }
                else
                {
                    ISLogger.Write("DragDropController: Error sending stream close for file {0}: cannot find dragdrop operation", file.FileName);
                    return;
                }
            }
            else
            {
                currentOperation.Host.RequestCloseStream(file.RemoteAccessToken, file.FileRequestId);
            }
        }

        private void BeginTextOrImageOperation(ClipboardDataBase cbData)
        { 
            //If the previous dragdrop operation is still transfering files, store it so that the files can keep being transfered
            if (currentOperation != null && currentOperation.State == DragDropState.TransferingFiles)
                previousOperationIds.Add(currentOperation.OperationId, currentOperation);
            
            
            //Create a new operation with the text/image data, we don't care about the ID or host
            //as text and image data is not streamed the same way as file data. Text/Image data are sent as a single block
            currentOperation = new DragDropOperation(cbData, null, Guid.NewGuid());

            if (currentInputClient.IsLocalhost)
            {
                ddManager.DoDragDrop(cbData);
            }
            else
            {
                ISLogger.Write("Sending dragdrop type {0} to {1}", cbData.DataType, currentInputClient.ClientName);
                currentInputClient.SendDragDropData(cbData.ToBytes(), currentOperation.OperationId);
            }

            
        }

        private void OnDropCancel(ISServerSocket sender, Guid operationId)
        {
            if (sender != currentInputClient)
            {
                ISLogger.Write("DragDropController: {0} attempted to cancel dragdrop operation when they are not input client!", sender.ClientName);
                return;
            }

            if(operationId == currentOperation?.OperationId)
            {
                ISLogger.Write("DragDropController: {0} cancelled current operation", sender.ClientName);
                currentOperation.State = DragDropState.Cancelled;
                //Make sure all clients release any files that are currently being dragged
                CancelAllDragDrops();

                if (currentOperation != null && currentOperation.Host != null && currentOperation.Host.IsLocalhost)
                {
                    fileController.DeleteToken(currentOperation.RemoteFileAccessToken);
                }
                else if(currentOperation != null && currentOperation.Host != null && !currentOperation.Host.IsLocalhost)
                {
                    //Tells the host client to close all filestreams for this operation
                    currentOperation.Host.SendDragDropComplete(operationId);
                }
                OnOperationChanged();
            }
            else if (previousOperationIds.ContainsKey(operationId))
            {
                ISLogger.Write("DragDropController: {0} cancelled old operation", sender.ClientName);
                if (previousOperationIds.TryGetValue(operationId, out DragDropOperation operation))
                {
                    operation.State = DragDropState.Cancelled;

                    if (operation.Host.IsLocalhost && operation.DataType == ClipboardDataType.File)
                    {
                        fileController.DeleteToken(currentOperation.RemoteFileAccessToken);
                    }
                    else if(operation.DataType == ClipboardDataType.File)
                    {
                        //Tells the host client to close all filestreams for this operation
                        operation.Host.SendDragDropComplete(operationId);
                    }
                    OnOperationChanged();
                }
            }

           
        }

        private void OnDropSuccess(ISServerSocket sender, Guid operationId)
        {
            if (sender != currentInputClient)
            {
                ISLogger.Write("DragDropController: {0} attempted to send drop success when they are not input client!", sender.ClientName);
                return;
            }


            //If the specified operation is the current operation, mark it as success only if it is in the dragging state
            if (currentOperation?.OperationId == operationId && currentOperation?.State == DragDropState.Dragging)
            {
                ISLogger.Write("{0} marked current dragdrop operation success... transfering files...", sender.ClientName);
                currentOperation.State = DragDropState.TransferingFiles;
                currentOperation.ReceiverClient = sender;
                OnOperationChanged();
            }
            else if (currentOperation.OperationId == operationId && currentOperation.State != DragDropState.Dragging)
            {
                ISLogger.Write("DragDropController: {0} attempted to send drop success when specified operationstate is {1}!", sender.ClientName, currentOperation.State);
                return;
            }

            //If it is not the current operation, check if it a previous operation, if so mark as transferingfiles
            if (currentOperation?.OperationId != operationId)
            {
                if (previousOperationIds.ContainsKey(operationId))
                {
                    if (previousOperationIds.TryGetValue(operationId, out DragDropOperation operation))
                    {
                        if (operation.State == DragDropState.Dragging)
                        {
                            ISLogger.Write("{0} marked previous dragdrop operation success", sender.ClientName);
                            operation.State = DragDropState.TransferingFiles;
                            operation.ReceiverClient = sender;
                            OnOperationChanged();
                            return;
                        }
                    }
                }
                else
                {
                    ISLogger.Write("{0} attempted to mark unknown dragdrop operation as success", sender.ClientName);
                    return;
                }
            }

            currentOperation.State = DragDropState.TransferingFiles;
        }

        /// <summary>
        /// Called when localhost or a client fully completes a dragdrop operation
        /// Complete means that the files have been fully read, while success means they have been dropped.
        /// When the drop is complete, we can then close all filestreams associated with this drop.
        /// Also if the host is a client, let them know that the drop is complete.
        /// </summary>
        /// <param name="sender"></param>
        private void OnDropComplete(ISServerSocket sender, Guid operationId)
        {
            if(currentOperation.OperationId == operationId)
            {
                ISLogger.Write("Current dragdrop operation marked as complete by " + sender.ClientName);
                currentOperation.State = DragDropState.Complete;
                CancelAllDragDrops();

                //Let the host know that it can now close all streams and delete access token
                currentOperation.Host.SendDragDropComplete(operationId);
                OnOperationChanged();
            }
            else
            {
                if (previousOperationIds.ContainsKey(operationId))
                {
                    previousOperationIds.TryGetValue(operationId, out DragDropOperation oldOperation);

                    if(oldOperation != null)
                    {
                        oldOperation.State = DragDropState.Complete;
                        ISLogger.Write(sender.ClientName + " marked old dragdrop operation as complete");
                        CancelAllDragDrops();
                        OnOperationChanged();
                        return;
                    }
                }
                ISLogger.Write(sender.ClientName + " attempted to mark incorrect dragdrop operation as complete - " + operationId);
            }
           
        }

        /// <summary>
        /// Cancels any drop operations currently running on any client
        /// </summary>
        private void CancelAllDragDrops(ISServerSocket exceptClient = null)
        {
            ddManager.CancelDrop();
            foreach(var client in clientMan.AllClients.Where(i => !i.IsLocalhost))
            {
                if (exceptClient != null && client == exceptClient)
                    continue;

                client.SendCancelDragDrop();
            }
        }

        public void HandleClientSwitch(ISServerSocket newClient, ISServerSocket oldClient)
        {
            currentInputClient = newClient;

            //if moving from localhost to client, check for drop if not in operation, Only do this if the left mouse button is down
            if (oldClient.IsLocalhost && currentOperation?.State != DragDropState.Dragging && ddManager.LeftMouseState)
                ddManager.CheckForDrop();

            if (currentOperation == null)
                return;

            //If we are dragging a file, make sure that we don't send the dragdrop back to the host
            //as this causes issues
            if (currentOperation.DataType == ClipboardDataType.File)
                if (newClient == currentOperation.Host)
                    return;

            if (currentOperation.State == DragDropState.Dragging)
                if (newClient.IsLocalhost)
                    ddManager.DoDragDrop(currentOperation.OperationData);

            //if we are dragging a file, send the dragdrop data to the client
            if(currentOperation.State == DragDropState.Dragging)
                if (!newClient.IsLocalhost)
                    newClient.SendDragDropData(currentOperation.OperationData.ToBytes(), currentOperation.OperationId);

            //Just a check to make sure that all other clients discard the last dragdrop
            CancelAllDragDrops(newClient);
        }

        bool reportedSuccess = false;

        /// <summary>
        /// The virtual file object calls this method when it needs to read the next block of data
        /// </summary>
        /// <param name="token"></param>
        /// <param name="fileId"></param>
        /// <param name="readLen"></param>
        /// <returns></returns>
        private async Task<byte[]> File_RequestDataAsync(Guid token, Guid fileId, int readLen)
        {
            if (!reportedSuccess)
            {
                reportedSuccess = true;
                currentOperation.State = DragDropState.TransferingFiles;
            }

            try
            {
                return await currentOperation.Host.RequestReadStreamAsync(token, fileId, readLen);
            }
            catch (Exception ex)
            {
                ISLogger.Write("DragDropController: " + ex.Message);
                return new byte[0];
            }
            
        }

        private async Task<Guid> CreateAccessTokenForOperation(DragDropOperation operation)
        {
            if (operation.Host.IsLocalhost)
            {
                return GenerateLocalAccessTokenForOperation(operation);
            }
            else
            {
                return await operation.Host.RequestFileTokenAsync(operation.OperationId);
            }

        }

        private void OnOperationChanged()
        {

        }

        private Guid GenerateLocalAccessTokenForOperation(DragDropOperation operation)
        {
            if(operation.DataType != ClipboardDataType.File)
            {
                throw new ArgumentException("DateType must be file");
            }

            ClipboardVirtualFileData file = operation.OperationData as ClipboardVirtualFileData;
            Guid[] fIds = new Guid[file.AllFiles.Count];
            string[] fSources = new string[file.AllFiles.Count];

            for(int i = 0; i < file.AllFiles.Count; i++)
            {
                fIds[i] = file.AllFiles[i].FileRequestId;
                fSources[i] = file.AllFiles[i].FullPath;
            }

            return fileController.CreateFileReadTokenForGroup(new FileAccessController.FileAccessInfo(fIds, fSources));
        }

        internal class DragDropOperation
        {
            public DragDropOperation(ClipboardDataBase operationData, ISServerSocket host, Guid operationId)
            {
                OperationData = operationData;
                Host = host;
                OperationId = operationId;
                StartTime = DateTime.Now;
            }
            public DateTime StartTime { get; }
            public ClipboardDataType DataType { get => OperationData.DataType; }
            public ClipboardDataBase OperationData { get; }
            public ISServerSocket Host { get; }
            public Guid OperationId { get; }
            public Guid RemoteFileAccessToken { get; set; }
            public ISServerSocket ReceiverClient { get; set; }
            public DragDropState State { get; set; } = DragDropState.Dragging;
        }

        public enum DragDropState
        {
            None,
            Dragging,
            Dropped,
            Complete,
            TransferingFiles,
            Cancelled
        }
    }
}
