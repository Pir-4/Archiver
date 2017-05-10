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
        string SourceFile { get;}
        string ResultFile { get;}
        List<Exception> Exceptions();
    }

    public abstract class GZipManager : IGZipManager
    {
        public const string Compress = "compress";
        public const string Decompress = "decompress";

        protected GzipDriver _driver;
        private string _act;

        private string _sourceFile;
        private string _resultFile;

        public static IGZipManager create(string act, string inputFile, string outputfile)
        {
            if (act.ToLower().Equals(Compress))
                return new GZipManagerCompress(inputFile, outputfile);

            if (act.ToLower().Equals(Decompress))
                return new GZipManagerDecompress(inputFile, outputfile);

            return null;
        }

        public GZipManager(string inputFile, string outputfile)
        {
            SourceFile = inputFile;
            ResultFile = outputfile;
        }
        public void Execute()
        {
            _driver.Execute(SourceFile, ResultFile);
        }
        public List<Exception> Exceptions()
        {
            return _driver.Exceptions;
        }
        public abstract string Act { get; }

        public string SourceFile
        {
            get { return _sourceFile; }
            private set { _sourceFile = value; }
        }
        public string ResultFile
        {
            get { return _resultFile; }
            private set { _resultFile = value; }
        }
    }

    public class GZipManagerCompress : GZipManager
    {
        public GZipManagerCompress(string inputFile, string outputfile) : base( inputFile, outputfile)
        {
            _driver = new GzipDriverCompress();
        }
        public override string Act
        {
            get { return Compress; }
        }
    }
    public class GZipManagerDecompress : GZipManager
    {
        public GZipManagerDecompress(string inputFile, string outputfile) : base(inputFile, outputfile)
        {
            _driver = new GzipDriverDecompress();
        }
        public override string Act
        {
            get { return Decompress; }
        }
    }

}
