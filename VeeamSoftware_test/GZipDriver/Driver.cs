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

namespace GZipTest.GZipDriver
{
    public abstract class Driver : IDriver
    {
        private bool _isComplited;
        private int _maxCountReadedBlocks = int.MaxValue;

        protected string SourceFilePath;
        protected string OutputFilePath;

        private readonly SyncronizedQueue<byte[]> _readQueue;
        private readonly SyncronizedQueue<byte[]> _writeQueue;

        private readonly IMyThreadPool _threadPool;

        protected Driver(string inputPath, string outputPath)
        {
            SourceFilePath = inputPath;
            OutputFilePath = outputPath;

            _readQueue = new SyncronizedQueue<byte[]>();
            _writeQueue = new SyncronizedQueue<byte[]>();

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

        protected abstract byte[] ProcessBlcok(byte[] input);

        private void Read()
        {
            try
            {
                var id = 0;
                using (var inputStream = File.OpenRead(SourceFilePath))
                {
                    while (!_isComplited && inputStream.Position < inputStream.Length)
                    {
                        var blockSize = GetBlockLength(inputStream);
                        var data = new byte[blockSize];
                        inputStream.Read(data, 0, data.Length);
                        _readQueue.Enqueue(data, id++);
                    }
                }
                _maxCountReadedBlocks = id;
            }
            catch (Exception e)
            {
                _isComplited = true;
                Exceptions.Add(e);
            }
        }

        private void Process()
        {
            try
            {
                while (!_isComplited)
                {
                    byte[] block;
                    long id;
                    if (_readQueue.TryGetValue(out block, out id))
                    {
                        var data = ProcessBlcok(block);
                        _writeQueue.Enqueue(data, id);
                    }
                }
            }
            catch (Exception e)
            {
                _isComplited = true;
                Exceptions.Add(e);
            }
        }

        private void Write()
        {
            try
            {
                var expectedId = 0;
                using (var outputStrem = new FileStream(OutputFilePath, FileMode.Append))
                {
                    while (!_isComplited && expectedId < _maxCountReadedBlocks)
                    {
                        byte[] block;
                        long id;
                        if (_writeQueue.TryGetValue(out block, out id))
                        {
                            expectedId++;
                            outputStrem.Write(block, 0, block.Length);
                            outputStrem.Flush(true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Exceptions.Add(e);
            }
            finally
            {
                _isComplited = true;
            }
        }
    }
}