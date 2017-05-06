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
        static Semaphore _semaphoreWrite2 = new Semaphore(0, 1);
        static Semaphore _semaphoreRead = new Semaphore(10, 3000);

        static object _lockerWrite = new object();
        static object _lockerRead = new object();

        static AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        private const int _bufferSize = 1024*1024*1024;
        private static Exception exception;

        private static Stream sourceStrem;
        private static Stream outputStream;

        private static string sourceFile;
        private static string outputFile;

        private static long sizeOutStream;

        public static void ModificationOfData(string pathSourceFile, string pathOutputFile)
        {
            sourceFile = pathSourceFile;
            outputFile = pathOutputFile;

             _processCount = Environment.ProcessorCount > 2 ? Environment.ProcessorCount : 2;

            Thread [] _threads = new Thread[_processCount];

            try
            {
                sourceStrem = new FileStream(pathSourceFile,FileMode.Open,FileAccess.Read,FileShare.Read);
                outputStream = new GZipStream(new FileStream(pathOutputFile, FileMode.Create, FileAccess.Write,FileShare.Read),CompressionMode.Compress);

               // tmp(outputStream);

                for (int i = 0; i < _processCount; i++)
                {
                    if (i%2 == 0)
                    {
                        _threads[i] = new Thread(Write);
                        _threads[i].Start();
                    }
                    else
                    {
                        _threads[i] = new Thread(Read);
                        _threads[i].Start();
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

                sourceStrem.Close();
                outputStream.Close();
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

        private static void Read()
        {
            try
            {
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
        private static void Write()
        {
            try
            {
                while (true)
                {
                    _semaphoreWrite.WaitOne();
                    chechkOutstream();
                    _semaphoreWrite2.WaitOne();
                    lock (_lockerWrite)
                    {
                        byte[] buffer = _queue.Dequeue();

                        if (buffer == null)
                        {
                            autoResetEvent.Set();
                            break;
                        }

                        outputStream.Write(buffer, 0, buffer.Length);
                        _semaphoreRead.Release();
                    }
                }
            }
            catch (Exception e)
            {

                exception = e;
            }
            

        }

        private static void chechkOutstream()
        {
            lock (_lockerWrite)
            {
                long size = _queue.Peek().Length;
                if (sizeOutStream + size >= 1294967296)
                {
                    outputStream.Close();
                    outputStream = new GZipStream(new FileStream(outputFile, FileMode.Append, FileAccess.Write, FileShare.Read), CompressionMode.Compress);
                    sizeOutStream = 0;
                }
                sizeOutStream += size;
                _semaphoreWrite2.Release();
            }
            
        }

        private static void tmp(Stream str)
        {
            for (int i = 0; i < 2; i++)
            {
                using (var testStream = new FileStream(@"E:\education\programs\Bolshoy_kush_HDRip_[scarabey.org]_by_Scarabey.avi", FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[testStream.Length];
                    testStream.Read(buffer, 0, buffer.Length);
                    str.Write(buffer, 0, buffer.Length);
                }
            }
           
        } 
    }
}
