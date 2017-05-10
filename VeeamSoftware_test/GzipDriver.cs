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
        List<Exception> Exceptions { get; }
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
        private string _outputFilePath;

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

    public class GzipDriverCompress : GzipDriver
    {
        private object _locker = new object();

        protected override void ReadStream()
        {
            try
            {
                FileInfo fileInfo = new FileInfo(_soutceFilePath);
                for (int i = 0; i < (int)Math.Ceiling((double)fileInfo.Length / BlockSize); i++)
                {
                    if (isBreak)
                        break;

                    int blockIndex = i;
                    long currentPosition = i * BlockSize;
                    int blockLength = (int)Math.Min(BlockSize, fileInfo.Length - currentPosition);
                    _threadDispatcher.Start(() => CompressBlock(currentPosition, blockLength, blockIndex));
                }
            }
            catch (Exception ex)
            {

                _exceptions.Add(ex);
            }

        }
        private void CompressBlock(long startPosition, int blockLength, int blockIndex)
        {
            try
            {
                byte[] readBuffer = new byte[blockLength];
                using (var sourceStream = new FileStream(_soutceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    sourceStream.Seek(startPosition, SeekOrigin.Begin);
                    sourceStream.Read(readBuffer, 0, readBuffer.Length);
                }

                byte[] comressBuffer;
                lock (_locker)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                        {
                            gzipStream.Write(readBuffer, 0, readBuffer.Length);
                        }
                        comressBuffer = memoryStream.GetBufferWithoutZeroTail();
                    }
                }

                _bufferQueue.Enqueue(blockIndex, comressBuffer);

                GC.Collect();
            }
            catch (Exception ex)
            {

                _exceptions.Add(ex);
            }
        }
    }

    public class GzipDriverDecompress : GzipDriver
    {
        private readonly byte[] gzipHeader = new byte[] { 31, 139, 8, 0, 0, 0, 0, 0, 4, 0 };
        private const int BlockSizeRead = 1024 * 1024;

        protected override void ReadStream()
        {
            try
            {
                using (var stream = new FileStream(_soutceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (!stream.StartsWith(gzipHeader))
                    {
                        DecompressBlock(0, 0);
                        return;
                    }


                    int blockIndex = 0;
                    while (stream.Position < stream.Length)
                    {
                        if (isBreak)
                            break;

                        var nextBlockIndex = stream.GetFirstBufferIndex(gzipHeader, BlockSizeRead);
                        if (nextBlockIndex == -1)
                        {
                            break;
                        }
                        int tmpBlcokIndex = blockIndex;
                        _threadDispatcher.Start(() => DecompressBlock(nextBlockIndex, tmpBlcokIndex));
                        blockIndex++;
                        GC.Collect();
                    }

                }
            }
            catch (Exception ex)
            {

                _exceptions.Add(ex);
            }

        }
        private void DecompressBlock(long startPosition, int blockIndex)
        {
            try
            {
                using (var inputStream = new FileStream(_soutceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    inputStream.Seek(startPosition, SeekOrigin.Begin);
                    using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress, true))
                    {
                        int bufferNumber = 0;
                        byte[] buffer = new byte[BlockSize];
                        int bytesread = gzipStream.Read(buffer, 0, buffer.Length);
                        if (bytesread < BlockSize)
                            Array.Resize(ref buffer, bytesread);


                        while (bytesread > 0)
                        {
                            if (isBreak)
                                break;

                            byte[] nextBuffer = new byte[BlockSize];
                            bytesread = gzipStream.Read(nextBuffer, 0, nextBuffer.Length);
                            if (bytesread < BlockSize)
                                Array.Resize(ref nextBuffer, bytesread);

                            _bufferQueue.Enqueue(blockIndex, bufferNumber, buffer, nextBuffer.Length == 0);
                            buffer = nextBuffer;
                            bufferNumber++;
                            GC.Collect();
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                _exceptions.Add(ex);
            }

        }
    }
}
