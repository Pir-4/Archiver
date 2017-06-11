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
    public class FixedThreadPool : IDisposable
    {

        private int _maxCountThreads;

        private bool _isStoping = false;
        private bool _isDispose = false;

        private Dictionary<int, ManualResetEvent> threadsEvent;
        private Thread[] threads;
        private Queue<Task> tasks;
        private int currentCountTreads;

        /// <summary>
        /// Создает пул потоков с количеством потоков равным количеству ядер процессора.
        /// </summary>
        public FixedThreadPool() : this(Environment.ProcessorCount) { }

        /// <summary>
        /// Создает пул потоков с указанным количеством потоков.
        /// </summary>
        /// <param name="maxCountThreads">Количество поток.</param>
        public FixedThreadPool(int maxCountThreads)
        {
            if (maxCountThreads < 1)
                maxCountThreads = 1;

            this._maxCountThreads = maxCountThreads;
            this.threads = new Thread[maxCountThreads];
            this.threadsEvent = new Dictionary<int, ManualResetEvent>(maxCountThreads);
            this.tasks = new Queue<Task>();
        }

        /// <summary>
        /// Прерывает выполнение всех потоков, не дожидаясь их завершения и уничтожает за собой все ресурсы.
        /// </summary>
        ~FixedThreadPool()
        {
            Dispose(true);
        }

        /// <summary>
        /// Высвобождает ресурсы, которые используются пулом потоков.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
                    return threads.Where(th => th.IsAlive).Count() != 0;
                }
            }
        }

        public bool isTasksEmpty
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
        public void UpCountTreaads()
        {
            
            lock (tasks)
            {
                Interlocked.Increment(ref _maxCountThreads);
                if ( tasks.Where(t => !t.IsRunned).Count() > 0)
                    ManagerThread();
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
                        break;

                    Task task = SelectTask();

                    if (task == null)
                    {
                        threadsEvent[Thread.CurrentThread.ManagedThreadId].Reset();
                        continue;
                    }

                    task.Execute();
                    threadsEvent[threads[currentCountTreads].ManagedThreadId].Set();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                threadsEvent[Thread.CurrentThread.ManagedThreadId].Reset();
            }

        }

        #region Управление задачами
        private Task SelectTask()
        {
            lock (tasks)
            {
                if (tasks.Count == 0)
                    return null;

                return tasks.Dequeue();
            }
        }
        private void AddTask(Task task)
        {
            lock (tasks)
            {
                tasks.Enqueue(task);
            }
            ManagerThread();
        }
        /// <summary>
        /// Ставит задачу в очередь.
        /// </summary>
        /// <param name="task">Задача.</param>
        /// <returns>Возвращает значание удалось ли поставить задачу в очередь.</returns>
        public bool Execute(Task task)
        {
            if (task == null)
                throw new ArgumentNullException("task", "The Task can't be null.");

            if (_isStoping)
                return false;

            AddTask(task);
            return true;

        }

        /// <summary>
        /// Ставить несколько задачь в очередь.
        /// </summary>
        /// <param name="tasks">Массив задач.</param>
        /// <returns>Возвращает False, если хотя бы одну задачу не удалось установить.</returns>
        public bool ExecuteRange(IEnumerable<Task> tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("task", "The Task can't be null.");

            bool result = true;
            foreach (var task in tasks)
            {
                if (!Execute(task))
                    result = false;
            }

            return result;
        }


        #endregion
        
        #region Управление зпуском и созданием потоков
        /// <summary>
        /// Управляет запуском потоков
        /// </summary>
        /// <returns></returns>
        private bool ManagerThread()
        {
            lock (threads)
            {
                return StartFreeThreads() || StartIsNotAliveThreads() || CreateThread();
            }

        }
        /// <summary>
        /// Создает новый поток и запускает его, если еще не достигнут предел
        /// </summary>
        /// <returns> успешность запуска</returns>
        private bool CreateThread()
        {
            if (currentCountTreads < _maxCountThreads)
            {
                if (threads.Length < _maxCountThreads)
                    Array.Resize(ref threads, _maxCountThreads);

                threads[currentCountTreads] =
                    new Thread(ThreadWork) { Name = "Pool Thread " + currentCountTreads.ToString(), IsBackground = true };
                threadsEvent.Add(threads[currentCountTreads].ManagedThreadId, new ManualResetEvent(false));

                threadsEvent[threads[currentCountTreads].ManagedThreadId].Set();
                threads[currentCountTreads].Start();
                Interlocked.Increment(ref currentCountTreads);
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



        /// <summary>
        /// Запускает первый попавшийся поток, который уже завершил свое выполнение
        /// </summary>
        /// <returns> успешность запуска</returns>
        private bool StartIsNotAliveThreads()
        {
            foreach (var thread in threads)
            {
                if (!thread.IsAlive)
                {
                    threadsEvent[thread.ManagedThreadId].Set();
                    thread.Start();
                    return true;
                }
            }
            return false;
        }


        #endregion

        #region Остановка и унитожение Pool
        /// <summary>
        /// Останавливает работу пула потоков. Ожидает завершения всех задач (запущенных и стоящих в очереди) и уничтожает все ресурсы.
        /// </summary>
        public void Stop()
        {
            lock (threads)
            {
                if (_isStoping)
                    return;

                _isStoping = true;

                foreach (var thread in threads)
                    if (thread.IsAlive && threadsEvent[thread.ManagedThreadId].WaitOne(0) == false)
                        threadsEvent[thread.ManagedThreadId].Set();
            }
        }
        /// <summary>
        /// Высвобождает ресурсы, которые используются пулом потоков.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool dis)
        {
            Stop();
            lock (threads)
            {
                if (_isDispose)
                    return;

                foreach (Thread thread in threads)
                {
                    threadsEvent[thread.ManagedThreadId].Close();
                    thread.Join();
                }
                _isDispose = true;
            }
        }


        #endregion


    }
}
