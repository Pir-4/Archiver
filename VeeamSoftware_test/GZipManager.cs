using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace VeeamSoftware_test.Gzip
{
    public interface IGZipManager
    {
        void Execute();
        string SourceFile { get; set; }
        string ResultFile { get; set; }
        string Act { get; }
    }

    public abstract class GZipManager : IGZipManager
    {
        public const string Compress = "compress";
        public const string Decompress = "decompress";

        private string sourceFile;
        private string resultFile;

        public static IGZipManager create(string act)
        {
            if (act.ToLower().Equals(Compress))
                return new GZipManagerCompress();

            /*if (act.ToLower().Equals(Decompress))
                return new GZipManagerDecompress();*/

            return null;
        }

        public abstract void Execute();
        public string SourceFile
        {
            get { return sourceFile; }
            set { sourceFile = value; }
        }
        public string ResultFile {
            get { return resultFile; }
            set { resultFile = value; }
        }
        public abstract string Act { get; }
    }

    public class GZipManagerCompress : GZipManager
    {
        public override void Execute( )
        {
            /* using (var sourceStream = new FileStream(SourceFile, FileMode.Open, FileAccess.Read))
             {
                 using (var outStream = new FileStream(ResultFile, FileMode.Create, FileAccess.Write))
                 {
                     using (var gZipStream = new GZipStream(outStream, CompressionMode.Compress))
                     {
                         GzipDriver.ModificationOfData(sourceStream, gZipStream);
                     }
                 }
             }*/

            GzipDriver.ModificationOfData(SourceFile, ResultFile);
        }
        public override string Act
        {
            get { return Compress; } 
        }
    }
   /* public class GZipManagerDecompress : GZipManager
    {
        public override void Execute()
        {
            using (var sourceStream = new FileStream(SourceFile, FileMode.Open, FileAccess.Read))
            {
                using (var gZipStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                {
                    using (var outStream = new FileStream(ResultFile, FileMode.Create,FileAccess.Write))
                    {
                        GzipDriver.ModificationOfData(gZipStream, outStream);
                    }
                }
            }
        }
        public override string Act
        {
            get { return Decompress; }
        }
    }*/
}
