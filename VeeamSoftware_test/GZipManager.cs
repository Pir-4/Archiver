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

    public class GZipManager : IGZipManager
    {
        public const string Compress = "compress";
        public const string Decompress = "decompress";

        private string sourceFile;
        private string resultFile;
        private GzipDriver driver;
        private string act;

        public static IGZipManager create(string act, string inputFile, string outputfile)
        {
            GZipManager manager = null;

            if (act.ToLower().Equals(Compress))
            {
                manager =  new GZipManager(new GzipDriverCompress(inputFile, outputfile));
                manager.Act = Compress;
            }

            if (act.ToLower().Equals(Decompress))
            {
                manager =  new GZipManager(new GzipDriverDecompress(inputFile, outputfile));
                manager.Act = Decompress;
            }

            return manager;
        }

        private GZipManager(GzipDriver driver)
        {
            this.driver = driver;
        }
        public void Execute()
        {
            driver.ModificationOfData();
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
        public  string Act
        {
            get { return act; }
            set { act = value; }
        }
    }

}
