using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GZipTest;

namespace UnitTest
{
    [TestClass]
    public class ProgrammTest : GZipManagerTest
    {

        [TestMethod]
        public void NullArgv()
        {
            Assert.IsTrue(Program.Main(null).Equals(1));
        }
        [TestMethod]
        public void EmptyArgv()
        {
            Assert.IsTrue(Program.Main(new string[] { }).Equals(1));
        }
        [TestMethod]
        public void LotOfArgv()
        {
            Assert.IsTrue(Program.Main(new string[] { "", "", "", "" }).Equals(1));
        }
        [TestMethod]
        public void BadParametersArgv()
        {
            Assert.IsTrue(Program.Main(new string[] { "To_compress", "", "" }).Equals(1));
            Assert.IsTrue(Program.Main(new string[] { "To_decompress", "", "" }).Equals(1));
        }
        [TestMethod]
        public void NotCorretSourceFileNameArgv()
        {
            Assert.IsTrue(Program.Main(new string[] { Command.Compress.ToString(), @"E:\1.lol", "" }).Equals(1));
        }
        [TestMethod]
        public void NotCorretOutputFileNameArgv()
        {
            Assert.IsTrue(Program.Main(new string[] { Command.Compress.ToString(), Path.Combine(_pathTotestFolder, "small.txt"), @"U:\Tgt" }).Equals(1));
        }
        [TestMethod]
        public void CorreteArgv()
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
    }
}
