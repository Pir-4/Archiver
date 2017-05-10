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
    public class QueueOrder<T>
    {
        private Dictionary<QueuOrder, T> _queueDictionary = new Dictionary<QueuOrder, T>();
        private Dictionary<int, int> _subOrderLimits = new Dictionary<int, int>();
        private int _currentOrder;
        private int _currentSubOrder;
        private readonly object _lock = new object();

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
            lock (_lock)
            {
                QueuOrder queuOrder = new QueuOrder(order, subOrder);
                if (lastSubOrder)
                    _subOrderLimits[order] = subOrder;

                _queueDictionary.Add(queuOrder, item);
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

            lock (_lock)
            {
                QueuOrder queuOrder = new QueuOrder(_currentOrder, _currentSubOrder);
                if (_queueDictionary.TryGetValue(queuOrder, out item))
                {
                    _queueDictionary.Remove(queuOrder);
                    if (_subOrderLimits.ContainsKey(_currentOrder) && _currentSubOrder == _subOrderLimits[_currentOrder])
                    {
                        Interlocked.Increment(ref _currentOrder);
                        _currentSubOrder = 0;
                    }
                    else
                    {
                        Interlocked.Increment(ref _currentSubOrder);
                    }
                    return true;
                }
            }

            return false;
        }
        public int Size
        {
            get { return _queueDictionary.Count; }
        }
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
