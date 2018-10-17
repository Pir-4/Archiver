using System;
using System.Collections.Generic;
using GZipTest;
using GZipTest.Drivers;

namespace GZipTest
{
    public class Manager : IManager
    {
        protected IDriver Driver;

        public static IManager Factory(string act, string inputFile, string outputfile, int blockSize = 0)
        {
            if (act.Equals(Command.Compress.ToString(), StringComparison.CurrentCultureIgnoreCase))
                return new Manager( new DriverGzipCompress(inputFile, outputfile), Command.Compress);

            if (act.Equals(Command.Decompress.ToString(), StringComparison.CurrentCultureIgnoreCase))
                return new Manager(new DriverGzipDecompress(inputFile, outputfile), Command.Decompress);

            if (act.Equals(Command.Sha256.ToString(), StringComparison.CurrentCultureIgnoreCase))
                return new Manager(new DriverSha256(inputFile, blockSize), Command.Sha256);

            return null;
        }

        protected Manager(IDriver driver, Command command)
        {
            Driver = driver;
            Act = command.ToString();
        }

        public void Execute()
        {
            Driver.Execute();
        }

        public List<Exception> Exceptions()
        {
            return Driver.Exceptions;
        }
        public string Act { get; private set; }

        public string SourceFile => Driver.SourceFile;

        public string ResultFile => Driver.ResultFile;
    }
}