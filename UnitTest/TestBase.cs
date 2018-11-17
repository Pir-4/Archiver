using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;

namespace UnitTest
{
    public class TestBase
    {
        protected const string PathTotestFolder = @"E:\education\programs\Veeam\test";

        protected void IfExistDeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        protected string GetMd5OfFile(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        protected void CheckResult(string inputFile, string outputfile, string gzip)
        {
            FileInfo input = new FileInfo(inputFile);
            FileInfo output = new FileInfo(outputfile);

            Assert.IsTrue(input.Length.Equals(output.Length), $"Length input {input.Length}, output {output.Length}");
            var inputHash = GetMd5OfFile(inputFile);
            var outputHash = GetMd5OfFile(outputfile);

            Assert.IsTrue(inputHash.Equals(outputHash), $"MD5 input '{inputHash}', output '{outputHash}'");

            File.Delete(outputfile);
            File.Delete(gzip);
        }
    }
}