using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Net.Messages
{
    internal class RequestGroupTokenResponseMessage : NetworkMessage
    {
        public RequestGroupTokenResponseMessage(byte[] data) : base(data)
        {
            byte[] idData = new byte[16];
            Buffer.BlockCopy(data, 21, idData, 0, 16);
            Token = new Guid(idData);
        }

        public override byte[] ToBytes()
        {
            byte[] data = WritePacketInfo(this, 16);
            Buffer.BlockCopy(Token.ToByteArray(), 0, data, 21, 16);
            return data;
        }

        public RequestGroupTokenResponseMessage(Guid token, Guid messageId = default) : base(MessageType.RequestFileGroupTokenReponse, messageId)
        {
            Token = token;
        }

        public Guid Token { get; }
    }
}
