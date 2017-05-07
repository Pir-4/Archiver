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

    public abstract class GzipDriver
    {
        private Queue<byte[]> _queue = new Queue<byte[]>();

        
        private Semaphore _semaphoreWrite = new Semaphore(0, Int32.MaxValue);
        private Semaphore _semaphoreCheckWriteOutputStream = new Semaphore(0, 1);

        
        private Semaphore _semaphoreRead = new Semaphore(10, 3000);
        private Semaphore _semaphoreCheckReadInputStream = new Semaphore(0, 1);

        private object _lockerWrite = new object();
        private object _lockerRead = new object();

        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private const int _bufferSize = 1024*1024*1024;
        private const long _maxSizeStream = 4294967296;

        private  Exception _exception;

        protected Stream _sourceStream;
        protected Stream _outputStream;

        protected  string _pathSourceFile;
        protected string _pathOutputFile;

        private long _sizeMemorySizeOutStream;
        private long _sizeMemorySourceStream;

        public GzipDriver(string pathPathSourceFile, string pathPathOutputFile)
        {
            _pathSourceFile = pathPathSourceFile;
            _pathOutputFile = pathPathOutputFile;
        }

        public void Run()
        {
           int  _processCount = Environment.ProcessorCount > 2 ? Environment.ProcessorCount : 2;
            Thread[] _threads = new Thread[_processCount];

            try
            {
                InitStream();
                for (int i = 0; i < _processCount; i++)
                {
                    if (i%2 == 0)
                    {
                        _threads[i] = new Thread(Write);
                        _threads[i].Start();
                    }
                    else
                    {
                        _threads[i] = new Thread(Read);
                        _threads[i].Start();
                    }

                }
                _autoResetEvent.WaitOne();
            }
            catch (Exception e)
            {

                throw e;
            }
            finally
            {
                for (int i = 0; i < _processCount; i++)
                    _threads[i].Join();

                _sourceStream.Close();
                _outputStream.Close();
            }

            if (_exception != null)
                throw _exception;
        }

        private  void PushBlock(byte[] buffer)
        {
            lock (_lockerWrite)
            {
                _queue.Enqueue(buffer);
                _semaphoreWrite.Release();
            }
        }

        private  void Read()
        {
            try
            {
                while (true)
                {
                    
                    checkInputStream();
                    _semaphoreCheckReadInputStream.WaitOne();
                    var buffer = new byte[_bufferSize];
                    lock (_lockerRead)
                    {
                        int bytesCount = _sourceStream.Read(buffer, 0, _bufferSize);
                        if (bytesCount == 0)
                        {
                            PushBlock(null);
                            break;
                        }
                        
                        buffer = bytesCount < _bufferSize ? buffer.Take(bytesCount).ToArray() : buffer;
                        _sizeMemorySourceStream += buffer.Length; 
                        PushBlock(buffer);
                    }
                }
            }
            catch (Exception e)
            {

                _exception = e;
            }
        }
        private  void Write()
        {
            try
            {
                while (true)
                {
                    checkOutputStream();
                    _semaphoreCheckWriteOutputStream.WaitOne();
                    lock (_lockerWrite)
                    {
                        byte[] buffer = _queue.Dequeue();

                        if (buffer == null)
                        {
                            _autoResetEvent.Set();
                            break;
                        }

                        _outputStream.Write(buffer, 0, buffer.Length);
                        _sizeMemorySizeOutStream += buffer.Length;
                        _semaphoreRead.Release();
                    }
                }
            }
            catch (Exception e)
            {

                _exception = e;
            }
            

        }

        protected void checkOutputStream()
        {
            _semaphoreWrite.WaitOne();
            lock (_lockerWrite)
            {
                if (_sizeMemorySizeOutStream + _queue.Peek().Length >= _maxSizeStream)
                {
                    _outputStream.Close();
                    _outputStream = GetOutputStream();
                    _sizeMemorySizeOutStream = 0;
                }
                _semaphoreCheckWriteOutputStream.Release();
            }
        }

        protected  void checkInputStream()
        {
            _semaphoreRead.WaitOne();
            lock (_lockerRead)
            {
                if (_sizeMemorySourceStream + _bufferSize >= 1294967296)
                {
                    long position = _sourceStream.Position;
                    _sourceStream.Close();
                    _sourceStream = GetSourceStream();
                    _sourceStream.Position = position;
                    _sizeMemorySourceStream = 0;

                }
                _semaphoreCheckReadInputStream.Release();
            }
        }

        protected abstract void InitStream();
        protected abstract Stream GetOutputStream();
        protected abstract Stream GetSourceStream();

    }

    public class GzipDriverCompress : GzipDriver
    {
        public GzipDriverCompress(string pathPathSourceFile, string pathPathOutputFile) : base(pathPathSourceFile, pathPathOutputFile)
        {
            
        }

        protected override void InitStream()
        {
            _sourceStream = new FileStream(_pathSourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            _outputStream =
                new GZipStream(new FileStream(_pathOutputFile, FileMode.Create, FileAccess.Write, FileShare.Read),
                    CompressionMode.Compress);
        }

        protected override Stream GetOutputStream()
        {
            return new GZipStream(new FileStream(_pathOutputFile, FileMode.Append, FileAccess.Write, FileShare.Read), CompressionMode.Compress);
        }

        protected override Stream GetSourceStream()
        {
            return new FileStream(_pathSourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }
    public class GzipDriverDecompress : GzipDriver
    {
        public GzipDriverDecompress(string pathPathSourceFile, string pathPathOutputFile) : base(pathPathSourceFile, pathPathOutputFile)
        {}

        protected override void InitStream()
        {
            _sourceStream =
                new GZipStream(new FileStream(_pathSourceFile, FileMode.Open, FileAccess.Read, FileShare.Read),
                    CompressionMode.Decompress);

            _outputStream = new FileStream(_pathOutputFile, FileMode.Create, FileAccess.Write, FileShare.Read);
        }
        protected override Stream GetOutputStream()
        {
            return new FileStream(_pathOutputFile, FileMode.Append, FileAccess.Write, FileShare.Read);
        }
        protected override Stream GetSourceStream()
        {
            return new GZipStream(new FileStream(_pathSourceFile, FileMode.Open, FileAccess.Read, FileShare.Read),
                CompressionMode.Decompress);
        }
    }
}
