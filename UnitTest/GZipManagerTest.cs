using System.IO;
using NUnit.Framework;
using ve

namespace UnitTest
{
    [TestFixture]
    public class GZipManagerTest
    {

        [Test]
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

        [Test]
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

        [Test]
        public void CompressFileToFatFile()
        {
            string inputFile = @"E:\education\programs\TonarinoTotoro.mkv";
            string gzip = @"E:\education\programs\test.gzip";

            IGZipManager zip = new GZipManagerCompress(inputFile, gzip);
            zip.Execute();
            Assert.IsTrue(zip.Exceptions().Count == 0);

            File.Delete(gzip);
        }

        [Test]
        public void DecompressFileToFatFile()
        {
            string outputFile = @"E:\education\programs\TonarinoTotoro2.mkv";
            string gzip = @"E:\education\programs\test2.gzip";

            IGZipManager zip = new GZipManagerDecompress(gzip, outputFile);
            zip.Execute();
            Assert.IsTrue(zip.Exceptions().Count == 0);

            File.Delete(outputFile);
        }
    }
}
