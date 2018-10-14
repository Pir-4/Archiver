using System;
using System.Collections.Generic;
using GZipTest;
using GZipTest.GZipDriver;

namespace GZipTest
{
    public abstract class Manager : IManager
    {
        protected IDriver Driver;

        public static IManager Factory(string act, string inputFile, string outputfile)
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
            Driver = new DriverGzipCompress(inputFile, outputfile);
        }
        public override string Act => Command.Compress.ToString();
    }

    public class ManagerGZipDecompress : Manager
    {
        public ManagerGZipDecompress(string inputFile, string outputfile) : base(inputFile, outputfile)
        {
            Driver = new DriverGzipDecompress(inputFile, outputfile);
        }
        public override string Act => Command.Decompress.ToString();
    }

}