using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GZipTest.ThreadPool
{
    internal class ThreadTask
    {
        private Thread _thread;
        private readonly ThreadStart _threadStart;

        public ThreadTask(Action action)
        {
            _threadStart = new ThreadStart(action);
        }

        public bool IsBackground { get; set; } = true;

        public void Run()
        {
            if (_thread != null)
            {
                throw new Exception("Task was started.");
            }

            _thread = new Thread(_threadStart)
                {
                    IsBackground = IsBackground
                };
            _thread.Start();
        }

        public void Wait()
        {
            if (_thread?.IsAlive ?? false)
            {
                _thread.Join();
            }
        }

        public void Cancel()
        {
            if (_thread?.IsAlive ?? false)
            {
                _thread.Abort();
            }
        }
    }
}