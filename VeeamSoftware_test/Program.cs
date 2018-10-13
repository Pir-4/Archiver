using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using VeeamSoftware_test.Gzip;

namespace VeeamSoftware_test
{
    public class Program
    {
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        private static readonly Mutex mutex = new Mutex(true, Assembly.GetExecutingAssembly().GetName().CodeBase);
        private static HandlerRoutine consoleHandler;

        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0
        }
        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            if (ctrlType.Equals(CtrlTypes.CTRL_C_EVENT))
                Process.GetCurrentProcess().Kill();
            
            return true;
        }



        public static int Main(string[] argv)
        {
            try
            {
                if (!mutex.WaitOne(TimeSpan.Zero, true))
                    throw new ApplicationException("Another instance already running");

                consoleHandler = new HandlerRoutine(ConsoleCtrlCheck);
                SetConsoleCtrlHandler(consoleHandler, true);

                IGZipManager manager = ValidateArguments(argv);

                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}ion started. Input file: {1}", manager.Act, manager.SourceFile));
                manager.Execute();
                if (manager.Exceptions().Count != 0)
                {
                    foreach (var ex in manager.Exceptions())
                        Console.WriteLine(String.Format(CultureInfo.InvariantCulture,
                            "Error:\n Message: {0} \n StackTrace:\n {1}\n", ex.Message, ex.StackTrace));
                    return 1;
                }
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}ion completed. Output file: {1}", manager.Act, manager.ResultFile));
            }
            catch (Exception e)
            {
                
                Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Error:\n Message: {0} \n StackTrace:\n {1}", e.Message,e.StackTrace));
                return 1;
               }
            return 0;
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
            
            if(!File.Exists(argv[1]))
                throw new ArgumentException("Please enter correct source file name.");

            if (!Directory.Exists(Path.GetDirectoryName(argv[2])))
                throw new ArgumentException("Please enter correct directory output file.");

            IGZipManager result = GZipManager.Сreate(argv[0], argv[1], argv[2]);

            if (result == null)
                throw new ArgumentException(String.Format("Please use \"{0}\" and \"{1}\" commands only as the first parameter.", GZipManager.Compress, GZipManager.Decompress));


            return result;

        }
    }
}
