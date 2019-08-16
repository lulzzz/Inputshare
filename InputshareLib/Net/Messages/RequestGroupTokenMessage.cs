using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages
{
    internal class RequestGroupTokenMessage : NetworkMessage
    {
        public RequestGroupTokenMessage(byte[] data) : base(data)
        {
            byte[] idData = new byte[16];
            Buffer.BlockCopy(data, 21, idData, 0, 16);
            FileGroupId = new Guid(idData);
        }

        public override byte[] ToBytes()
        {
            byte[] data = WritePacketInfo(this, 16);
            Buffer.BlockCopy(FileGroupId.ToByteArray(), 0, data, 21, 16);
            return data;
        }

        public RequestGroupTokenMessage(Guid fileGroupId, Guid messageId = default) : base(MessageType.RequestFileGroupToken, messageId)
        {
            FileGroupId = fileGroupId;
        }

        public Guid FileGroupId { get; }
    }
}
