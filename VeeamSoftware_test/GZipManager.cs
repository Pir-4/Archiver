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
        void Execute(string sourceFile, string resultFile);
    }

    public abstract class GZipManager : IGZipManager
    {
        private const string Compress = "compress";
        private const string Decompress = "decompress";

        public static IGZipManager create(string act)
        {
            if (act.ToLower().Equals(Compress))
                return new GZipManagerCompress();

            if (act.ToLower().Equals(Decompress))
                return new GZipManagerDecompress();

            return null;
        }

        public abstract void Execute(string sourceFile, string resultFile);
    }

    public class GZipManagerCompress : GZipManager
    {
        public override void Execute(string sourceFile, string resultFile)
        {
                using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                {
                    using (var outStream = new FileStream(resultFile, FileMode.Create, FileAccess.Write))
                    {
                        using (var gZipStream = new GZipStream(outStream, CompressionMode.Compress))
                        {
                            GzipDriver.ModificationOfData(sourceStream, gZipStream);
                        }
                    }
                }

            
        }
    }
    public class GZipManagerDecompress : GZipManager
    {
        public override void Execute(string sourceFile, string resultFile)
        {
            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
            {
                using (var gZipStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                {
                    using (var outStream = new FileStream(resultFile,FileMode.Create,FileAccess.Write))
                    {
                        GzipDriver.ModificationOfData(gZipStream, outStream);
                    }
                }
            }
        }
    }
}
