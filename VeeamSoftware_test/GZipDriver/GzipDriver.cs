﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using VeeamSoftware_test.Gzip;

namespace VeeamSoftware_test.GZipDriver
{
    public abstract class GzipDriver : IGzipDriver
    {
        protected const int BlockSize = 10*1024*1024;

        private readonly Thread _sourceThread;
        private readonly Thread _outputThread;

        protected int countTreadsOfObject;
        protected IMyThreadPool _threadPool;
        protected QueueOrder<byte[]> _bufferQueue = new QueueOrder<byte[]>();
        private List<Exception> _exceptions = new List<Exception>();

        protected string _soutceFilePath;
        private string _outputFilePath;

        protected Stream sourceStream;

        private static readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        protected  AutoResetEvent _writeResetEvent = new AutoResetEvent(false);

        protected GzipDriver()
        {
            _sourceThread = new Thread(ReadStream);
            _outputThread = new Thread(WriteStream);

            countTreadsOfObject = 1;
            

        }

        public void Execute(string inputPath, string outputPath)
        {
            _soutceFilePath = inputPath;
            _outputFilePath = outputPath;

            _threadPool = new MyThreadPool(Environment.ProcessorCount - countTreadsOfObject);

            _sourceThread.Start();
            _outputThread.Start();

            _autoResetEvent.WaitOne();

            _sourceThread.Join();
            _outputThread.Join();
        }

        public List<Exception> Exceptions
        {
            get { return _exceptions; }
            protected set { _exceptions = value; }
        }

        protected abstract void ReadStream();

        private void WriteStream()
        {
            try
            {
                using (
                    FileStream outputStream = new FileStream(_outputFilePath, FileMode.Create, FileAccess.Write,
                        FileShare.Read, BlockSize, FileOptions.Asynchronous))
                {
                    while (_sourceThread.IsAlive || _bufferQueue.Size > 0 || _threadPool.isWork)
                    {
                        if (isBreak)
                            break;

                        byte[] buffer;
                        if (_bufferQueue.TryGetValue(out buffer))
                        {
                            outputStream.Write(buffer, 0, buffer.Length);
                            outputStream.Flush();
                            _bufferQueue.Release();
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
                _threadPool.Free();
                sourceStream.Close();
                _autoResetEvent.Set();
            }
        }

        protected bool isBreak
        {
            get { return Exceptions.Count != 0; }
        }
    }

    

    
}
