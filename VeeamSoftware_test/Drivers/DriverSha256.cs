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
    public class DriverSha256 : Driver
    {
        public int BlockSize { get; private set; }

        public DriverSha256(string inputPath, int blcokSize ) : base(inputPath, "")
        {
            BlockSize = blcokSize == 0 ? 10*1024*1024 : blcokSize;
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