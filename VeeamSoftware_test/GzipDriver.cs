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
        static Semaphore _semaphoreRead ;
        private const int _bufferSize = 2*1024;
        static object _lockerWrite = new object();
        static object _lockerRead = new object();
        private static Exception exception;
        static List<Thread> threadRead = new List<Thread>();
        static List<Thread> threadWrite = new List<Thread>();
        private static int count = 5;

        public static void ModificationOfData(Stream sourceStrem, Stream outputStream)
        {
            /*Thread threadWrite = new Thread(Write);
            Thread threadRead = new Thread(Read);*/
            try
            {
                 _semaphoreRead = new Semaphore(count, _bufferSize * 10);
                for (int i = 0; i < count; i++)
                {
                    threadRead.Add(new Thread(Read));
                    threadWrite.Add(new Thread(Write));
                    threadRead[i].Start(sourceStrem);
                    threadWrite[i].Start(outputStream);
                }
               /* threadWrite.Start(outputStream);
                threadRead.Start(sourceStrem);*/
                Read(sourceStrem);
            }
            catch (Exception e)
            {

                throw e;
            }
            finally
            {
                PushBlock(null);
                //threadWrite.Join();
                for (int i = 0; i < count; i++)
                {
                    threadRead[i].Join();
                    threadWrite[i].Join();
                }
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
                    _semaphoreRead.WaitOne();
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
            /*try
            {*/
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
                    _semaphoreRead.Release();
                }
            //}
            /*catch (Exception e)
            {

                exception = e;
            }*/
            

        }
    }
}
