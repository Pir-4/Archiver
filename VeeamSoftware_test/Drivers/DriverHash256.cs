using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using GZipTest.Drivers;
using System.Security.Cryptography;

namespace GZipTest.Drivers
{
    public class DriverHash256 : Driver
    {
        public int BlockSize { get; set; } = 10 * 1024 * 1024;

        public DriverHash256(string inputPath, string outputPath) : base(inputPath, outputPath)
        {
        }

        protected override int GetBlockLength(Stream stream)
        {
            return (int)Math.Min(BlockSize, stream.Length - stream.Position);
        }

        protected override byte[] ProcessBlcok(byte[] input)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(input);
            }
        }

        protected override void WriteBlock()
        {
            var expectedId = 0;
            while (!IsComplited && expectedId < MaxCountReadedBlocks)
            {
                byte[] block;
                long id;
                if (WriteQueue.TryGetValue(out block, out id))
                {
                    expectedId++;
                    var hash = BitConverter.ToString(block);
                    Console.WriteLine($"Hash #{id} '{hash}'");
                }
            }
        }
    }
}
