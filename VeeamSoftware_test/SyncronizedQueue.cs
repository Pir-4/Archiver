using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VeeamSoftware_test
{
    /// <summary>
    /// Упорядоченная очередь.
    /// Для каждого эллемента указывается порядковый номер, сотоящий из основного порядка и подпорядка (если блок разбит на части)
    /// </summary>
    /// <typeparam name="T">Тип элементов в очереди</typeparam>
    public class SyncronizedQueue<T>
    {
        private Dictionary<QueuOrder, T> _queueDictionary = new Dictionary<QueuOrder, T>();
        private Dictionary<int, int> _subOrderLimits = new Dictionary<int, int>();
        private int _currentOrder;
        private int _currentSubOrder;

        private Semaphore _semaphoreEnqueue;

        AutoResetEvent _addEvent = new AutoResetEvent(false);

        public SyncronizedQueue(int maxSize = 15)
        {
            _semaphoreEnqueue = new Semaphore(maxSize, maxSize);
        }
        /// <summary>
        /// Добавление эелмента в очередь
        /// </summary>
        /// <param name="order">Основной порядковый номер элемента</param>
        /// <param name="subOrder">Второстепенный порядковый номер</param>
        /// <param name="item">Добавляемый элемент</param>
        /// <param name="lastSubOrder">Флаг, указывающий, что  второстепенный порядковый номер является последним
        /// для основного порядкового номера</param>
        public void Enqueue(int order, int subOrder, T item, bool lastSubOrder = false)
        {
            lock (_queueDictionary)
            {
                QueuOrder queuOrder = new QueuOrder(order, subOrder);
                if (lastSubOrder)
                    _subOrderLimits[order] = subOrder;

                _queueDictionary.Add(queuOrder, item);
                _addEvent.Set();
            }
        }
        /// <summary>
        /// Добавление элемента в очередь
        /// </summary>
        /// <param name="order">Порядковый номер жлемента</param>
        /// <param name="item">Элемент</param>
        public void Enqueue(int order, T item)
        {
            Enqueue(order, 0, item, true);
        }
        /// <summary>
        /// получение элемента из очереди
        /// </summary>
        /// <param name="item">Полученный элемент</param>
        /// <returns>true - успешное извлечение из очереди. false -элемент не найден</returns>
        public bool TryGetValue(out T item)
        {
            item = default(T);
            while (true)
            {
                if (!isEnd)
                    _addEvent.WaitOne();

                lock (_queueDictionary)
                {
                    QueuOrder queuOrder = new QueuOrder(_currentOrder, _currentSubOrder);
                    if (_queueDictionary.TryGetValue(queuOrder, out item))
                    {
                        _queueDictionary.Remove(queuOrder);
                        if (_subOrderLimits.ContainsKey(_currentOrder) &&
                            _currentSubOrder == _subOrderLimits[_currentOrder])
                        {
                            Interlocked.Increment(ref _currentOrder);
                            _currentSubOrder = 0;
                        }
                        else
                        {
                            Interlocked.Increment(ref _currentSubOrder);
                        }
                        _addEvent.Set();
                        return true;
                    }
                    else if (isEnd && Size == 0)
                        break;
                }
            }
            return false;
        }

        public void WaitOne()
        {
            _semaphoreEnqueue.WaitOne();
        }

        public void Release()
        {
            _semaphoreEnqueue.Release();
        }
        public int Size
        {
            get
            {
                lock (_queueDictionary)
                {
                    return _queueDictionary.Count();
                }
            }
        }
        public bool isEnd { get; set; }

        /// <summary>
        /// Составной порядковый элемент в очереди
        /// </summary>
        private struct QueuOrder
        {
            public int Order;
            public int SubOrder;
            public QueuOrder(int order, int subOrder = 0)
            {
                Order = order;
                SubOrder = subOrder;
            }
        }
    }

}
