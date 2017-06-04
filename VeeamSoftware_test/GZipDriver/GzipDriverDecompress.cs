using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using VeeamSoftware_test.Gzip;

namespace VeeamSoftware_test.GZipDriver
{
    public class GzipDriverDecompress : GzipDriver
    {
        /// <summary>
        /// Заголовок из массива байтов, который записывается с помощью GZipStream
        /// в начало каждого сжатого блока данных.
        /// Содержимое заголовка соответсвует RFC для формата GZip (https://www.ietf.org/rfc/rfc1952.txt).
        /// </summary>
        private readonly byte[] gzipHeader = new byte[] { 31, 139, 8, 0, 0, 0, 0, 0, 4, 0 };

        private const int BlockSizeRead = 1024 * 1024;

        private Semaphore _readSemaphore = new Semaphore(0, Int32.MaxValue);
        private Queue<long> _queuePositionBlock = new Queue<long>();

        public GzipDriverDecompress() : base()
        {
            countTreadsOfObject += 2;
        }

        protected override void ReadStream()
        {
            Thread positionThread = new Thread(SearchStartPositionBlock);
            positionThread.Start();

            try
            {
                sourceStream = new FileStream(_soutceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                    BlockSizeRead, FileOptions.Asynchronous);

                
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

                Exceptions.Add(ex);
            }
            finally
            {
                _threadPool.UpCountTreaads();
               // positionThread.Join();
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
                    using (var gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress,true))
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
                            _writeResetEvent.Set();
                            buffer = nextBuffer;
                            bufferNumber++;
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                Exceptions.Add(ex);
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
                Exceptions.Add(ex);
            }
            finally
            {
                Thread.CurrentThread.Join();
                _threadPool.UpCountTreaads();
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
