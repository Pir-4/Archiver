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
            string input_1 = @"E:\education\programs\test\testttt\dfdf.txt";

            string output = @"E:\education\programs\test22.txt";
            string input = @"E:\education\programs\test.gzip";

            IGZipManager zip = new GZipManagerCompress();
            zip.SourceFile = input_1;
            zip.ResultFile = input;
            zip.Execute();

            zip = new GZipManagerDecompress();
            zip.SourceFile = input;
            zip.ResultFile = output;
            zip.Execute();

            FileInfo Etalon = new FileInfo(input_1);
            FileInfo create = new FileInfo(output);
            Assert.IsTrue( Etalon.Length == create.Length);

            File.Delete(output);
            File.Delete(input);
        }
        [TestMethod]
        public void CompressFileAndDecompressToFatFile()
        {
            string input_1 = @"E:\education\programs\TonarinoTotoro.mkv";

            string output = @"E:\education\programs\test22.txt";
            string input = @"E:\education\programs\test.gzip";

            IGZipManager zip = new GZipManagerCompress();
            zip.SourceFile = input_1;
            zip.ResultFile = input;
            zip.Execute();

            zip = new GZipManagerDecompress();
            zip.SourceFile = input;
            zip.ResultFile = output;
            zip.Execute();

            FileInfo Etalon = new FileInfo(input_1);
            FileInfo create = new FileInfo(output);
            Assert.IsTrue(Etalon.Length == create.Length);

            File.Delete(output);
            File.Delete(input);
        }


    }
}
