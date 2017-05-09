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

       public ThreadDispatcher(int countThreads)
       {
           semaphore = new Semaphore(countThreads,countThreads);
       }

       public void Start(Action threadAction)
       {
           semaphore.WaitOne();
           Thread thread = new Thread(ExceuteThread);
            thread.Start(threadAction);
       }

       private void ExceuteThread(object act)
       {
           
           if (act is Action)
               (act as Action)();

            semaphore.Release();
        }
   }
}
