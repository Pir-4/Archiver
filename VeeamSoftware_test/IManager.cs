using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GZipTest
{
    public interface IManager
    {
        void Execute();
        string Act { get; }
        string SourceFile { get; }
        string ResultFile { get; }
        List<Exception> Exceptions();
    }
}