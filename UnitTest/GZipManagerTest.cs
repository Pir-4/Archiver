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
        public void CompressDirectory()
        {
            string input = @"E:\education\programs\test";
            string output = @"E:\education\programs\test.gzip";

            Compress(input, output);

            Assert.IsTrue(File.Exists(output));
            File.Delete(output);
        }
        private IGZipManager Compress(string input, string output)
        {
            IGZipManager manager = new GZipManager();
            manager.Compress(input, output);

            return manager;
        }
        [TestMethod]
        public void CompressFile()
        {
            string input = @"E:\education\programs\test\test.txt";
            string output = @"E:\education\programs\test.gzip";
            Compress(input, output);
            Assert.IsTrue(File.Exists(output));
            File.Delete(output);
        }

        [TestMethod]
        public void DecompressToDirectory()
        {
            string input_1 = @"E:\education\programs\test";

            string output = @"E:\education\programs\test2";
            string input = @"E:\education\programs\test.gzip";

            Compress(input_1, input).Decompress(input, output);

            Assert.IsTrue(Directory.Exists(output));

            Directory.Delete(output, true);
            File.Delete(input);
        }
        [TestMethod]
        public void DecompressToFile()
        {
            string input_1 = @"E:\education\programs\test\test.txt";

            string output = @"E:\education\programs\test2";
            string input = @"E:\education\programs\test.gzip";

            Compress(input_1, input).Decompress(input, output);

            Assert.IsTrue(Directory.Exists(output));

            Directory.Delete(output, true);
            File.Delete(input);
        }
    }
}
