using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VeeamSoftware_test
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Метод возвращает первое вхождение указанного массива байтов в потоке
        /// </summary>
        /// <param name="inputStream">Поток байтов</param>
        /// <param name="block">Массив байтов</param>
        /// <param name="readBlockSize">Размер блока для чтения потока байтов</param>
        /// <returns>Первое вхождение указанного массива байтов</returns>
        public static long GetFirstBufferIndex(this Stream inputStream, byte[] block, int readBlockSize = 1024)
        {
            while (inputStream.Position < inputStream.Length)
            {
                long statrPosition = inputStream.Position;

                byte[] buffer = new byte[readBlockSize];
                if (inputStream.Read(buffer, 0, buffer.Length) == 0)
                    break;
                var arrayIndex = GetSubArrayIndexes(buffer, block);
                if (arrayIndex.Length > 0)
                {
                    inputStream.Position = arrayIndex.Length == 1
                        ? statrPosition + readBlockSize
                        : statrPosition + arrayIndex[1];
                    return statrPosition + arrayIndex[0];
                }

                if (inputStream.Position < inputStream.Length)
                    inputStream.Position -= block.Length;
            }
            return -1;
        }
        private static long[] GetSubArrayIndexes(byte[] array, byte[] subArray)
        {
            var indexes = new List<long>();

            for (int i = 0; i < array.Length; i++)
            {
                if (CompareArrays(array, i, subArray))
                {
                    indexes.Add(i);
                }
            }

            return indexes.ToArray();
        }
        private static bool CompareArrays(byte[] array, int startIndex, byte[] arrayToCompare)
        {
            if (startIndex < 0 || startIndex > array.Length - arrayToCompare.Length)
                return false;


            for (int i = 0; i < arrayToCompare.Length; i++)
                if (array[startIndex + i] != arrayToCompare[i])
                    return false;

            return true;
        }
        /// <summary>
        /// Метод возвращает массив байтов из потока в памяти.
        /// В отличие от метода MemoryStream::GetBuffer метод при больших объемах данных 
        /// возвращает массив байтов без нулевых байтов в конце.
        /// </summary>
        /// <param name="memoryStream">Поток в памяти</param>
        /// <returns>Массив байтов</returns>
        public static byte[] GetBufferWithoutZeroTail(this MemoryStream memoryStream)
        {
            memoryStream.Position = 0;

            var buffer = new byte[memoryStream.Length];
            memoryStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        /// <summary>
        /// Метод определяет начинается ли поток с указанного массива байтов.
        /// </summary>
        /// <param name="inputStream">Поток байтов</param>
        /// <param name="buffer">Массив байтов</param>
        /// <returns>Поток байтов начинается указанного массива байтов</returns>
        public static bool StartsWith(this Stream inputStream, byte[] buffer)
        {
            byte[] streamBuffer = new byte[buffer.Length];
            if (inputStream.Position > 0)
                inputStream.Seek(0, SeekOrigin.Begin);

            if (inputStream.Read(streamBuffer, 0, streamBuffer.Length) > 0)
            {
                inputStream.Seek(0, SeekOrigin.Begin);
                return CompareArrays(streamBuffer, 0, buffer);
            }

            return false;
        }
    }
}
