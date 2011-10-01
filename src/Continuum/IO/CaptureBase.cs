using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Continuum.IO
{
    public abstract class CaptureBase
    {
        /// <summary>
        /// Gets the position of the stream
        /// </summary>
        public virtual long Position { get; set; }

        /// <summary>
        /// Gets the count of the number of elements in the stream
        /// </summary>
        public virtual long Count { get; protected set; }

        /// <summary>
        /// Gets the length of the stream
        /// </summary>
        public virtual TimeSpan Length { get; protected set; }

        /// <summary>
        /// Gets the time the stream was created
        /// </summary>
        public virtual DateTime Timestamp { get; protected set; }

        /// <summary>
        /// Gets the version of Continuum the stream was created under
        /// </summary>
        public virtual Version Version { get; protected set; }

        /// <summary>
        /// Gets the underlying <see cref="IStream"/> instance
        /// </summary>
        public IStream BaseStream { get; protected set; }

        public CaptureBase(IStream baseStream)
        {
            this.BaseStream = baseStream;            
        }
    }
}
