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
        protected GzipDriver driver;
        private string act;

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
            driver.Run();
        }
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
        public GZipManagerCompress(string inputFile, string outputfile)
        {
            driver = new GzipDriverCompress(inputFile, outputfile);
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
            driver = new GzipDriverDecompress(inputFile, outputfile);
        }
        public override string Act
        {
            get { return Decompress; }
        }
    }

}
