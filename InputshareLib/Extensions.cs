using InputshareLib.Clipboard.DataTypes;
using System;
using System.IO;

namespace InputshareLib
{
    public static class Extensions
    {
        public static void WriteFolderStructure(ClipboardVirtualFileData data, string rootDir)
        {
            if (!Directory.Exists(rootDir))
                Directory.CreateDirectory(rootDir);

            foreach(var rootFolder in data.RootDirectory.SubFolders)
            {
                if (!Directory.Exists(Path.Combine(rootDir, rootFolder.Name)))
                    Directory.CreateDirectory(Path.Combine(rootDir, rootFolder.Name));

                foreach (var file in rootFolder.Files)
                {
                    File.Create(Path.Combine(rootDir, rootFolder.Name, file.FileName)).Dispose();
                }
                
            }

            WriteRecursive(data.RootDirectory, rootDir);
        }

        private static void WriteRecursive(ClipboardVirtualFileData.DirectoryAttributes folder, string rootDir)
        {
            foreach(var sub in folder.SubFolders)
            {
                try
                {
                    ISLogger.Write("Reading folder " + sub.RelativePath);
                    Directory.CreateDirectory(Path.Combine(rootDir, sub.RelativePath));
                    foreach (var file in sub.Files)
                    {
                        File.Create(Path.Combine(rootDir, file.RelativePath)).Dispose();
                        ISLogger.Write("Reading file " + file.RelativePath);
                    }
                }catch(Exception ex)
                {
                    ISLogger.Write("Error writing folder " + sub.RelativePath + ": " + ex.Message);
                }
                finally
                {
                    WriteRecursive(sub, rootDir);
                }
                
                
            }
        }

        public static bool IsBitSet(this byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }
        public static Edge Opposite(this Edge edge)
        {
            return edge switch
            {
                Edge.Bottom => Edge.Top,
                Edge.Top => Edge.Bottom,
                Edge.Left => Edge.Right,
                Edge.Right => Edge.Left,
                _ => Edge.Top,
            };
        }


    }
}
