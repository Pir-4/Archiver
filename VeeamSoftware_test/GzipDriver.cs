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
        private readonly Thread _sourceThread;
        private readonly Thread _outputThread;

        protected readonly ThreadDispatcher _threadDispatcher;
        protected QueueOrder<byte[]> _bufferQueue = new QueueOrder<byte[]>();
        protected List<Exception> _exceptions = new List<Exception>();

        protected string _soutceFilePath;
        protected string _outputFilePath;

        private static readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        protected GzipDriver()
        {
            _sourceThread = new Thread(ReadStream);
            _outputThread = new Thread(WriteStream);

            _threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);
        }
        public void Execute(string inputPath, string outputPath)
        {
            _soutceFilePath = inputPath;
            _outputFilePath = outputPath;

            _sourceThread.Start();
            _outputThread.Start();
            _autoResetEvent.WaitOne();
        }
        public List<Exception> Exceptions
        {
            get { return _exceptions; }
        }
        protected abstract void ReadStream();

        private void WriteStream()
        {
            try
            {
                using (
                    FileStream outputStream = new FileStream(_outputFilePath, FileMode.Create, FileAccess.Write,
                        FileShare.Read))
                {
                    while (_sourceThread.IsAlive || _bufferQueue.Size > 0 || !_threadDispatcher.isEmpty)
                    {
                        if (isBreak)
                            break;

                        byte[] buffer;
                        if (_bufferQueue.TryGetValue(out buffer))
                        {

                            outputStream.Write(buffer, 0, buffer.Length);
                            outputStream.Flush();
                            GC.Collect();
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                _exceptions.Add(ex);
            }
            finally
            {
                _autoResetEvent.Set();
            }
        }
        protected bool isBreak
        {
            get { return _exceptions.Count != 0; }
        }
    }
}
