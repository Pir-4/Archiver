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

    public class GzipDriver
    {
        static Queue<byte[]> _queue = new Queue<byte[]>();

        static Semaphore _semaphore = new Semaphore(0, Int32.MaxValue);
        static object _lockerWrite = new object();
        static object _lockerRead = new object();

        private const int _bufferSize = 2*1024;
        private static Exception exception;

        public static void ModificationOfData(Stream sourceStrem, Stream outputStream)
        {
            Thread threadWrite = new Thread(Write);
            Thread threadRead = new Thread(Read);
            try
            {
                threadWrite.Start(outputStream);
                threadRead.Start(sourceStrem);
                Read(sourceStrem);
            }
            catch (Exception e)
            {

                throw e;
            }
            finally
            {
                PushBlock(null);
                threadWrite.Join();
                threadRead.Join();
            }

            if (exception != null)
                throw exception;


        }

        private static void PushBlock(byte[] buffer)
        {
            lock (_lockerWrite)
            {
                _queue.Enqueue(buffer);
                _semaphore.Release();
            }
        }

        private static void Read(Object obj)
        {
            try
            {
                Stream sourceStrem = obj as Stream; 

                while (true)
                {
                    var buffer = new byte[_bufferSize];
                    lock (_lockerRead)
                    {
                        int bytesCount = sourceStrem.Read(buffer, 0, _bufferSize);
                        if (bytesCount == 0)
                            break;

                        buffer = bytesCount < _bufferSize ? buffer.Take(bytesCount).ToArray() : buffer;
                        PushBlock(buffer);
                    }
                }
            }
            catch (Exception e)
            {

                exception = e;
            }
        }
        private static void Write(Object obj)
        {
            try
            {
                Stream outStream = obj as Stream;
                while (true)
                {
                    _semaphore.WaitOne();
                    byte[] buffer;
                    lock (_lockerWrite)
                        buffer = _queue.Dequeue();

                    if (buffer == null)
                        break;

                    outStream.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception e)
            {

                exception = e;
            }
            

        }
    }
}
