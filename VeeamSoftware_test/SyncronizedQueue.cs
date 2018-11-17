using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipTest
{
    public class SyncronizedQueue<T> where T: class 
    {
        private class Node
        {
            public Node Next;
            public long Id;
            public T Data;
        }

        private Node _head;
        private int _count;
        private long _expected;
        private bool _isBreak;

        public SyncronizedQueue(int maxSize = 15)
        {
            MaxSize = maxSize;
        }

        public int MaxSize { get; set; }

        public void Enqueue(T data, long id)
        {
            lock (this)
            {
                while (!_isBreak &&
                    (Volatile.Read(ref _count) >= MaxSize || id != Interlocked.Read(ref _expected)))
                {
                    Monitor.Wait(this);
                }

                if (_isBreak)
                {
                    return;
                }

                var node = new Node {Data = data, Id = id};
                if (_head == null)
                {
                    _head = node;
                }
                else
                {
                    var current = _head;
                    while (current.Next != null)
                    {
                        current = current.Next;
                    }
                    current.Next = node;
                }

                Interlocked.Increment(ref _count);
                Interlocked.Increment(ref _expected);
                Monitor.PulseAll(this);
            }
        }

        public bool TryGetValue(out T data, out long id)
        {
            lock (this)
            {
                while (!_isBreak && Volatile.Read(ref _count) <= 0)
                {
                    Monitor.Wait(this);
                }

                if (_isBreak)
                {
                    data = null;
                    id = 0;
                    return false;
                }

                var result = Volatile.Read(ref _count) > 0;

                data = _head?.Data;
                id = _head?.Id ?? 0;
                if (result)
                {
                    _head = _head?.Next;
                    Interlocked.Decrement(ref _count);
                }
                Monitor.PulseAll(this);
                return result;
            }
        }

        public void Break()
        {
            lock (this)
            {
                _isBreak = true;
                Monitor.PulseAll(this);
            }
        }
    }
}