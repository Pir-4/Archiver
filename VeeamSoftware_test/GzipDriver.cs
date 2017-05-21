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
        protected const int BlockSize = 10*1024*1024;

        private readonly Thread _sourceThread;
        private readonly Thread _outputThread;

        protected readonly ThreadDispatcher _threadDispatcher;
        protected FixedThreadPool _threadPool;
        protected QueueOrder<byte[]> _bufferQueue = new QueueOrder<byte[]>();
        protected List<Exception> _exceptions = new List<Exception>();

        protected string _soutceFilePath;
        private string _outputFilePath;

        protected Stream sourceStream;

        private static readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        protected GzipDriver()
        {
            _sourceThread = new Thread(ReadStream);
            _outputThread = new Thread(WriteStream);

            _threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);
        }

        public void Execute(string inputPath, string outputPath)
        {
            _threadPool = new FixedThreadPool();

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
                        FileShare.Read, BlockSize, FileOptions.Asynchronous))
                {
                    while (_sourceThread.IsAlive || _bufferQueue.Size > 0 || !_threadPool.isEmpty
                        /*!_threadDispatcher.isEmpty*/)
                    {
                        if (isBreak)
                            break;

                        byte[] buffer;
                        if (_bufferQueue.TryGetValue(out buffer))
                        {

                            outputStream.Write(buffer, 0, buffer.Length);
                            outputStream.Flush();

                            // Размер буфера превышает ограничение сборщика мусора 85000 байтов, 
                            // необходимо вручную очистить данные буфера из Large Object Heap
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
                _threadPool.Stop();
                sourceStream.Close();
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
        protected override void ReadStream()
        {
            try
            {
                sourceStream = new FileStream(_soutceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BlockSize,
                    FileOptions.Asynchronous);
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < (int) Math.Ceiling((double) sourceStream.Length/BlockSize); i++)
                {
                    if (isBreak)
                        break;

                    int blockIndex = i;
                    byte[] readBuffer = new byte[BlockSize];
                    int bytesread = sourceStream.Read(readBuffer, 0, readBuffer.Length);
                    if (bytesread < BlockSize)
                        Array.Resize(ref readBuffer, bytesread);
                    _threadPool.Execute(new Task(() => CompressBlock(readBuffer, blockIndex)));
                }
            }
            catch (Exception ex)
            {

                _exceptions.Add(ex);
            }
            finally
            {


            }

        }

        /// <summary>
        /// Сжатие отдельного блока данных из входного файла
        /// </summary>
        /// <param name="startPosition">Смещение от начала файла</param>
        /// <param name="blockLength">Длина блока</param>
        /// <param name="blockIndex">Порядок блока</param>
        private void CompressBlock(byte[] readBuffer, int blockIndex)
        {
            try
            {
                // Сжимаем исходный массив байтов  
                byte[] comressBuffer;
                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                    {
                        gzipStream.Write(readBuffer, 0, readBuffer.Length);
                    }
                    comressBuffer = memoryStream.GetBufferWithoutZeroTail();
                }

                _bufferQueue.Enqueue(blockIndex, comressBuffer);

                // Размер буфера превышает ограничение сборщика мусора 85000 байтов, 
                // необходимо вручную очистить данные буфера из Large Object Heap 
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
        /// <summary>
        /// Заголовок из массива байтов, который записывается с помощью GZipStream
        /// в начало каждого сжатого блока данных.
        /// Содержимое заголовка соответсвует RFC для формата GZip (https://www.ietf.org/rfc/rfc1952.txt).
        /// </summary>
        private readonly byte[] gzipHeader = new byte[] {31, 139, 8, 0, 0, 0, 0, 0, 4, 0};

        private const int BlockSizeRead = 1024*1024;

        private Semaphore _readSemaphore = new Semaphore(0, Int32.MaxValue);
        private Queue<long> _queuePositionBlock = new Queue<long>();

        protected override void ReadStream()
        {
            Thread positionThread = new Thread(SearchStartPositionBlock);
            try
            {
                sourceStream = new FileStream(_soutceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                    BlockSizeRead, FileOptions.Asynchronous);

                positionThread.Start();
                int blockIndex = 0;
                while (true)
                {
                    _readSemaphore.WaitOne();
                    lock (_queuePositionBlock)
                    {
                        long tmpPosition = _queuePositionBlock.Dequeue();
                        if (tmpPosition == -1 || isBreak)
                            break;

                        int tmpBlcokIndex = blockIndex;
                        _threadPool.Execute(new Task(() => DecompressBlock(tmpPosition, tmpBlcokIndex)));
                        blockIndex++;
                    }
                }

            }
            catch (Exception ex)
            {

                _exceptions.Add(ex);
            }
            finally
            {
                positionThread.Join();
                // Размер буфера превышает ограничение сборщика мусора 85000 байтов, 
                // необходимо вручную очистить данные буфера из Large Object Heap 
                GC.Collect();
            }

        }

        /// <summary>
        /// Декомпрессия отдельного блока данных
        /// </summary>
        /// <param name="startPosition">Смещение от начала файла</param>
        /// <param name="blockIndex">Порядок блока</param>
        private void DecompressBlock(long startPosition, int blockIndex)
        {
            try
            {
                lock (sourceStream)
                {
                    sourceStream.Seek(startPosition, SeekOrigin.Begin);
                    using (var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress, true))
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

        private void SearchStartPositionBlock()
        {

            try
            {
                using (Stream localSourceStream = new FileStream(_soutceFilePath, FileMode.Open, FileAccess.Read,
                        FileShare.Read,
                        BlockSizeRead, FileOptions.Asynchronous))
                {
                    if (localSourceStream.StartsWith(gzipHeader))
                    {
                        while (localSourceStream.Position < localSourceStream.Length)
                        {
                            if (isBreak)
                                break;

                            var nextBlockIndex = localSourceStream.GetFirstBufferIndex(gzipHeader, BlockSizeRead);
                            if (nextBlockIndex == -1)
                                break;
                            Enqueue(nextBlockIndex);
                        }
                    }
                    else
                    {
                        Enqueue(0);
                    }
                }
                Enqueue(-1);
            }
            catch (Exception ex)
            {
                _exceptions.Add(ex);
            }
            finally
            {
                // Размер буфера превышает ограничение сборщика мусора 85000 байтов, 
                // необходимо вручную очистить данные буфера из Large Object Heap 
                GC.Collect();
            }
        }

        private void Enqueue(long position)
        {
            lock (_queuePositionBlock)
            {
                _queuePositionBlock.Enqueue(position);
                _readSemaphore.Release();
            }
        }

    }
}
