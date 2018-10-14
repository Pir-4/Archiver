using System;

namespace GZipTest.ThreadPool
{
    public interface IMyThreadPool
     {
         void Add(Action action);

         int Count { get; }

         void Run();

         void Wait();

         void Cancel();

         void Clear();
     }

    public static class ThreadPoolExtension
    {
        public static void Execute(this IMyThreadPool pool)
        {
            pool.Run();
            pool.Wait();
        }
    }
}