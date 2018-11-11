using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using GZipTest.ThreadPool;

namespace GZipTest.Drivers
{
    public abstract class Driver : IDriver
    {
        protected bool IsComplited;
        protected int MaxCountReadedBlocks = int.MaxValue;

        public string SourceFile { get; private set; }
        public string ResultFile { get; private set; }

        private readonly SyncronizedQueue<byte[]> _readQueue;
        protected readonly SyncronizedQueue<byte[]> WriteQueue;

        private readonly IMyThreadPool _threadPool;

        protected Driver(string inputPath, string outputPath)
        {
            SourceFile = inputPath;
            ResultFile = outputPath;

            _readQueue = new SyncronizedQueue<byte[]>();
            WriteQueue = new SyncronizedQueue<byte[]>();

            _threadPool = new MyThreadPool();
        }

        public void Execute()
        {
            _threadPool.Add(this.Read);
            _threadPool.Add(this.Write);

            var maxThreadCount = Environment.ProcessorCount - _threadPool.Count;
            maxThreadCount = maxThreadCount > 0 ? maxThreadCount : 1;
            Enumerable.Range(1, maxThreadCount).ToList().ForEach(_ => _threadPool.Add(this.Process));

            _threadPool.Execute();
        }

        public List<Exception> Exceptions { get; set; } = new List<Exception>();

        protected abstract int GetBlockLength(Stream stream);

        protected abstract byte[] ProcessBloсk(byte[] input);

        protected abstract void WriteBlock();

        private void Read()
        {
            try
            {
                var id = 0;
                using (var inputStream = File.OpenRead(SourceFile))
                {
                    while (!IsComplited && inputStream.Position < inputStream.Length)
                    {
                        var blockSize = GetBlockLength(inputStream);
                        var data = new byte[blockSize];
                        inputStream.Read(data, 0, data.Length);
                        _readQueue.Enqueue(data, id++);
                    }
                }
                MaxCountReadedBlocks = id;
            }
            catch (Exception e)
            {
                Complete();
                Exceptions.Add(e);
            }
        }

        private void Process()
        {
            try
            {
                while (!IsComplited)
                {
                    byte[] block;
                    long id;
                    if (_readQueue.TryGetValue(out block, out id))
                    {
                        var data = ProcessBloсk(block);
                        WriteQueue.Enqueue(data, id);
                    }
                }
            }
            catch (Exception e)
            {
                Complete();
                Exceptions.Add(e);
            }
        }

        private void Write()
        {
            try
            {
                this.WriteBlock();
            }
            catch (Exception e)
            {
                Exceptions.Add(e);
            }
            finally
            {
                Complete();
            }
        }

        private void Complete()
        {
            IsComplited = true;
            _readQueue.Break();
            WriteQueue.Break();
        }
    }
}