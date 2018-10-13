﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;
using VeeamSoftware_test.GZipDriver;

namespace VeeamSoftware_test.Gzip
{
    public abstract class GZipManager : IGZipManager
    {
        public const string Compress = "compress";
        public const string Decompress = "decompress";

        protected IGzipDriver Driver;

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

        protected GZipManager(string inputFile, string outputfile)
        {
            SourceFile = inputFile;
            ResultFile = outputfile;
        }
        public void Execute()
        {
            Driver.Execute(SourceFile, ResultFile);
        }
        public List<Exception> Exceptions()
        {
            return Driver.Exceptions;
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
            Driver = new GzipDriverCompress();
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
            Driver = new GzipDriverDecompress();
        }
        public override string Act
        {
            get { return Decompress; }
        }
    }

}
