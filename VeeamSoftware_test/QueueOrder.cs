using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VeeamSoftware_test
{
    public class QueueOrder<T>
    {
        private Dictionary<QueuOrder, T> queueDictionary = new Dictionary<QueuOrder, T>();
        private Dictionary<int, int> subOrderLimits = new Dictionary<int, int>();
        private int currentOrder;
        private int currentsubOrder;
        private readonly object _lock = new object();

        public void Enqueue(int order, int subOrder, T item, bool lastSubOrder = false)
        {
            lock (_lock)
            {
                QueuOrder queuOrder = new QueuOrder(order, subOrder);
                if (lastSubOrder)
                    subOrderLimits[order] = subOrder;

                queueDictionary.Add(queuOrder, item);
            }
        }
        public void Enqueue(int order, T item)
        {
            Enqueue(order, 0, item, true);
        }
        public bool TryGetValue(out T item)
        {
            item = default(T);

            lock (_lock)
            {
                QueuOrder queuOrder = new QueuOrder(currentOrder, currentsubOrder);
                if (queueDictionary.TryGetValue(queuOrder, out item))
                {
                    queueDictionary.Remove(queuOrder);
                    if (subOrderLimits.ContainsKey(currentOrder) && currentsubOrder == subOrderLimits[currentOrder])
                    {
                        Interlocked.Increment(ref currentOrder);
                        currentsubOrder = 0;
                    }
                    else
                    {
                        Interlocked.Increment(ref currentsubOrder);
                    }
                    return true;
                }
            }

            return false;
        }
        public int Size
        {
            get { return queueDictionary.Count; }
        }
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
