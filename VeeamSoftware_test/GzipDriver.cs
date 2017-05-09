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
        protected Semaphore _semaphoreCheckReadInputStream = new Semaphore(0, 1);

        private object _lockerWrite = new object();
        private object _lockerRead = new object();

        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private const int _bufferSize = 10*1024*1024;
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
            int _processCount = Environment.ProcessorCount > 2 ? Environment.ProcessorCount : 2;
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
                if (buffer != null)
                {
                    buffer = modifData(buffer);
                    _semaphoreCheckReadInputStream.WaitOne();
                }

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

                    // _semaphoreCheckReadInputStream.WaitOne();
                    _semaphoreRead.WaitOne();
                    lock (_lockerRead)
                    {
                        var buffer = new byte[_bufferSize];
                        int bytesCount = _sourceStream.Read(buffer, 0, _bufferSize);
                        if (bytesCount == 0)
                        {
                            PushBlock(null);
                            break;
                        }
                        
                        buffer = bytesCount < _bufferSize ? buffer.Take(bytesCount).ToArray() : buffer;
                       // _sizeMemorySourceStream += buffer.Length; 
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
                    // checkOutputStream();
                    // _semaphoreCheckWriteOutputStream.WaitOne();
                    _semaphoreWrite.WaitOne();
                    lock (_lockerWrite)
                    {
                        byte[] buffer = _queue.Dequeue();

                        if (buffer == null)
                        {
                            _autoResetEvent.Set();
                            break;
                        }

                        _outputStream.Write(buffer, 0, buffer.Length);
                       // _sizeMemorySizeOutStream += buffer.Length;
                        _semaphoreRead.Release();
                    }
                }
            }
            catch (Exception e)
            {

                _exception = e;
            }
            

        }

       /* protected void checkOutputStream()
        {
            _semaphoreWrite.WaitOne();
            lock (_lockerWrite)
            {
                if (_queue.Peek() != null && 
                    _sizeMemorySizeOutStream + _queue.Peek().Length >= _maxSizeStream)
                {
                    _outputStream.Close();
 
                    _sizeMemorySizeOutStream = 0;
                }
                _semaphoreCheckWriteOutputStream.Release();
            }
        }*/

        /*protected  void checkInputStream()
        {
            _semaphoreRead.WaitOne();
            lock (_lockerRead)
            {
                if (_sizeMemorySourceStream + _bufferSize >= _maxSizeStream)
                {
                    long position = _sourceStream.Position;
                    _sourceStream.Close();
;                    _sourceStream.Position = position;
                    _sizeMemorySourceStream = 0;

                }
                _semaphoreCheckReadInputStream.Release();
            }
        }*/

        protected abstract void InitStream();

        protected virtual byte[] modifData(byte[] buffer)
        {
            return null;
        }

    }

    public class GzipDriverCompress : GzipDriver
    {
        public GzipDriverCompress(string pathPathSourceFile, string pathPathOutputFile) : base(pathPathSourceFile, pathPathOutputFile)
        {
            
        }

        protected override void InitStream()
        {
            _sourceStream = new FileStream(_pathSourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            _outputStream = new FileStream(_pathOutputFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            /*new GZipStream(new FileStream(_pathOutputFile, FileMode.Create, FileAccess.Write, FileShare.Read),
                CompressionMode.Compress);*/
        }
        protected  override byte[] modifData(byte[] buffer)
        {
            byte[] result = null;
            using (var memoryStream = new MemoryStream())
            {
                using (var compressStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    compressStream.Write(buffer, 0, buffer.Length);
                }

                memoryStream.Position = 0;
                result = new byte[memoryStream.Length];
                memoryStream.Read(result, 0, result.Length);
            }
            _semaphoreCheckReadInputStream.Release();
            return result;
        }
    }
    public class GzipDriverDecompress : GzipDriver
    {
        public GzipDriverDecompress(string pathPathSourceFile, string pathPathOutputFile) : base(pathPathSourceFile, pathPathOutputFile)
        {}

        protected override void InitStream()
        {
            _sourceStream = new FileStream(_pathSourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                /*new GZipStream(new FileStream(_pathSourceFile, FileMode.Open, FileAccess.Read, FileShare.Read),
                    CompressionMode.Decompress);*/

            _outputStream = new FileStream(_pathOutputFile, FileMode.Create, FileAccess.Write, FileShare.Read);
        }

    }
}
