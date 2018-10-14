using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GZipTest
{
    public interface IGZipManager
    {
        void Execute();
        string Act { get; }
        string SourceFile { get; }
        string ResultFile { get; }
        List<Exception> Exceptions();
    }
}
