using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using GZipTest;

namespace GZipTest
{
    public class Program
    {
        private static readonly string Help = "Please enter 3 parameters: \n"+
            $"- for compression: GZipTest.exe {Command.Compress.ToString()} [source file name] [archive file name]"+
            $"- for decompression: GZipTest.exe {Command.Decompress.ToString()} [archive file name] [decompressed file name]"+
            $"- for calcilate hash sha256: GZipTest.exe {Command.Sha256.ToString()} [source file name] [block size]";

        public delegate bool HandlerRoutine(CtrlTypes ctrlType);

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);

        private static readonly Mutex Mutex = new Mutex(true, Assembly.GetExecutingAssembly().GetName().CodeBase);
        private static HandlerRoutine _consoleHandler;

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
                if (!Mutex.WaitOne(TimeSpan.Zero, true))
                    throw new ApplicationException("Another instance already running");

                _consoleHandler = new HandlerRoutine(ConsoleCtrlCheck);
                SetConsoleCtrlHandler(_consoleHandler, true);

                var blocksize = ValidateArguments(argv);
                IManager manager = GetManager(argv, blocksize);

                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}ion started. Input file: {1}", manager.Act, manager.SourceFile));

                manager.Execute();

                if (manager.Exceptions().Count != 0)
                {
                    foreach (var ex in manager.Exceptions())
                        Console.WriteLine(String.Format(CultureInfo.InvariantCulture,
                            "Error:\n Message: {0} \n StackTrace:\n {1}\n", ex.Message, ex.StackTrace));
                    return 1;
                }

                if (manager.Act.Equals(Command.Compress.ToString()) ||
                    manager.Act.Equals(Command.Decompress.ToString()))
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}ion completed. Output file: {1}",
                        manager.Act, manager.ResultFile));
                }
            }
            catch (Exception e)
            {

                Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Error:\n Message: {0} \n StackTrace:\n {1}", e.Message, e.StackTrace));
                return 1;
            }
            return 0;
        }

        private static int ValidateArguments(string[] argv)
        {
            if (argv == null || argv.Length < 3)
            {
                throw new ArgumentException(Help);
            }

            int blockSize = 0;

            if (!File.Exists(argv[1]))
                throw new ArgumentException("Please enter correct source file name.");

            if (!argv[0].Equals(Command.Sha256.ToString(),StringComparison.CurrentCultureIgnoreCase))
            {
                if (!Directory.Exists(Path.GetDirectoryName(argv[2])))
                    throw new ArgumentException("Please enter correct directory output file.");
            }
            else
            {
                if (!int.TryParse(argv[2], out blockSize))
                    throw new ArgumentException("Please enter correct block size");
            }
            return blockSize;
        }

        private static IManager GetManager(string[] argv, int blockSize)
        {
            IManager result = Manager.Factory(act: argv[0], 
                inputFile: argv[1], outputfile: argv[2], 
                blockSize:blockSize);

            if (result == null)
            {
                throw new ArgumentException(
                    $"Please use \"{Command.Compress.ToString()}\" or \"{Command.Decompress.ToString()}\" " +
                    $"or \"{Command.Sha256.ToString()}\" commands only as the first parameter.");
            }

            return result;
        }
    }
}