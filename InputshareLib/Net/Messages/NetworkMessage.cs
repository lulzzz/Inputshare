using System;

namespace InputshareLib.Net.Messages
{
    internal class NetworkMessage
    {
        public const int MessageOverheadBytes = 21;
        public virtual MessageType Type { get; }
        public Guid MessageId { get; }

        public virtual byte[] ToBytes()
        {
            return WritePacketInfo(this, 0);
        }

        public NetworkMessage(MessageType type, Guid messageId = default)
        {
            Type = type;

            if (messageId == default)
                MessageId = Guid.NewGuid();
            else
                MessageId = messageId;
        }

        /// <summary>
        /// Read the network message from a byte array
        /// </summary>
        /// <param name="data"></param>
        public NetworkMessage(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            Type = (MessageType)data[4];
            byte[] idData = new byte[16];
            Buffer.BlockCopy(data, 5, idData, 0, 16);
            MessageId = new Guid(idData);
        }

        /// <summary>
        /// Writes header information for the specified packet into a byte array, and returns a byte
        /// array with enough free room to store the rest of the packet
        /// </summary>
        /// <param name="message">target packet header</param>
        /// <param name="neededLen">required length for rest of packet</param>
        /// <returns></returns>
        public static byte[] WritePacketInfo(NetworkMessage message, int neededLen)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            byte[] data = new byte[21 + neededLen];
            Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, data, 0, 4);
            data[4] = (byte)message.Type;
            Buffer.BlockCopy(message.MessageId.ToByteArray(), 0, data, 5, 16);
            return data;
        }
    }
}
