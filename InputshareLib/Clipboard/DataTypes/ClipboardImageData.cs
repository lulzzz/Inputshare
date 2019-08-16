using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Mime;
using System.Text;

namespace InputshareLib.Clipboard.DataTypes
{
    public class ClipboardImageData : ClipboardDataBase
    {
        public ClipboardImageData(byte[] imageData, bool constructor = false)
        {
            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            ImageData = imageData;

        }

        public ClipboardImageData(byte[] data)
        {
            ImageData = new byte[data.Length - 1];
            Buffer.BlockCopy(data, 1, ImageData, 0, ImageData.Length);
        }

        public override byte[] ToBytes()
        {
            byte[] data = new byte[ImageData.Length + 1];
            data[0] = (byte)ClipboardDataType.Image;
            Buffer.BlockCopy(ImageData, 0, data, 1, ImageData.Length);
            return data;
        }

        public byte[] ImageData { get; }

        public override ClipboardDataType DataType { get => ClipboardDataType.Image; }
    }
}
