using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipTest.Drivers
{
    public class DriverGzipDecompress : DriverGZip
    {
        public DriverGzipDecompress(string inputPath, string outputPath) : base(inputPath, outputPath)
        {
        }

        protected override int GetBlockLength(Stream stream)
        {
            var startPosition = stream.Position;
            var blockLengthBytes = new byte[8];
            stream.Read(blockLengthBytes, 0, blockLengthBytes.Length);
            var blockLength = BitConverter.ToInt32(blockLengthBytes, 4);
            stream.Position = startPosition;
            return blockLength;
        }

        protected override byte[] ProcessBlcok(byte[] input)
        {
            using (var sourceStream = new MemoryStream(input))
            {
                using (var targetStream = new MemoryStream())
                {
                    using (var decompressionStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(targetStream);
                        return targetStream.ToArray();
                    }
                }
            }
        }
    }
}
