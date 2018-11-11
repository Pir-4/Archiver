using System;
using System.Collections.Generic;
using GZipTest;
using GZipTest.Drivers;

namespace GZipTest
{
    public class Manager : IManager
    {
        protected IDriver Driver;

        public static IManager Factory(Command act, string inputFile, string outputfile, int blockSize = 0)
        {
            switch (act)
            {
                    case Command.Compress:
                        return new Manager(new DriverGzipCompress(inputFile, outputfile), act);
                    case Command.Decompress:
                        return new Manager(new DriverGzipDecompress(inputFile, outputfile), act);
                    case Command.Sha256:
                        return new Manager(new DriverSha256(inputFile, blockSize), act);
                default:
                    return null;
            }
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