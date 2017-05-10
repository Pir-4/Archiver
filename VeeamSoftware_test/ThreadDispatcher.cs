using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VeeamSoftware_test
{
    /// <summary>
    /// Дисечер потоков.
    /// Горантирует, что будет запущенно не более указанного количества потоков
    /// </summary>
   public class ThreadDispatcher
   {
        /// <summary>
        /// Счетчик запущенных потоков
        /// </summary>
        private int currentThreads;
        private readonly Semaphore semaphore;
        
        private object _lock = new object();

        public ThreadDispatcher(int countThreads)
       {
           semaphore = new Semaphore(countThreads,countThreads);
       }
        /// <summary>
        /// Выполнить действие в отдельном потоке.
        /// Если было достигнуто максимальное количество потоков
        /// планировщик блокирует вызывающий поток и ожидает завершения одного из запущенных потоков.
        /// </summary>
        /// <param name="threadAction">Выполняемое действие</param>
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
