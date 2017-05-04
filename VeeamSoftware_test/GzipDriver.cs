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
        delegate void ThreadsActions(Thread thr, object stream);

        static Queue<byte[]> _queue = new Queue<byte[]>();

        private static Thread[] _threads;
        private static int _processCount;
        static Semaphore _semaphoreWrite = new Semaphore(0, Int32.MaxValue);
        static Semaphore _semaphoreRead = new Semaphore(10, 3000);

        static object _lockerWrite = new object();
        static object _lockerRead = new object();

        static AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        private const int _bufferSize = 1024*1024*1024;
        private static Exception exception;

        public static void ModificationOfData(Stream sourceStrem, Stream outputStream)
        {
             _processCount = Environment.ProcessorCount > 2 ? Environment.ProcessorCount : 2;
            Thread [] _threads = new Thread[_processCount];

            try
            {
                for (int i = 0; i < _processCount; i++)
                {
                    if (i%2 == 0)
                    {
                        _threads[i] = new Thread(Write);
                        _threads[i].Start(outputStream);
                    }
                    else
                    {
                        _threads[i] = new Thread(Read);
                        _threads[i].Start(sourceStrem);
                    }

                }
                autoResetEvent.WaitOne();
            }
            catch (Exception e)
            {

                throw e;
            }
            finally
            {
                for (int i = 0; i < _processCount; i++)
                    _threads[i].Join();
            }

            if (exception != null)
                throw exception;


        }

        private static void PushBlock(byte[] buffer)
        {
            lock (_lockerWrite)
            {
                _queue.Enqueue(buffer);
                _semaphoreWrite.Release();
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
                        {
                            PushBlock(null);
                            break;
                        }

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
                    _semaphoreWrite.WaitOne();
                    byte[] buffer;
                    lock (_lockerWrite)
                    {
                        buffer = _queue.Dequeue();

                        if (buffer == null)
                        {
                            autoResetEvent.Set();
                            break;
                        }

                        outStream.Write(buffer, 0, buffer.Length);
                        _semaphoreRead.Release();
                    }
                }
            }
            catch (Exception e)
            {

                exception = e;
            }
            

        }
    }
}
