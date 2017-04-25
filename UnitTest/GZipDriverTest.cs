using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VeeamSoftware_test.Gzip;

namespace UnitTest
{
    [TestClass]
    public class GZipDriverTest
    {
        [TestMethod]
        public void CompressDirectory()
        {
            string input = @"E:\education\programs\test";
            string output = @"E:\education\programs\test.gzip";

            CompressDirectory(input, output);

            Assert.IsTrue(File.Exists(output));
            File.Delete(output);
        }
        private void CompressDirectory(string input,string output)
        {
            IGzipDriver driver = new GzipDriverDirectory();
            driver.Compress(input, output);
        }
        [TestMethod]
        public void CompressFile()
        {
            string input = @"E:\education\programs\test\test.txt";
            string output = @"E:\education\programs\test.gzip";
            CompressFile(input, output);
            Assert.IsTrue(File.Exists(output));
                File.Delete(output);
        }
        private void CompressFile(string input, string output)
        {
            IGzipDriver driver = new GzipDriverDirectory();
            driver.Compress(input, output);
        }

        [TestMethod]
        public void DecompressToDirectory()
        {
            string input_1 = @"E:\education\programs\test";

            string output = @"E:\education\programs\test2";
            string input = @"E:\education\programs\test.gzip";

            CompressDirectory(input_1, input);
            IGzipDriver driver = new GzipDriverDirectory();
            driver.Decompress(input, output);

            Assert.IsTrue(Directory.Exists(output));

            Directory.Delete(output,true);
            File.Delete(input);
        }
        [TestMethod]
        public void DecompressToFile()
        {
            string input_1 = @"E:\education\programs\test\test.txt";

            string output = @"E:\education\programs\test2";
            string input = @"E:\education\programs\test.gzip";

            CompressFile(input_1, input);
            GzipDriver.Decompress(input, output);

            Assert.IsTrue(Directory.Exists(output));

            Directory.Delete(output, true);
            File.Delete(input);
        }
    }
}
