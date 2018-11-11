using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using GZipTest;

namespace UnitTest
{
    [TestFixture]
    public class GZipManagerTest
    {
        protected const string _pathTotestFolder = @"E:\education\programs\Veeam\test";

        [TestCase("empty.txt")]
        [TestCase("small.txt")]
        [TestCase("4GB.mkv")]
        [TestCase("9GB.rar")]
        public void CompressFileAndDecompressToFile(string fileName)
        {
            string inputFile = Path.Combine(_pathTotestFolder, fileName);

            string outputfile = inputFile+"_output";
            string gzip = inputFile + "_gz";

            IfExistDeleteFile(outputfile);
            IfExistDeleteFile(gzip);

            CompressFile(inputFile, gzip);
            DecompressFile(gzip, outputfile);

            CheckResult(inputFile, outputfile, gzip);
        }

        protected static void CheckResult(string inputFile, string outputfile, string gzip)
        {
            FileInfo input = new FileInfo(inputFile);
            FileInfo output = new FileInfo(outputfile);

            Assert.IsTrue(input.Length.Equals(output.Length),$"Length input {input.Length}, output {output.Length}");
            var inputHash = GetMd5OfFile(inputFile);
            var outputHash = GetMd5OfFile(outputfile);

            Assert.IsTrue(inputHash.Equals(outputHash), $"MD5 input '{inputHash}', output '{outputHash}'");

            File.Delete(outputfile);
            File.Delete(gzip);
        }

        private static void CompressFile(string inputFile, string gzip)
        {
            IManager zip = Manager.Factory(Command.Compress, inputFile, gzip);
            zip.Execute();
            Assert.IsTrue(zip.Exceptions().Count == 0);
        }

        private static void DecompressFile(string gzip, string outputfile)
        {
            IManager zip;
            zip = Manager.Factory(Command.Compress, gzip, outputfile);
            zip.Execute();
            Assert.IsTrue(zip.Exceptions().Count == 0);
        }

        protected static void IfExistDeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        protected static string GetMd5OfFile(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}