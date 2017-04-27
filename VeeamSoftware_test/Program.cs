﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using VeeamSoftware_test.Gzip;

namespace VeeamSoftware_test
{
    class Program
    {
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        private static readonly Mutex mutex = new Mutex(true, Assembly.GetExecutingAssembly().GetName().CodeBase);
        private static bool _userRequestExit = false;
        private static bool _doIStop = false;
        static HandlerRoutine consoleHandler;

        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            if (ctrlType.Equals(CtrlTypes.CTRL_C_EVENT))
            {
                _userRequestExit = true;
                throw new ApplicationException("Stop programm");
            }
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

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}ion started", manager.Act));
            manager.Execute();
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}ion completed", manager.Act));
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

            result.SourceFile = argv[1];
            result.ResultFile = argv[2];

            return result;

        }
    }
}
