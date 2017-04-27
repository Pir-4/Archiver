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

    public  class GzipDriver
    {
        static  Queue<byte[]> _queue = new Queue<byte[]>();
        static Semaphore _semaphore = new Semaphore(0, Int32.MaxValue);
        private const int _bufferSize = 2*1024;
        static object _locker = new object();

        public static void ModificationOfData(Stream sourceStrem, Stream outputStream)
        {
            Thread threadWrite = new Thread(Write);
            threadWrite.Start(outputStream);
            int bytesCount;
            var buffer = new byte[_bufferSize];
            while ((bytesCount = sourceStrem.Read(buffer,0, _bufferSize)) != 0)
            {
                if (bytesCount < _bufferSize)
                    buffer.Take(bytesCount);

                PushBlock(buffer);
                buffer = new byte[_bufferSize];
            }
        }

        private static void PushBlock(byte[] buffer)
        {
            lock (_locker)
            {
                _queue.Enqueue(buffer);
                _semaphore.Release();
            }
        }

        private static void Write(Object obj)
        {
            Stream outStream = obj as Stream;
            while (true)
            {
                _semaphore.WaitOne();
                byte[] buffer;
                lock (_locker)
                {
                    buffer = _queue.Dequeue();
                }
                if(buffer == null)
                    break;
                outStream.Write(buffer,0,buffer.Length);
            }
        }
    }
}
