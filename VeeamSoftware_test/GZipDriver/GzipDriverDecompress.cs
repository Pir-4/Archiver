using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace VeeamSoftware.GZipDriver
{
    public class GzipDriverDecompress : GzipDriver
    {
        protected override int GetBlockLength(Stream stream)
        {
            var startPosition = stream.Position;
            var blockLengthBytes = new byte[8];
            stream.Read(blockLengthBytes, 0, blockLengthBytes.Length);
            var blockLength = BitConverter.ToInt32(blockLengthBytes, 4);//TODO ?
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
