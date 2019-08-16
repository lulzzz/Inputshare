using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages
{
    class DragDropCompleteMessage : NetworkMessage
    {
        public DragDropCompleteMessage(byte[] data) : base(data)
        {
            byte[] idData = new byte[16];
            Buffer.BlockCopy(data, 21, idData, 0, 16);
            OperationId = new Guid(idData);
        }

        public override byte[] ToBytes()
        {
            byte[] data = WritePacketInfo(this, 16);
            Buffer.BlockCopy(OperationId.ToByteArray(), 0, data, 21, 16);
            return data;
        }

        public DragDropCompleteMessage(Guid fileGroupId, Guid messageId = default) : base(MessageType.DragDropComplete, messageId)
        {
            OperationId = fileGroupId;
        }

        public Guid OperationId { get; }
    }
}
