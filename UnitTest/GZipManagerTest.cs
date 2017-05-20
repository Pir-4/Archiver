using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VeeamSoftware_test.Gzip;

namespace UnitTest
{
    [TestClass]
    public class GZipManagerTest
    {

        [TestMethod]
        public void CompressFileAndDecompressToFile()
        {
            string inputFile = @"E:\education\programs\test\testttt\dfdf.txt";

            string outputfile = @"E:\education\programs\test22.txt";
            string gzip = @"E:\education\programs\test.gzip";

            IGZipManager zip = new GZipManagerCompress(inputFile, gzip);
            zip.Execute();
            Assert.IsTrue(zip.Exceptions().Count == 0);

            zip = new GZipManagerDecompress(gzip, outputfile);
            zip.Execute();
            Assert.IsTrue(zip.Exceptions().Count == 0);

            FileInfo Etalon = new FileInfo(inputFile);
            FileInfo create = new FileInfo(outputfile);
            Assert.IsTrue(Etalon.Length == create.Length);

            File.Delete(outputfile);
            File.Delete(gzip);
        }
        [TestMethod]
        public void CompressFileAndDecompressToFatFile()
        {
            string inputFile = @"E:\education\programs\programs.rar";

            string outputfile = @"E:\education\programs\test22.txt";
            string gzip = @"E:\education\programs\test.gzip";

            IGZipManager zip = new GZipManagerCompress(inputFile, gzip);
            zip.Execute();
            Assert.IsTrue(zip.Exceptions().Count == 0);

            zip = new GZipManagerDecompress(gzip, outputfile);
            zip.Execute();
            Assert.IsTrue(zip.Exceptions().Count == 0);

            FileInfo Etalon = new FileInfo(inputFile);
            FileInfo create = new FileInfo(outputfile);
            Assert.IsTrue(Etalon.Length == create.Length);

            File.Delete(outputfile);
            File.Delete(gzip);
        }
        [TestMethod]
        public void CompressFileToFatFile()
        {
            string inputFile = @"E:\education\programs\programs.rar";
            string gzip = @"E:\education\programs\test.gzip";

            IGZipManager zip = new GZipManagerCompress(inputFile, gzip);
            zip.Execute();
            Assert.IsTrue(zip.Exceptions().Count == 0);

            File.Delete(gzip);
        }
    }
}
