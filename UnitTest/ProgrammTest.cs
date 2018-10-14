using System;
using System.IO;
using NUnit.Framework;
using GZipTest;

namespace UnitTest
{
    [TestFixture]
    public class ProgrammTest : GZipManagerTest
    {
        [TestCase(null)]
        public void NullArgv(string[] argv)
        {
            Assert.IsTrue(Program.Main(argv).Equals(1));
        }

        [TestCase]
        public void EmptyArgv()
        {
            Assert.IsTrue(Program.Main(new string[] { }).Equals(1));
        }

        [TestCase("To_compress", "", "")]
        [TestCase("To_decompress", "", "")]
        [TestCase("To_sha256", "", "")]
        [TestCase("Compress", @"E:\1.lol", "")]
        [TestCase("", "", "")]
        public void BadParametersArgv(string command, string argv1, string argv2)
        {
            Assert.IsTrue(Program.Main(new string[] { command, argv1, argv1 }).Equals(1));
        }

        [TestCase("Compress", @"U:\Tgt")]
        [TestCase("Sha256", @"U:\Tgt")]
        public void NotCorretSecondArgv(string command, string argv2)
        {
            Assert.IsTrue(Program.Main(new string[] { command, Path.Combine(_pathTotestFolder, "small.txt"), argv2}).Equals(1));
        }

        [TestCase]
        public void CorreteArgvToArchive()
        {
            string inputFile = Path.Combine(_pathTotestFolder, "small.txt");
            string zipFile = inputFile + "_gz";
            string decompressFile = inputFile +"output";

            IfExistDeleteFile(decompressFile);
            IfExistDeleteFile(zipFile);

            Assert.IsTrue(Program.Main(new string[] { Command.Compress.ToString(), inputFile, zipFile }).Equals(0));

            Assert.IsTrue(Program.Main(new string[] { Command.Compress.ToString(), zipFile, decompressFile }).Equals(0));

            CheckResult(inputFile, decompressFile, zipFile);
        }

        [TestCase(0)]
        [TestCase(1024)]
        public void CorreteArgvToSha256(int blockSize)
        {
            string inputFile = Path.Combine(_pathTotestFolder, "small.txt");
            Assert.IsTrue(Program.Main(new string[] { Command.Sha256.ToString(), inputFile, blockSize.ToString() }).Equals(0));
        }
    }
}
