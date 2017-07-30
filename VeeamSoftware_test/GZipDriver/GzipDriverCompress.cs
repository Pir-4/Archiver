﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using VeeamSoftware_test.Gzip;

namespace VeeamSoftware_test.GZipDriver
{
    public class GzipDriverCompress : GzipDriver
    {
        public GzipDriverCompress() : base()
        {
            countTreadsOfObject++;
        }
        /// <summary>
        /// Чтение данных из файла 
        /// </summary>
        protected override void ReadStream()
        {
            try
            {
                sourceStream = new FileStream(_soutceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                    BlockSize,
                    FileOptions.Asynchronous);
                _threadPool.Start();
                for (int i = 0; i < (int) Math.Ceiling((double) sourceStream.Length / BlockSize); i++)
                {
                    if (isBreak)
                        break;

                    int blockIndex = i;
                    byte[] readBuffer = new byte[BlockSize];
                    BufferMyQueue.WaitOne();
                    int bytesread = sourceStream.Read(readBuffer, 0, readBuffer.Length);
                    if (bytesread < BlockSize)
                        Array.Resize(ref readBuffer, bytesread);

                    _threadPool.Execute(() => CompressBlock(readBuffer, blockIndex));
                }
            }
            catch (Exception ex)
            {

                Exceptions.Add(ex);
            }
            finally
            {
                _threadPool.UpCountTreaads();
                BufferMyQueue.isEnd = true;
                GC.Collect();
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
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress,true))
                    {
                        gzipStream.Write(readBuffer, 0, readBuffer.Length);
                    }
                    comressBuffer = memoryStream.GetBufferWithoutZeroTail();
                }

                BufferMyQueue.Enqueue(blockIndex, comressBuffer);
            }
            catch (Exception ex)
            {
                Exceptions.Add(ex);
            }

        }
    }
}
