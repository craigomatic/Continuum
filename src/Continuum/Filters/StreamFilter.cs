using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;

namespace Continuum.Filters
{
    /// <summary>
    /// Generic filter that evaluates based on a predicate
    /// </summary>
    public class StreamFilter : IStreamFilter
    {
        public Predicate<ICaptureState> Predicate { get; private set; }

        public StreamFilter(Predicate<ICaptureState> predicate)
        {
            this.Predicate = predicate;
        }

        public bool Filter(ICaptureState captureState)
        {
            return Predicate(captureState);
        }
    }
}
