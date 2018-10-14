using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GZipTest;

namespace UnitTest
{
    [TestClass]
    public class ProgrammTest
    {
        [TestMethod]
        public void NullArgv()
        {
            Assert.IsTrue(Program.Main(null).Equals(1));
        }
        [TestMethod]
        public void EmptyArgv()
        {
            Assert.IsTrue(Program.Main(new string[] {}).Equals(1));
        }
        [TestMethod]
        public void LotOfArgv()
        {
            Assert.IsTrue(Program.Main(new string[] {"","","","" }).Equals(1));
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
            Assert.IsTrue(Program.Main(new string[] { "compress", @"E:\1.lol", "" }).Equals(1));
        }
        [TestMethod]
        public void NotCorretOutputFileNameArgv()
        {
            Assert.IsTrue(Program.Main(new string[] { "compress", @"E:\education\programs\test\test.txt", @"U:\Tgt" }).Equals(1));
        }
        [TestMethod]
        public void CorreteArgv()
        {
            string compressFile = @"E:\education\programs\test\test.txt";
            string zipFile = @"E:\education\programs\test\test.gzip";
            string controllFile = @"E:\education\programs\test\testControll.txt";

            Assert.IsTrue(Program.Main(new string[] { "compress", compressFile, zipFile }).Equals(0));

            Assert.IsTrue(Program.Main(new string[] { "decompress", zipFile, controllFile }).Equals(0));

            FileInfo Etalon = new FileInfo(compressFile);
            FileInfo create = new FileInfo(controllFile);
            Assert.IsTrue(Etalon.Length == create.Length);

            File.Delete(zipFile);
            File.Delete(controllFile);
        }
    }
}
