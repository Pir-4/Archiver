using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using VeeamSoftware_test.Gzip;

namespace VeeamSoftware_test.GZipDriver
{
    public class GzipDriverCompress : GzipDriver
    {
        protected const int BlockSize = 10 * 1024 * 1024;

        protected override int GetBlockLength(Stream stream)
        {
            return (int)Math.Min(BlockSize, stream.Length - stream.Position);
        }

        protected override byte[] ProcessBlcok(byte[] input)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var compressionStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    compressionStream.Write(input, 0, input.Length);
                }
                var data = memoryStream.ToArray();
                BitConverter.GetBytes(data.Length).CopyTo(data, 4);//TODO ?
            }
        }
    }
}
