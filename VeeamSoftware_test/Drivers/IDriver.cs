using System;
using System.Collections.Generic;

namespace GZipTest.Drivers
{
    public interface IDriver
    {
        void Execute();
        List<Exception> Exceptions { get; }
        string SourceFile { get; }
        string ResultFile { get; }
    }
}