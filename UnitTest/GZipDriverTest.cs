using System;
using System.IO;
using System.IO.Compression;
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
            string[] sFiles = Directory.GetFiles(input, "*.*", SearchOption.AllDirectories);
            int iDirLen = input[input.Length - 1] == Path.DirectorySeparatorChar ? input.Length : input.Length + 1;

            using (FileStream outFile = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None))
            using (GZipStream str = new GZipStream(outFile, CompressionMode.Compress))
            {
                GzipDriver driver = new GzipDriver();
                driver.CompressDir = input;
                GzipDriver.ZipStream = str;

                foreach (string sFilePath in sFiles)
                {
                    string sRelativePath = sFilePath.Substring(iDirLen);
                    driver.CompressFileName = sRelativePath;
                    driver.CompressFile();
                    //GzipDriver.CompressFile(input, sRelativePath, str);
                }
            }
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
            using (FileStream outFile = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None))
            using (GZipStream str = new GZipStream(outFile, CompressionMode.Compress))
            {
                string sRelativePath = Path.GetFileName(input);

                GzipDriver driver = new GzipDriver();
                driver.CompressDir = Path.GetDirectoryName(input);
                driver.CompressFileName = sRelativePath;
                GzipDriver.ZipStream = str;
                driver.CompressFile();
            }
        }

        [TestMethod]
        public void DecompressToDirectory()
        {
            string input_1 = @"E:\education\programs\test";

            string output = @"E:\education\programs\test2";
            string input = @"E:\education\programs\test.gzip";

            CompressDirectory(input_1, input);
            Decompress(input, output);

            Assert.IsTrue(Directory.Exists(output));

            Directory.Delete(output,true);
            File.Delete(input);
        }

        public void Decompress(string input, string output)
        {
            using (FileStream inFile = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.None))
            using (GZipStream zipStream = new GZipStream(inFile, CompressionMode.Decompress, true))
            {
                GzipDriver driver = new GzipDriver();
                GzipDriver.ZipStream = zipStream;
                driver.DeCompressDir = output;

                while (driver.DecompressFile()) ;
            }
        }
        [TestMethod]
        public void DecompressToFile()
        {
            string input_1 = @"E:\education\programs\test\test.txt";

            string output = @"E:\education\programs\test2";
            string input = @"E:\education\programs\test.gzip";

            CompressFile(input_1, input);
            Decompress(input, output);

            Assert.IsTrue(Directory.Exists(output));

            Directory.Delete(output, true);
            File.Delete(input);
        }
    }
}
