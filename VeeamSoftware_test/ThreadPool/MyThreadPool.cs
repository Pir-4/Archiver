using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VeeamSoftware_test.ThreadPool
{
    public class MyThreadPool : IMyThreadPool
    {
        private readonly List<ThreadTask> _tasks = new List<ThreadTask>();

        public int Count
        {
            get
            {
                return _tasks.Count;
            }
        }

        public void Add(Action action) => _tasks.Add(new ThreadTask(action));

        public void Run() => Foreach(task => task.Run());       

        public void Wait() => Foreach(task => task.Wait());

        public void Cancel() => Foreach(task => task.Cancel());

        public void Clear() => _tasks.Clear();

        private void Foreach(Action<ThreadTask> action)
        {
            foreach (var task in _tasks)
            {
                action(task);
            }
        }
    }
}
