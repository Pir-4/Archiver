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
        string Act { get; }
    }

    public abstract class GZipManager : IGZipManager
    {
        public const string Compress = "compress";
        public const string Decompress = "decompress";

        protected GzipDriver _driver;
        private string _act;

        public static IGZipManager create(string act, string inputFile, string outputfile)
        {
            GZipManager manager = null;

            if (act.ToLower().Equals(Compress))
                return new GZipManagerCompress(inputFile, outputfile);

            if (act.ToLower().Equals(Decompress))
                return new GZipManagerDecompress(inputFile, outputfile);

            return null;
        }
        public void Execute()
        {
            _driver.Run();
        }
        public abstract string Act { get; }
    }

    public class GZipManagerCompress : GZipManager
    {
        public GZipManagerCompress(string inputFile, string outputfile)
        {
            _driver = new GzipDriverCompress(inputFile, outputfile);
        }
        public override string Act
        {
            get { return Compress; }
        }
    }
    public class GZipManagerDecompress : GZipManager
    {
        public GZipManagerDecompress(string inputFile, string outputfile)
        {
            _driver = new GzipDriverDecompress(inputFile, outputfile);
        }
        public override string Act
        {
            get { return Decompress; }
        }
    }

}
