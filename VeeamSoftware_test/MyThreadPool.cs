using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VeeamSoftware_test
{
   /// <summary>
    /// Пул потоков. В нем задачи запускаются по приоритетам. Если количество потоков больше или равно 4, то на каждые 3 задачи с высоким приоритетом - будет запущена задача с нормальным приоритетом. Если количество потоков меньше 4, тогда задачи выполняются прямо следуя приоритетам. Задачи с низким приоритетом выполняются в последнюю очередь.
    /// </summary>
    public class MyThreadPool : IMyThreadPool, IDisposable
    {

        private int _maxCountThreads;

        private bool _isStoping = true;
        private bool _isBreak;
        private readonly object _isBreakLock = new object();

        private Dictionary<int, ManualResetEvent> threadsEvent;

        private List<Thread> threads;
        private Queue<Task> tasks;

        /// <summary>
        /// Создает пул потоков с количеством потоков равным количеству ядер процессора.
        /// </summary>
        public MyThreadPool() : this(Environment.ProcessorCount) { }

        /// <summary>
        /// Создает пул потоков с указанным количеством потоков.
        /// </summary>
        /// <param name="maxCountThreads">Количество поток.</param>
        public MyThreadPool(int maxCountThreads)
        {
            if (maxCountThreads < 1)
                maxCountThreads = 1;

            this._maxCountThreads = maxCountThreads;

            this.threads = new List<Thread>();
            this.threadsEvent = new Dictionary<int, ManualResetEvent>(maxCountThreads);

            this.tasks = new Queue<Task>();
        }
        /// <summary>
        /// Прерывает выполнение всех потоков, не дожидаясь их завершения и уничтожает за собой все ресурсы.
        /// </summary>
        ~MyThreadPool()
        {
            Dispose(true);
        }

        public void Start()
        {
            lock (threads)
            {
                _isBreak =_isStoping = false;

                foreach (var thread in threads)
                    threadsEvent[thread.ManagedThreadId].Set();
            }

            for (int i = 0; i < tasks.Where(t => !t.IsRunned).Count(); i++)
            {
                lock (threads)
                {
                    if (threads.Count == _maxCountThreads)
                        break;
                }
                CreatedThread();
            }
        }

        public void Stop()
        {
            lock (threads)
            {
                _isStoping = true;
                foreach (var thread in threads)
                    threadsEvent[thread.ManagedThreadId].Set();
            }
        }

        public void Execute(Action action)
        {
            lock (tasks)
            {
                tasks.Enqueue(new Task(action));
            }
            CreatedThread();
        }

        public void Execute(IEnumerable<Action> actions)
        {
            foreach (var action in actions)
                Execute(action);
        }

       

        /// <summary>
        /// Заняты ли потоки работой
        /// </summary>
        public bool isWork
        {
            get
            {
                lock (threads)
                {
                    return threads.Where(th => th.ThreadState.Equals(ThreadState.Running)).Count() > 0;
                }
            }
        }
        /// <summary>
        /// Пуст ли пул задач
        /// </summary>
        public bool isEmpty
        {
            get {
                lock (tasks)
                {
                    return tasks.Count == 0;
                }
            }
        }

        #region Управление максимальным количеством Threds
        /// <summary>
        /// Увеличивает количество максимально доступных потоков
        /// </summary>
        public bool UpCountTreaads()
        {
            
            lock (tasks)
            {
                Interlocked.Increment(ref _maxCountThreads);
               return (tasks.Where(t => !t.IsRunned).Count() > 0) && CreatedThread();
            }
            
        }
        /// <summary>
        /// Уменьшает количество потоков в пуле
        /// </summary>
        public bool DownCountThreads()
        {
            lock (threads)
            {
                if (threads.Count == 0)
                    return false;

                lock (_isBreakLock)
                {
                    _isBreak = true;
                    Interlocked.Decrement(ref _maxCountThreads);
                }
                return true;
            }
        }
        #endregion
        /// <summary>
        /// Очищение Pool-а от всех объектов Thread
        /// </summary>
        public void Free()
        {
            lock (threads)
            {
                _isStoping = true;
                foreach (var thread in threads)
                {
                    _isBreak = true;
                    lock (_isBreakLock)
                    {
                       // _isBreak = true;
                        threadsEvent[thread.ManagedThreadId].Set();
                    }
                }
            }
           
        }

        #region Выполнение задач
        /// <summary>
        /// Функция потока в которой идет выполенение поставленных задач
        /// </summary>
        private void ThreadWork()
        {
            try
            {
                while (true)
                {
                    threadsEvent[Thread.CurrentThread.ManagedThreadId].WaitOne();
                    lock (_isBreakLock)
                    {
                        if (_isBreak)
                        {
                            break;
                        }
                    }

                    if (_isStoping)
                    {
                        threadsEvent[Thread.CurrentThread.ManagedThreadId].Reset();
                        continue;
                    }

                    Task task = SelectTask();

                    if (task == null)
                    {
                        threadsEvent[Thread.CurrentThread.ManagedThreadId].Reset();
                        continue;
                    }

                    task.Execute();
                   // threadsEvent[Thread.CurrentThread.ManagedThreadId].Set();

                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                ClearThread(Thread.CurrentThread);
            }

        }

      
        /// <summary>
        /// Функция поулчения Task
        /// </summary>
        /// <returns>null если очередь пуста</returns>
        private Task SelectTask()
        {
            lock (tasks)
            {
                return tasks.Count == 0 ? null : tasks.Dequeue();
            }
        }

        #endregion
        
        #region Управление запуском и созданием потоков
        /// <summary>
        /// Управляет запуском потоков
        /// </summary>
        /// <returns></returns>
        private bool CreatedThread()
        {
            lock (threads)
            {
                
                return (!_isStoping ) &&   (StartFreeThreads() || CreateNewThread());
            }

        }
        /// <summary>
        /// Создает новый поток и запускает его, если еще не достигнут предел
        /// </summary>
        /// <returns> успешность запуска</returns>
        private bool CreateNewThread()
        {
            if (threads.Count < _maxCountThreads)
            {
                int currentCountTreads = threads.Count;

                threads.Add(new Thread(ThreadWork)
                {
                    Name = "Pool Thread " + currentCountTreads.ToString(),
                    IsBackground = true
                });

                threadsEvent.Add(threads[currentCountTreads].ManagedThreadId, new ManualResetEvent(false));
                threadsEvent[threads[currentCountTreads].ManagedThreadId].Set();
                threads[currentCountTreads].Start();

                return true;
            }
            return false;

        }

        /// <summary>
        /// Запускает первый попавшийся поток, который находиться в ожидании
        /// </summary>
        /// <returns>упешность запуска</returns>
        private bool StartFreeThreads()
        {
            foreach (var thread in threads)
            {
                if (thread.IsAlive && thread.ThreadState.Equals(ThreadState.Suspended))
                {
                    threadsEvent[thread.ManagedThreadId].Set();
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Удаляет и завершает поток и его события из всех списков
        /// </summary>
        /// <param name="thread">Удаляемый поток</param>
        private void ClearThread(Thread thread)
        {
            lock (threads)
            {
                threads.Remove(thread);
                threadsEvent[thread.ManagedThreadId].Close();
                threadsEvent.Remove(thread.ManagedThreadId);
                thread.Join();
            }
        }


        #endregion

        #region Уничтожение Pool
        /// <summary>
        /// Высвобождает ресурсы, которые используются пулом потоков.
        /// </summary>
        /// <param name="disposing"></param>
         protected virtual void Dispose(bool dis)
         {
            Free();
         }

        /// <summary>
        /// Высвобождает ресурсы, которые используются пулом потоков.
        /// </summary>
         public void Dispose()
         {
             Dispose(true);
             GC.SuppressFinalize(this);
         }

        #endregion


    }
}
