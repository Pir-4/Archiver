using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Runtime.Remoting.Messaging;

namespace VeeamSoftware_test.Gzip
{

    public static class GzipDriver
    {
        public static void CompressFile(string sDir, string sRelativePath, GZipStream zipStream)
        {

            CompresFileName(sRelativePath, zipStream);

            //Compress file content
            using (FileStream file = new FileStream(Path.Combine(sDir, sRelativePath), FileMode.Open, FileAccess.Read))
            {
                zipStream.Write(BitConverter.GetBytes(file.Length), 0, sizeof(int));

                long numBytesToRead = (long) file.Length;
                int numBytesRead = 0;
                int size = numBytesToRead > Int32.MaxValue ?  Int32.MaxValue : (int)numBytesToRead;
                try
                {
                    while (numBytesToRead > 0)
                    {
                        byte[] bytes = new byte[size];
                        // Read may return anything from 0 to numBytesToRead.
                        int n = file.Read(bytes, 0, size);

                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;

                        zipStream.Write(bytes, 0, bytes.Length);
                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                }
                catch (Exception e)
                {
                    
                    throw e;
                }
                

            }
                /*byte[] bytes = File.ReadAllBytes(Path.Combine(sDir, sRelativePath));
            zipStream.Write(BitConverter.GetBytes(bytes.Length), 0, sizeof(int));
            zipStream.Write(bytes, 0, bytes.Length);*/
        }
        private static void CompresFileName(string sRelativePath, GZipStream zipStream)
        {
            //Compress file name
            char[] chars = sRelativePath.ToCharArray();
            zipStream.Write(BitConverter.GetBytes(chars.Length), 0, sizeof(int));
            foreach (char c in chars)
                zipStream.Write(BitConverter.GetBytes(c), 0, sizeof(char));
        }

        public static bool DecompressFile(string sDir, GZipStream zipStream)
        {
            string sFileName = DecompressFileName(zipStream);
            if (sFileName == null)
                return false;

            //Decompress file content
            byte[]  bytes = new byte[sizeof(int)];
            zipStream.Read(bytes, 0, sizeof(int));
            int iFileLen = BitConverter.ToInt32(bytes, 0);

            bytes = new byte[iFileLen];
            zipStream.Read(bytes, 0, bytes.Length);

            string sFilePath = Path.Combine(sDir, sFileName);
            string sFinalDir = Path.GetDirectoryName(sFilePath);
            if (!Directory.Exists(sFinalDir))
                Directory.CreateDirectory(sFinalDir);

            using (FileStream outFile = new FileStream(sFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                outFile.Write(bytes, 0, iFileLen);

            return true;
        }
        private static string DecompressFileName(GZipStream zipStream)
        {
            //Decompress file name

            byte[] bytes = new byte[sizeof(int)];
            int Readed = zipStream.Read(bytes, 0, sizeof(int));
            if (Readed < sizeof(int))
                return null;

            int iNameLen = BitConverter.ToInt32(bytes, 0);
            bytes = new byte[sizeof(char)];
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < iNameLen; i++)
            {
                zipStream.Read(bytes, 0, sizeof(char));
                char c = BitConverter.ToChar(bytes, 0);
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
