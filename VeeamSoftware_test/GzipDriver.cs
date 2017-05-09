using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Runtime.Remoting.Messaging;
using System.Threading;

namespace VeeamSoftware_test.Gzip
{
    public interface IGzipDriver
    {
        void Execute(string inputPath, string outputPath);
    }
    public abstract class GzipDriver : IGzipDriver
    {
        protected const long BlockSize = 10*1024*1024;
        protected readonly int TreadCount;
        protected const int MaxQueueSize = 10;

        private readonly Thread sourceThread;
        private readonly Thread outputThread;

        protected GzipDriver()
        {
            TreadCount = Environment.ProcessorCount;
            sourceThread = new Thread(ReadStream);
            outputThread = new Thread(WriteStream);
        }
        public void Execute(string inputPath, string outputPath)
        {
            
        }
        protected abstract void ReadStream();

        private void WriteStream()
        {
            
        }
    }
}
