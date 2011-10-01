using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Continuum.Test
{
    public class TypedEventArgs<T> : EventArgs
    {
        public T Value { get; private set; }

        public TypedEventArgs(T value)            
        {
            this.Value = value;
        }
    }
}
