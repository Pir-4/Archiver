using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZipTest.Drivers
{
    public abstract class DriverGZip : Driver
    {
        public const int BlockSize = 1024 * 1024;

        protected DriverGZip(string inputPath, string outputPath) : base(inputPath, outputPath)
        {}

        protected override void WriteBlock()
        {
            var expectedId = 0;
            using (var outputStrem = new FileStream(ResultFile, FileMode.Append))
            {
                while (!IsComplited && expectedId < MaxCountReadedBlocks)
                {
                    byte[] block;
                    long id;
                    if (WriteQueue.TryGetValue(out block, out id))
                    {
                        expectedId++;
                        outputStrem.Write(block, 0, block.Length);
                        outputStrem.Flush(true);
                    }
                }
            }
        }
    }
}