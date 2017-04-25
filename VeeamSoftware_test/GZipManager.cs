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
        private IGzipDriver driver;

        public int Compress(string input, string output)
        {
            Driver(input).Compress(input,  output);
            return 0;
        }

        public int Decompress(string input, string output)
        {
            Driver(input).Decompress(input, output);
            return 0;
        }

        private IGzipDriver Driver (string path)
        {
            driver = driver == null? driver = GzipDriver.create(path) : driver;
            return driver;
        }
    }
}
