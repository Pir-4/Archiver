using System;
using System.Collections.Generic;

namespace VeeamSoftware_test
{
    public interface IMyThreadPool
    {
        void Start();
        void Stop();

        void Execute(Action action);
        void Execute(IEnumerable<Action> actions);

        bool isWork { get; }
        bool isEmpty { get; }

        bool UpCountTreaads();
        bool DownCountThreads();

        void Free();

    }
}