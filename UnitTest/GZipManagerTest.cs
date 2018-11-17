using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using GZipTest;

namespace UnitTest
{
    [TestFixture]
    public class GZipManagerTest : TestBase
    {
        [TestCase("empty.txt")]
        [TestCase("small.txt")]
        [TestCase("4GB.mkv")]
        [TestCase("9GB.rar")]
        public void CompressFileAndDecompressToFile(string fileName)
        {
            string inputFile = Path.Combine(PathTotestFolder, fileName);

            string outputfile = inputFile+"_output";
            string gzip = inputFile + "_gz";

            IfExistDeleteFile(outputfile);
            IfExistDeleteFile(gzip);

            CompressFile(inputFile, gzip);
            DecompressFile(gzip, outputfile);

            CheckResult(inputFile, outputfile, gzip);
        }

        private void CompressFile(string inputFile, string gzip)
        {
            IManager zip = Manager.Factory(Command.Compress, inputFile, gzip);
            zip.Execute();
            Assert.IsTrue(zip.Exceptions().Count == 0);
        }

        private void DecompressFile(string gzip, string outputfile)
        {
            IManager zip = Manager.Factory(Command.Decompress, gzip, outputfile);
            zip.Execute();
            Assert.IsTrue(zip.Exceptions().Count == 0);
        }

        
    }
}