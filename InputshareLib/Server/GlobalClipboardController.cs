using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InputshareLib.Server
{
    //TODO - write this properly
    class GlobalClipboardController
    {
        public delegate void SetClipboardDataDelegate(ClipboardDataBase cbData);

        private SetClipboardDataDelegate SetLocalClipboardData;
        private ClientManager clientMan;
        private ClipboardOperation currentData;

        private Dictionary<Guid, ClipboardOperation> previousOperationDictionary = new Dictionary<Guid, ClipboardOperation>();

        public GlobalClipboardController(ClientManager clientManager, SetClipboardDataDelegate setcb)
        {
            SetLocalClipboardData = setcb;
            clientMan = clientManager;
        }

        public void OnLocalClipboardDataCopied(object sender, ClipboardDataBase cbData)
        {
            //Generate a new GUID for this clipboard operation
            Guid opId = Guid.NewGuid();
            SetGlobalClipboard(cbData, ISServerSocket.Localhost, opId);
        }

        public void OnClientClipboardDataReceived(object sender, NetworkSocket.ClipboardDataReceivedArgs args)
        {
            ISServerSocket client = sender as ISServerSocket;
            ClipboardDataBase cbData = ClipboardDataBase.FromBytes(args.rawData);
            SetGlobalClipboard(cbData, client, args.OperationId);
        }

        private void SetGlobalClipboard(ClipboardDataBase data, ISServerSocket host, Guid operationId)
        {
            ISLogger.Write("New clipboard operation. Type {0}, Host {1}, ID {2}", data.DataType, host.ClientName, operationId);

            if (data.DataType == ClipboardDataType.File)
            {
                SetClipboardFiles((data as ClipboardVirtualFileData), host, operationId);
            }
            else
            {
                SetClipboardTextOrImage(data, host, operationId);
            }
        }

        private async void SetClipboardFiles(ClipboardVirtualFileData cbFiles, ISServerSocket host, Guid operationId)
        {
            if (currentData != null)
                previousOperationDictionary.Add(currentData.OperationId, currentData);

            currentData = new ClipboardOperation(operationId, cbFiles.DataType, cbFiles, host);
            if (host == ISServerSocket.Localhost)
            {
                //create tokens and send to client
            }
            else
            {
                currentData.HostFileAccessToken = await currentData.Host.RequestFileTokenAsync(operationId);
                ISLogger.Write("Host file access token: " + currentData.HostFileAccessToken);
            }

            foreach(var file in cbFiles.AllFiles)
            {
                file.ReadComplete += File_ReadComplete;
                file.ReadDelegate = File_RequestDataAsync;
            }

            SetLocalClipboardData(cbFiles);
        }

        private void File_ReadComplete(object sender, EventArgs e)
        {
            ClipboardVirtualFileData.FileAttributes file = sender as ClipboardVirtualFileData.FileAttributes;
            ISLogger.Write("Debug: Clipboard: {0} read complete", file.FileName);
        }

        private async Task<byte[]> File_RequestDataAsync(Guid token, Guid fileId, int readLen)
        {
            return await currentData.Host.RequestReadStreamAsync(token, fileId, readLen);
        }

        private void SetClipboardTextOrImage(ClipboardDataBase cbData, ISServerSocket host, Guid operationId)
        {
            if (currentData != null)
                previousOperationDictionary.Add(currentData.OperationId, currentData);

            currentData = new ClipboardOperation(operationId, cbData.DataType, cbData, host);
            BroadcastCurrentOperation();
        }

        private void BroadcastCurrentOperation()
        {
            foreach(var client in clientMan.AllClients.Where(i => i != currentData.Host && i != ISServerSocket.Localhost))
            {
                client.SendClipboardData(currentData.Data.ToBytes(), currentData.OperationId);
                ISLogger.Write("Debug: Sent operation " + currentData.OperationId + " to " + client.ClientName);
            }
            SetLocalClipboardData(currentData.Data);
        }

        class ClipboardOperation
        {
            public ClipboardOperation(Guid operationId, ClipboardDataType dataType, ClipboardDataBase data, ISServerSocket host)
            {
                OperationId = operationId;
                DataType = dataType;
                Data = data;
                Host = host;
            }

            public Guid OperationId { get; }
            public ClipboardDataType DataType { get; }
            public ClipboardDataBase Data { get; }
            public ISServerSocket Host { get; }
            public Guid HostFileAccessToken { get; set; }
        }
    }
}
