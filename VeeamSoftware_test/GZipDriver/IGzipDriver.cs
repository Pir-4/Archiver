using System;
using System.Collections.Generic;

namespace VeeamSoftware_test.Gzip
{
    public interface IGzipDriver
    {
        void Execute(string inputPath, string outputPath);
        List<Exception> Exceptions { get; }
    }
}