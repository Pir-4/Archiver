﻿using System;
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
        protected Queue<byte[]> _queue = new Queue<byte[]>();

        
        protected Semaphore _semaphoreWrite = new Semaphore(0, Int32.MaxValue);
        protected Semaphore _semaphoreCheckWriteOutputStream = new Semaphore(0, 1);

        
        protected Semaphore _semaphoreRead = new Semaphore(10, 3000);
        protected Semaphore _semaphoreCheckReadInputStream = new Semaphore(0, 1);

        protected object _lockerWrite = new object();
        protected object _lockerRead = new object();

        AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        protected const int _bufferSize = 1024*1024*1024;
        private  Exception exception;

        protected Stream sourceStrem;
        protected Stream outputStream;

        protected  string sourceFile;
        protected string outputFile;

        protected long sizeOutStream;
        protected long sizeInStream;

        public GzipDriver(string pathSourceFile, string pathOutputFile)
        {
            sourceFile = pathSourceFile;
            outputFile = pathOutputFile;
        }

        public void ModificationOfData()
        {
           int  _processCount = Environment.ProcessorCount > 2 ? Environment.ProcessorCount : 2;
            Thread[] _threads = new Thread[_processCount];

            try
            {
                initStream();
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
                autoResetEvent.WaitOne();
            }
            catch (Exception e)
            {

                throw e;
            }
            finally
            {
                for (int i = 0; i < _processCount; i++)
                    _threads[i].Join();

                sourceStrem.Close();
                outputStream.Close();
            }

            if (exception != null)
                throw exception;
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
                        int bytesCount = sourceStrem.Read(buffer, 0, _bufferSize);
                        if (bytesCount == 0)
                        {
                            PushBlock(null);
                            break;
                        }
                        
                        buffer = bytesCount < _bufferSize ? buffer.Take(bytesCount).ToArray() : buffer;
                        sizeInStream += buffer.Length; 
                        PushBlock(buffer);
                    }
                }
            }
            catch (Exception e)
            {

                exception = e;
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
                            autoResetEvent.Set();
                            break;
                        }

                        outputStream.Write(buffer, 0, buffer.Length);
                        sizeOutStream += buffer.Length;
                        _semaphoreRead.Release();
                    }
                }
            }
            catch (Exception e)
            {

                exception = e;
            }
            

        }

        protected abstract void checkOutputStream();
        protected abstract void checkInputStream();
        protected abstract void initStream();

    }

    public class GzipDriverCompress : GzipDriver
    {
        public GzipDriverCompress(string pathSourceFile, string pathOutputFile) : base(pathSourceFile, pathOutputFile)
        {
            
        }

        protected override void initStream()
        {
            sourceStrem = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            outputStream =
                new GZipStream(new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.Read),
                    CompressionMode.Compress);
        }
        protected override void checkOutputStream()
        {
            _semaphoreWrite.WaitOne();
            lock (_lockerWrite)
            {
                if (sizeOutStream + _queue.Peek().Length >= 1294967296)
                {
                    outputStream.Close();
                    outputStream = new GZipStream(new FileStream(outputFile, FileMode.Append, FileAccess.Write, FileShare.Read), CompressionMode.Compress);
                    sizeOutStream = 0;
                }
                _semaphoreCheckWriteOutputStream.Release();
            }

        }

        protected override void checkInputStream()
        {
            _semaphoreRead.WaitOne();
            lock (_lockerRead)
            {
                if (sizeInStream + _bufferSize >= 1294967296)
                {
                    long position = sourceStrem.Position;
                    sourceStrem.Close();
                    sourceStrem = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                    sourceStrem.Position = position;
                    sizeInStream = 0;

                }
                _semaphoreCheckReadInputStream.Release();
            }
        }
    }
    public class GzipDriverDecompress : GzipDriver
    {
        public GzipDriverDecompress(string pathSourceFile, string pathOutputFile) : base(pathSourceFile, pathOutputFile)
        {

        }

        protected override void initStream()
        {
            sourceStrem =
                new GZipStream(new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read),
                    CompressionMode.Decompress);

            outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.Read);
        }
        protected override void checkOutputStream()
        {
            _semaphoreWrite.WaitOne();
            lock (_lockerWrite)
            {
                if (sizeOutStream + _queue.Peek().Length >= 1294967296)
                {
                    outputStream.Close();
                    outputStream = new FileStream(outputFile, FileMode.Append, FileAccess.Write, FileShare.Read);
                    sizeOutStream = 0;
                }
                _semaphoreCheckWriteOutputStream.Release();
            }

        }

        protected override void checkInputStream()
        {
            _semaphoreRead.WaitOne();
            lock (_lockerRead)
            {
                if (sizeInStream + _bufferSize >= 1294967296)
                {
                    long position = sourceStrem.Position;
                    sourceStrem.Close();
                    sourceStrem = new GZipStream(new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read),
                    CompressionMode.Decompress); ;
                    sourceStrem.Position = position;
                    sizeInStream = 0;

                }
                _semaphoreCheckReadInputStream.Release();
            }
        }
    }
}
