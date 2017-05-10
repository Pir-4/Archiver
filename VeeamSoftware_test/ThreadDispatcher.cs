using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VeeamSoftware_test
{
   public class ThreadDispatcher
   {
       private readonly Semaphore semaphore;
        private int currentThreads = 0;
        private object _lock = new object();

        public ThreadDispatcher(int countThreads)
       {
           semaphore = new Semaphore(countThreads,countThreads);
       }

       public void Start(Action threadAction)
       {
           semaphore.WaitOne();
           Thread thread;
           lock (_lock)
           {
                thread = new Thread(ExceuteThread);
                Interlocked.Increment(ref currentThreads);
            }
           
            thread.Start(threadAction);
       }

       private void ExceuteThread(object act)
       {
           
           if (act is Action)
               (act as Action)();

           lock (_lock)
           {
                semaphore.Release();
                Interlocked.Decrement(ref currentThreads);
            }
           
        }

       public bool isEmpty
       {
           get { return currentThreads == 0; }
       }
   }
}
