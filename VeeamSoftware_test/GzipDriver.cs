using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Runtime.Remoting.Messaging;
using System.Threading;

namespace VeeamSoftware_test.Gzip
{

    public  class GzipDriver
    {
        static Mutex mtx = new Mutex();
        private static GZipStream _zipStream;
        private string compressDir;
        private string compressFileName;
        private string decompressDir;

        public static GZipStream ZipStream
        {
            get { return _zipStream; }
            set { _zipStream = value; }
        }
        public string CompressDir
        {
            get { return compressDir; }
            set { compressDir = value; }
        }
        public string DeCompressDir
        {
            get { return decompressDir; }
            set { decompressDir = value; }
        }
        public string CompressFileName
        {
            get { return compressFileName; }
            set { compressFileName = value; }
        }
        public  void CompressFile()
        {
            /*lock (ZipStream)
            {*/
            while (ZipStream == null) ;
            //mtx.WaitOne();
                //CompresFileName();
                char[] chars = CompressFileName.ToCharArray();
                ZipStream.Write(BitConverter.GetBytes(chars.Length), 0, sizeof(int));
                foreach (char c in chars)
                    ZipStream.Write(BitConverter.GetBytes(c), 0, sizeof(char));

                //Compress file content
                byte[] bytes = File.ReadAllBytes(Path.Combine(CompressDir, CompressFileName));
                ZipStream.Write(BitConverter.GetBytes(bytes.Length), 0, sizeof(int));
                ZipStream.Write(bytes, 0, bytes.Length);
            //mtx.ReleaseMutex();
            //}
            
        }

        private void CompresFileName()
        {
            //Compress file name

            char[] chars = CompressFileName.ToCharArray();
            ZipStream.Write(BitConverter.GetBytes(chars.Length), 0, sizeof(int));
            foreach (char c in chars)
                ZipStream.Write(BitConverter.GetBytes(c), 0, sizeof(char));


        }

        public  bool DecompressFile()
        {
            string sFileName = DecompressFileName();
            if (sFileName == null)
                return false;

            //Decompress file content
            byte[]  bytes = new byte[sizeof(int)];
            ZipStream.Read(bytes, 0, sizeof(int));
            int iFileLen = BitConverter.ToInt32(bytes, 0);

            bytes = new byte[iFileLen];
            ZipStream.Read(bytes, 0, bytes.Length);

            string sFilePath = Path.Combine(DeCompressDir, sFileName);
            string sFinalDir = Path.GetDirectoryName(sFilePath);
            if (!Directory.Exists(sFinalDir))
                Directory.CreateDirectory(sFinalDir);

            using (FileStream outFile = new FileStream(sFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                outFile.Write(bytes, 0, iFileLen);

            return true;
        }
        private  string DecompressFileName()
        {
            //Decompress file name

            byte[] bytes = new byte[sizeof(int)];
            int Readed = ZipStream.Read(bytes, 0, sizeof(int));
            if (Readed < sizeof(int))
                return null;

            int iNameLen = BitConverter.ToInt32(bytes, 0);
            bytes = new byte[sizeof(char)];
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < iNameLen; i++)
            {
                ZipStream.Read(bytes, 0, sizeof(char));
                char c = BitConverter.ToChar(bytes, 0);
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
