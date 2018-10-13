using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using VeeamSoftware_test.Gzip;

namespace VeeamSoftware_test.GZipDriver
{
    public abstract class GzipDriver : IGzipDriver
    {
        private bool _isComplited;
        private int _maxCountReadedBlocks;

        protected string SourceFilePath;
        protected string OutputFilePath;


        protected GzipDriver()
        {

        }

        public void Execute(string inputPath, string outputPath)
        {
            SourceFilePath = inputPath;
            OutputFilePath = outputPath;
        }

        public List<Exception> Exceptions { get; set; } = new List<Exception>();

        protected abstract int GetBlockLength(Stream stream);

        protected abstract byte[] ProcessBlcok(byte[] input);

        private void Read()
        {
            try
            {
                var id = 0;
                using (var inputStream = File.OpenRead(SourceFilePath))
                {
                    while (!_isComplited && inputStream.Position < inputStream.Length)
                    {
                        var blockSize = GetBlockLength(inputStream);
                        var data = new byte[blockSize];
                        inputStream.Read(data, 0, data.Length);
                        //TODO создать очередь для чтения
                    }
                }
                _maxCountReadedBlocks = id;
            }
            catch (Exception e)
            {
                _isComplited = true;
                Exceptions.Add(e);
            }
        }

        private void Process()
        {
            try
            {
                while (!_isComplited)
                {
                    if (true) //TODO вставить попытку считывания с очереди чтения
                    {
                        //gлучили блок
                        var block = new byte[6];
                        var data = ProcessBlcok(block);
                        //TODО добавление в очерезь записи
                    }
                }
            }
            catch (Exception e)
            {
                _isComplited = true;
                Exceptions.Add(e);
            }
        }

        private void Write()
        {
            try
            {
                var expectedId = 0;
                using (var outputStrem = new FileStream(OutputFilePath, FileMode.Append))
                {
                    while (!_isComplited && expectedId < _maxCountReadedBlocks)
                    {
                        if (true)//TODO вставить попытка взять объект из очереди записи
                        {
                            var blcok = new byte[4];
                            expectedId++;
                            outputStrem.Write(blcok, 0, 5);
                            outputStrem.Flush(true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Exceptions.Add(e);
            }
            finally
            {
                _isComplited = true;
            }
        }
    }
}