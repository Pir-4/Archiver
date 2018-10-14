using System;
using System.Collections.Generic;
using GZiptest;
using GZipTest.GZipDriver;

namespace GZipTest
{
    public abstract class Manager : IManager
    {
        protected IGzipDriver Driver;

        public static IManager Сreate(string act, string inputFile, string outputfile)
        {
            if (act.Equals(Command.Compress.ToString(), StringComparison.CurrentCultureIgnoreCase))
                return new ManagerGZipCompress(inputFile, outputfile);

            if (act.Equals(Command.Decompress.ToString(), StringComparison.CurrentCultureIgnoreCase))
                return new ManagerGZipDecompress(inputFile, outputfile);

            return null;
        }

        protected Manager(string inputFile, string outputfile)
        {
            SourceFile = inputFile;
            ResultFile = outputfile;
        }
        public void Execute()
        {
            Driver.Execute();
        }
        public List<Exception> Exceptions()
        {
            return Driver.Exceptions;
        }
        public abstract string Act { get; }

        public string SourceFile { get; private set; }

        public string ResultFile { get; private set; }
    }

    public class ManagerGZipCompress : Manager
    {
        public ManagerGZipCompress(string inputFile, string outputfile) : base(inputFile, outputfile)
        {
            Driver = new GzipDriverCompress(inputFile, outputfile);
        }
        public override string Act => Command.Compress.ToString();
    }

    public class ManagerGZipDecompress : Manager
    {
        public ManagerGZipDecompress(string inputFile, string outputfile) : base(inputFile, outputfile)
        {
            Driver = new GzipDriverDecompress(inputFile, outputfile);
        }
        public override string Act => Command.Decompress.ToString();
    }

}