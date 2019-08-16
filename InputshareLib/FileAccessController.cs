
using InputshareLib.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InputshareLib
{
    /// <summary>
    /// Controls network access to files for use with dragdrop and clipboard virtual files
    /// </summary>
    class FileAccessController
    {
        private Dictionary<Guid, AccessToken> currentAccessTokens = new Dictionary<Guid, AccessToken>();

        private class AccessToken
        {
            public AccessToken(Guid tokenId, Guid[] allowedFiles, string[] allowedFileSources)
            {
                TokenId = tokenId;
                AllowedFiles = allowedFiles;

                for(int i = 0; i < allowedFiles.Length; i++)
                {
                    fileSourceDictionary.Add(allowedFiles[i], allowedFileSources[i]);
                }
            }

            public void CloseAllStreams()
            {
                foreach(var stream in openFileStreams)
                {
                    stream.Value.Dispose();
                }
            }

            public void CloseStream(Guid file)
            {
                if(openFileStreams.TryGetValue(file, out FileStream stream))
                {
                    stream.Close();
                }
            }

            public int ReadFile(Guid file, byte[] buffer, int offset, int readLen)
            {
                if(openFileStreams.TryGetValue(file, out FileStream stream))
                {
                    return stream.Read(buffer, offset, readLen);
                }
                else
                {
                    if(fileSourceDictionary.TryGetValue(file, out string source))
                    {
                        FileStream fs = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
                        //ISLogger.Write("Debug: Filestream created for " + source);
                        openFileStreams.Add(file, fs);
                        return fs.Read(buffer, offset, readLen);
                    }
                    else
                    {
                        throw new ArgumentException("Stream not found in token");
                    }
                }
            }

            public long SeekFile(Guid file, SeekOrigin origin, long offset)
            {
                if (openFileStreams.TryGetValue(file, out FileStream stream))
                {
                    return stream.Seek(offset, origin);
                }
                else
                {
                    if(fileSourceDictionary.TryGetValue(file, out string source))
                    {
                        FileStream fs = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
                       // ISLogger.Write("Debug: Filestream created for " + source);
                        openFileStreams.Add(file, fs);
                        return fs.Seek(offset, origin);
                    }
                    else
                    {
                        throw new ArgumentException("Stream not found in token");
                    }

                }
            }


            public Guid TokenId { get; }
            public Guid[] AllowedFiles { get; }

            private Dictionary<Guid, string> fileSourceDictionary = new Dictionary<Guid, string>();
            private Dictionary<Guid, FileStream> openFileStreams = new Dictionary<Guid, FileStream>();
        }

        public Guid CreateFileReadToken(string sourceFile, Guid fileId)
        {
            Guid id = Guid.NewGuid();
            AccessToken newToken = new AccessToken(id, new Guid[] { fileId }, new string[] { sourceFile });
            currentAccessTokens.Add(id, newToken);
            return id;
        }

        public Guid CreateFileReadTokenForGroup(FileAccessInfo info)
        {
            Guid accessId = Guid.NewGuid();
            AccessToken token = new AccessToken(accessId, info.FileIds, info.FileSources);
            currentAccessTokens.Add(accessId, token);
            ISLogger.Write("Created group token {0} for {1} files", accessId, info.FileIds.Length);
            return accessId;
        }

        public int ReadStream(Guid token, Guid file, byte[] buffer, int offset, int readLen)
        {
            if(currentAccessTokens.TryGetValue(token, out AccessToken access)){
                int r = access.ReadFile(file, buffer, offset, readLen);
                return r;
            }
            else
            {
                ISLogger.Write("FileAccessController: Token not found");
                throw new TokenNotFoundException();
            }
            
        }

        public long SeekStream(Guid token, Guid file, SeekOrigin origin, long offset)
        {
            if (currentAccessTokens.TryGetValue(token, out AccessToken access))
            {
                return access.SeekFile(file, origin, offset);
            }
            else
            {
                throw new TokenNotFoundException();
            }
        }

        public bool DoesTokenExist(Guid token)
        {
            return currentAccessTokens.ContainsKey(token);
        }

        public void CloseStream(Guid token, Guid file)
        {
            if(currentAccessTokens.TryGetValue(token, out AccessToken access))
            {
                access.CloseStream(file);
            }
        }

        public void DeleteToken(Guid token)
        {
            ISLogger.Write("Deleting access token " + token);

            if (currentAccessTokens.ContainsKey(token))
            {
                currentAccessTokens.TryGetValue(token, out AccessToken access);

                if(access == null)
                {
                    ISLogger.Write("Could not delete access token: Access token was null");
                    return;
                }

                access.CloseAllStreams();

            }
            else
            {
                ISLogger.Write("Could not delete access token: Key {0} not found", token);
            }

        }

        public class FileAccessInfo
        {
            public FileAccessInfo(Guid[] fileIds, string[] fileSources)
            {
                FileIds = fileIds;
                FileSources = fileSources;
            }

            public Guid[] FileIds { get; }
            public string[] FileSources { get; }
        }

        public class TokenNotFoundException : Exception
        {
            public TokenNotFoundException() : base()
            {

            }
        }
    }
}
