using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace VeeamSoftware_test
{
    class Program
    {
        delegate void ProgressDelegate(string sMessage);

        

        public static int Main(string[] argv)
        {
            if (argv.Length != 2)
            {
                Console.WriteLine("Usage: CmprDir.exe <in_dir compressed_file> | <compressed_file out_dir>");
                return 1;
            }

            string sDir;
            string sCompressedFile;
            bool bCompress = false;
            try
            {
                if (Directory.Exists(argv[0]))
                {
                    sDir = argv[0];
                    sCompressedFile = argv[1];
                    bCompress = true;
                }
                else
                  if (File.Exists(argv[0]))
                {
                    sCompressedFile = argv[0];
                    sDir = argv[1];
                    bCompress = false;
                }
                else
                {
                    Console.Error.WriteLine("Wrong arguments");
                    return 1;
                }

                if (bCompress)
                    GzipDriver.CompressDirectory(sDir, sCompressedFile);
                else
                    GzipDriver.Decompress(sCompressedFile, sDir);

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }
    }
}
