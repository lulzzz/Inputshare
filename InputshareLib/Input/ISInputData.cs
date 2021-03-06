﻿using System;

namespace InputshareLib.Input
{
    public class ISInputData
    {
        public ISInputData(ISInputCode code, short param1, short param2)
        {
            Code = code;
            Param1 = param1;
            Param2 = param2;
        }

        public ISInputData(byte[] data)
        {
            if (data.Length < 5)
                throw new ArgumentException("Invalid input data");

            this.Code = (ISInputCode)data[0];
            this.Param1 = BitConverter.ToInt16(data, 1);
            this.Param2 = BitConverter.ToInt16(data, 3);
        }

        public byte[] ToBytes()
        {
            byte[] data = new byte[5];
            data[0] = (byte)Code;
            Buffer.BlockCopy(BitConverter.GetBytes(this.Param1), 0, data, 1, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(this.Param2), 0, data, 3, 2);
            return data;
        }

        public ISInputCode Code { get; }
        public short Param1 { get; }
        public short Param2 { get; }
    }
}
