﻿using System;
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

        private ManualResetEvent stopEvent;
        private bool _isStoping;
        private object stopLock;

        private Dictionary<int, ManualResetEvent> threadsEvent;
        private Thread[] threads;
        private Queue<Task> tasks;
        private int currentCountTreads;

        private ManualResetEvent scheduleEvent;
        private Thread scheduleThread;

        private bool isDisposed;

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
            if (maxCountThreads <= 0)
                maxCountThreads = 1;

            this._maxCountThreads = maxCountThreads;

            this.stopLock = new object();
            this.stopEvent = new ManualResetEvent(false);

            this.scheduleEvent = new ManualResetEvent(false);
            this.scheduleThread = new Thread(SelectAndStartFreeThread) { Name = "Schedule Thread", IsBackground = true };
            scheduleThread.Start();

            this.threads = new Thread[maxCountThreads];
            this.threadsEvent = new Dictionary<int, ManualResetEvent>(maxCountThreads);

            this.tasks = new Queue<Task>();
        }

        /// <summary>
        /// Прерывает выполнение всех потоков, не дожидаясь их завершения и уничтожает за собой все ресурсы.
        /// </summary>
        ~FixedThreadPool()
        {
            Dispose(false);
        }

        /// <summary>
        /// Высвобождает ресурсы, которые используются пулом потоков.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool isStoping
        {
            get { return _isStoping; }
        }

        public bool isEmpty
        {
            get { return (tasks.Count == 0); }
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
                    CreateThread();
            }
            
        }
        /// <summary>
        /// Высвобождает ресурсы, которые используются пулом потоков.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    scheduleThread.Abort();
                    scheduleEvent.Close();
                    lock (threads)
                    {
                        for (int i = 0; i < threads.Length; i++)
                        {
                            if (threads[i] != null)
                            {
                                threads[i].Abort();
                                threadsEvent[threads[i].ManagedThreadId].Close();
                            }
                        }
                    }
                }

                isDisposed = true;
            }
        }

        private Task SelectTask()
        {
            lock (tasks)
            {
                if (tasks.Count == 0)
                    return null;

                return tasks.Dequeue();
            }
        }

        private void ThreadWork()
        {
            while (true)
            {
                threadsEvent[Thread.CurrentThread.ManagedThreadId].WaitOne();

                Task task = SelectTask();
                if (task != null)
                {
                    try
                    {
                        task.Execute();
                    }
                    finally
                    {
                        if (!isEmpty)
                            scheduleEvent.Set();

                        if (isStoping)
                            stopEvent.Set();

                        //Thread.Sleep(500);
                        threadsEvent[Thread.CurrentThread.ManagedThreadId].Reset();
                    }
                }
            }
        }

        private void SelectAndStartFreeThread()
        {
            while (true)
            {
                scheduleEvent.WaitOne();
                lock (threads)
                {
                    foreach (var thread in threads)
                    {
                        if (threadsEvent[thread.ManagedThreadId].WaitOne(0) == false)
                        {
                            threadsEvent[thread.ManagedThreadId].Set();
                            break;
                        }
                    }
                }

                scheduleEvent.Reset();
            }
        }

        private void AddTask(Task task)
        {
            lock (tasks)
            {
                tasks.Enqueue(task);
            }
            CreateThread();
            scheduleEvent.Set();
        }

        private void CreateThread()
        {
            lock (threads)
            {
                if (currentCountTreads < _maxCountThreads)
                {
                    if (threads.Length  < _maxCountThreads)
                        Array.Resize(ref threads, _maxCountThreads);

                    threads[currentCountTreads] = new Thread(ThreadWork) {Name = "Pool Thread " + currentCountTreads.ToString(), IsBackground = true};
                    threadsEvent.Add(threads[currentCountTreads].ManagedThreadId, new ManualResetEvent(false));

                    threads[currentCountTreads].Start();
                    Interlocked.Increment(ref currentCountTreads);
                }
            }
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

            lock (stopLock)
            {
                if (isStoping)
                {
                    return false;
                }

                AddTask(task);
                return true;
            }
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

        /// <summary>
        /// Останавливает работу пула потоков. Ожидает завершения всех задач (запущенных и стоящих в очереди) и уничтожает все ресурсы.
        /// </summary>
        public void Stop()
        {
            lock (stopLock)
            {
                _isStoping = true;
            }

            while (tasks.Count > 0)
            {
                stopEvent.WaitOne();
                stopEvent.Reset();
            }

            Dispose(true);
        }

    }
}
