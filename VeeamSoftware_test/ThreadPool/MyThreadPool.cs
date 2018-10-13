using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VeeamSoftware_test.ThreadPool
{
    public class MyThreadPool : IMyThreadPool
    {
        private readonly List<ThreadStart> _actions;

        public void Add(Action action) => _actions.Add(new ThreadStart(action));

        public void Run()
        {
            foreach (var threadStart in _actions)
            {
                
            }
        }
    }
}
