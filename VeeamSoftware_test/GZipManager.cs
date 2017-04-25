using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VeeamSoftware_test.Gzip
{
    public interface IGZipManager
    {
        int Compress(string input, string output);
        int Decompress(string input, string output);
    }

    class GZipManager : IGZipManager
    {
        public int Compress(string input, string output)
        {
            return 0;
        }

        public int Decompress(string input, string output)
        {
            return 0;
        }
    }
}
