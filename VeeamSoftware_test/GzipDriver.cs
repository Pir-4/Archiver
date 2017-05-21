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
                        FileShare.Read,BlockSize,FileOptions.Asynchronous))
                {
                    while (_sourceThread.IsAlive || _bufferQueue.Size > 0 || !_threadPool.isEmpty /*!_threadDispatcher.isEmpty*/)
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
        private readonly byte[] gzipHeader = new byte[] { 31, 139, 8, 0, 0, 0, 0, 0, 4, 0 };
        private const int BlockSizeRead = 1024 * 1024;

        protected override void ReadStream()
        {
            try
            {
                sourceStream = new FileStream(_soutceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,BlockSizeRead,FileOptions.Asynchronous);
                int blockIndex = 0;
                foreach (var position in getListPositionStartBlock(sourceStream))
                {
                    int tmpBlcokIndex = blockIndex;
                    long tmpPosition = position;
                    _threadPool.Execute(new Task(() => DecompressBlock(tmpPosition, tmpBlcokIndex)));
                    blockIndex++;
                }
                // Если файл не начинается со стандартного заголовка, значит архив был создан с помощью сторонней программы.
                // В этом случае разбить файл на отдельные части не удастся, выполняем распаковку архива в одном потоке.
               
            }
            catch (Exception ex)
            {

                _exceptions.Add(ex);
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

        private List<long> getListPositionStartBlock(Stream stream)
        {
            /*lock (stream)
            {*/
                long startPosition = stream.Position;
                List<long> result = new List<long>();
                if (!stream.StartsWith(gzipHeader))
                {
                    result.Add(0);
                    return result;
                }

                while (stream.Position < stream.Length)
                {
                    if (isBreak)
                        break;

                    var nextBlockIndex = stream.GetFirstBufferIndex(gzipHeader, BlockSizeRead);
                    if (nextBlockIndex == -1)
                        break;

                    result.Add(nextBlockIndex);
                }

                stream.Position = startPosition;
                // Размер буфера превышает ограничение сборщика мусора 85000 байтов, 
                // необходимо вручную очистить данные буфера из Large Object Heap 
                GC.Collect();
                return result;
           // }
        }

        /*private int getReadBuffer(long startPosition, ref long bais, ref byte[] buffer)
        {

                long currentPosition = sourceStream.Position;
                long seek = startPosition + bais;
                sourceStream.Seek(seek, SeekOrigin.Begin);
                int bytesread = 0;
                using (var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress, true))
                {
                    bytesread = gzipStream.Read(buffer, 0, buffer.Length);
                    if (bytesread < BlockSize)
                        Array.Resize(ref buffer, bytesread);
                }
                bais += bytesread;
                sourceStream.Position = currentPosition;
                return bytesread;
        }*/
    }
}
