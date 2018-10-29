using System;
using System.IO;
using System.Security.Cryptography;


namespace GZipTest.Drivers
{
    public class DriverSha256 : Driver
    {
        public int BlockSize { get; private set; }

        public DriverSha256(string inputPath, int blcokSize ) : base(inputPath, "")
        {
            BlockSize = blcokSize <= 0 ? 1024 * 1024 : blcokSize;
        }

        protected override int GetBlockLength(Stream stream)
        {
            return (int)Math.Min(BlockSize, stream.Length - stream.Position);
        }

        protected override byte[] ProcessBloсk(byte[] input)
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
                    var hash = BitConverter.ToString(block).Replace("-", "").ToLowerInvariant();
                    Console.WriteLine($"Hash #{id} '{hash}'");
                }
            }
        }
    }
}