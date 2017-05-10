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
        protected const int MaxQueueSize = 10;

        private readonly Thread _sourceThread;
        private readonly Thread _outputThread;

        protected readonly ThreadDispatcher _threadDispatcher;
        protected QueueOrder<byte[]> _bufferQueue = new QueueOrder<byte[]>();
        protected Semaphore _bufferQueueSemaphore = new Semaphore(0, Int32.MaxValue);
        protected List<Exception> _exceptions = new List<Exception>();

        protected string _soutceFilePath;
        protected string _outputFilePath;

        protected GzipDriver()
        {
            _sourceThread = new Thread(ReadStream);
            _outputThread = new Thread(WriteStream);

            _threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);
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
