using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using VeeamSoftware_test.Gzip;

namespace VeeamSoftware_test
{
    class Program
    {
        delegate void ProgressDelegate(string sMessage);



        public static int Main(string[] argv)
        {
            try
            {
                Work(argv);
            }
            catch (Exception e)
            {
                
                Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Error:\n Message: {0} \n StackTrace:\n {1}", e.Message,e.StackTrace));
                return 1;
            }
            return 0;
        }

        private static void Work(string[] argv)
        {
            IGZipManager manager = ValidateArguments(argv);
        }

        private static IGZipManager ValidateArguments(string[] argv)
        {
            if (argv == null || argv.Length != 3)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Please enter 3 parameters:").
                    AppendLine("- for compression: GZipTest.exe compress [source file name] [archive file name]").
                    AppendLine(
                        "- for decompression: GZipTest.exe decompress [archive file name] [decompressed file name]");
                throw new ArgumentException(sb.ToString());
            }

            IGZipManager result = GZipManager.create(argv[0]);
            
            if(result == null)
                throw new ArgumentException(String.Format("Please use \"{0}\" and \"{1}\" commands only as the first parameter.", GZipManager.Compress,GZipManager.Decompress));

            if(!File.Exists(argv[1]))
                throw new ArgumentException("Please enter correct source file name.");

            return result;

        }
    }
}
