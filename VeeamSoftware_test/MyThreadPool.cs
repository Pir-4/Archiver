using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

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

    }
    /// <summary>
    /// Пул потоков. В нем задачи запускаются по приоритетам. Если количество потоков больше или равно 4, то на каждые 3 задачи с высоким приоритетом - будет запущена задача с нормальным приоритетом. Если количество потоков меньше 4, тогда задачи выполняются прямо следуя приоритетам. Задачи с низким приоритетом выполняются в последнюю очередь.
    /// </summary>
    public class MyThreadPool /*: IDisposable*/
    {

        private int _maxCountThreads;

        private bool _isStoping = true;
        private bool _isBreak;
        private object _isBreakLock;

        private Dictionary<int, AutoResetEvent> threadsEvent;
        private List<Thread> threads;
        private Queue<Task> tasks;

        private Semaphore _semaphoreQueue;


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

            _semaphoreQueue = new Semaphore(this._maxCountThreads * 2, this._maxCountThreads * 2);

            this.threads = new List<Thread>();
            this.threadsEvent = new Dictionary<int, AutoResetEvent>(maxCountThreads);
            this.tasks = new Queue<Task>();
        }
        /// <summary>
        /// Прерывает выполнение всех потоков, не дожидаясь их завершения и уничтожает за собой все ресурсы.
        /// </summary>
        ~MyThreadPool()
        {
            //Dispose(true);
        }

        public void Start()
        {
            lock (threads)
            {
                _isStoping = false;

                foreach (var thread in threads)
                    threadsEvent[thread.ManagedThreadId].Set();
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
                _semaphoreQueue.WaitOne();
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
                lock (threadsEvent)
                {
                    return threadsEvent.Where(kvp => kvp.Value.WaitOne(0)).Count() > 0;
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
                lock (_isBreakLock)
                {
                    _isBreak = true;
                }
                Interlocked.Decrement(ref _maxCountThreads);
                lock (_semaphoreQueue)
                {
                    int currentValue =  (_semaphoreQueue.Release(), _maxCountThreads*2);
                    _semaphoreQueue = new Semaphore(Math.m(currentValue, _maxCountThreads), _maxCountThreads*2);
                }
                return true;
            }
        }

        private void ThreadWork()
        {
            try
            {
                while (true)
                {
                    threadsEvent[Thread.CurrentThread.ManagedThreadId].WaitOne();

                    if (_isStoping)
                        continue;

                    lock (_isBreakLock)
                    {
                        if (_isBreak)
                        {
                            _isBreak = false;
                            break;
                        }
                    }
                    

                    Task task = SelectTask();

                    if (task == null)
                        continue;

                    task.Execute();
                    _semaphoreQueue.Release();
                    threadsEvent[Thread.CurrentThread.ManagedThreadId].Set();

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

        #region Управление задачами
        private Task SelectTask()
        {
            lock (tasks)
            {
                return tasks.Count == 0 ? null : tasks.Dequeue();
            }
        }

        #endregion
        
        #region Управление зпуском и созданием потоков
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

                threadsEvent.Add(threads[currentCountTreads].ManagedThreadId, new AutoResetEvent(false));

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
                if (thread.IsAlive && threadsEvent[thread.ManagedThreadId].WaitOne(0) == false)
                {
                    threadsEvent[thread.ManagedThreadId].Set();
                    return true;
                }
            }
            return false;
        }

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

        #region Остановка и унитожение Pool
        /// <summary>
        /// Высвобождает ресурсы, которые используются пулом потоков.
        /// </summary>
        /// <param name="disposing"></param>
         protected virtual void Dispose(bool dis)
         {
             lock (threads)
             {
                 if (_isDispose)
                     return;

                 foreach (Thread thread in threads)
                 {
                     threadsEvent[thread.ManagedThreadId].Close();
                 }
             }
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
