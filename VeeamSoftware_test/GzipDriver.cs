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
    public interface IGzipDriver
    {
        void Compress(string sInDir, string sOutFile);
        void Decompress(string sCompressedFile, string sDir);
    }
    public abstract class GzipDriver : IGzipDriver
    {
        public IGzipDriver create(string pathToFileOrDirecoty)
        {
            if (File.Exists(pathToFileOrDirecoty))
                return new GzipDriverFile();

             return new GzipDriverDirectory();
        }
        public abstract void Compress(string sInDir, string sOutFile);

        private static void CompressFile(string sDir, string sRelativePath, GZipStream zipStream)
        {

            CompresFileName(sRelativePath, zipStream);

            //Compress file content
            byte[] bytes = File.ReadAllBytes(Path.Combine(sDir, sRelativePath));
            zipStream.Write(BitConverter.GetBytes(bytes.Length), 0, sizeof(int));
            zipStream.Write(bytes, 0, bytes.Length);
        }
        private static void CompresFileName(string sRelativePath, GZipStream zipStream)
        {
            //Compress file name
            char[] chars = sRelativePath.ToCharArray();
            zipStream.Write(BitConverter.GetBytes(chars.Length), 0, sizeof(int));
            foreach (char c in chars)
                zipStream.Write(BitConverter.GetBytes(c), 0, sizeof(char));
        }

        private static bool DecompressFile(string sDir, GZipStream zipStream)
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

        protected static void CompressDirectory(string sInDir, string sOutFile)
        {
            string[] sFiles = Directory.GetFiles(sInDir, "*.*", SearchOption.AllDirectories);
            int iDirLen = sInDir[sInDir.Length - 1] == Path.DirectorySeparatorChar ? sInDir.Length : sInDir.Length + 1;

            using (FileStream outFile = new FileStream(sOutFile, FileMode.Create, FileAccess.Write, FileShare.None))
            using (GZipStream str = new GZipStream(outFile, CompressionMode.Compress))
                foreach (string sFilePath in sFiles)
                {
                    string sRelativePath = sFilePath.Substring(iDirLen);
                    CompressFile(sInDir, sRelativePath, str);
                }
        }
        protected static void CompressFile(string sInFile, string sOutFile)
        {
            using (FileStream outFile = new FileStream(sOutFile, FileMode.Create, FileAccess.Write, FileShare.None))
            using (GZipStream str = new GZipStream(outFile, CompressionMode.Compress))
            {
                string sRelativePath = Path.GetFileName(sInFile);
                CompressFile(Path.GetDirectoryName(sInFile), sRelativePath, str);
            }
        }

        public void Decompress(string sCompressedFile, string sDir)
        {
            using (FileStream inFile = new FileStream(sCompressedFile, FileMode.Open, FileAccess.Read, FileShare.None))
            using (GZipStream zipStream = new GZipStream(inFile, CompressionMode.Decompress, true))
                while (DecompressFile(sDir, zipStream)) ;
        }
    }

    public class GzipDriverFile : GzipDriver
    {
        public override void Compress(string sInDir, string sOutFile)
        {
            CompressFile(sInDir, sOutFile);
        }
    }
    public class GzipDriverDirectory : GzipDriver
    {
        public override void Compress(string sInDir, string sOutFile)
        {
            CompressDirectory(sInDir, sOutFile);
        }
    }
}
