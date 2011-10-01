using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Continuum.IO
{
    /// <summary>
    /// Represents a capture stream where <see cref="ICaptureState" /> instances can be read from or recorded to
    /// </summary>
    public interface ICaptureStream : IDisposable
    {
        /// <summary>
        /// Gets the CanRead state of the stream
        /// </summary>
        bool CanRead { get; }

        /// <summary>
        /// Gets the CanWrite state of the stream
        /// </summary>
        bool CanWrite { get; }

        /// <summary>
        /// Gets the codecs used in this stream.
        /// </summary>
        IList<Guid> Codecs { get; }

        /// <summary>
        /// Gets the number of <see cref="ICaptureState" /> instances stored in the stream
        /// </summary>
        long Count { get; }

        /// <summary>
        /// Gets the index of the current <see cref="ICaptureState" />
        /// </summary>
        long Position { get; }

        /// <summary>
        /// Gets the length of time the stream occupies when played back at normal speed
        /// </summary>
        TimeSpan Length { get; }

        /// <summary>
        /// Gets the time the stream was created
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Gets the version of Continuum the stream is
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Gets the next <see cref="ICaptureState"/>. Does not advance the read pointer from the current position.
        /// </summary>
        /// <returns></returns>
        ICaptureState Peek();

        /// <summary>
        /// Gets the next <see cref="ICaptureState"/> found that matches the specified Guid. Does not advance the read pointer from the current position.
        /// </summary>
        /// <returns></returns>
        ICaptureState Peek(Guid guid);

        /// <summary>
        /// Reads the next <see cref="ICaptureState" /> instance from the stream, advancing the read pointer.
        /// </summary>
        /// <returns></returns>
        ICaptureState Read();

        /// <summary>
        /// Writes an <see cref="ICaptureState" /> instance into the stream
        /// </summary>
        /// <param name="state"></param>
        void Write(ICaptureState state);
    }
}
