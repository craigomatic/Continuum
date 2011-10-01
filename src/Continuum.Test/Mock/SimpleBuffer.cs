using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;

namespace Continuum.Test.Mock
{
    public class SimpleBuffer<T> : IBuffer<T>
    {
        public int Count
        {
            get { return _Queue.Count; }
        }

        private Queue<T> _Queue;

        public SimpleBuffer()
        {
            _Queue = new Queue<T>();
        }

        public void Enqueue(T obj)
        {
            _Queue.Enqueue(obj); 
        }

        public bool TryDequeue(out T obj)
        {
            try
            {
                obj = _Queue.Dequeue();
                return true;
            }
            catch { }

            obj = default(T);
            return false;
        }

        public bool TryPeek(out T obj)
        {
            try
            {
                obj = _Queue.Peek();
                return true;
            }
            catch { }

            obj = default(T);
            return false;
        }
    }
}
