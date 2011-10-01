using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;

namespace Continuum
{
    /// <summary>
    /// Provides functionality to start and stop state captures into a buffer.
    /// </summary>
    public interface IStateRecorder
    {
        /// <summary>
        /// Gets the collection of <see cref="ICaptureState" /> instances stored in the buffer
        /// </summary>
        IBuffer<ICaptureState> Buffer { get; }

        /// <summary>
        /// Gets the GUID.
        /// </summary>
        Guid Guid { get; }

        /// <summary>
        /// Gets the IsStarted state of the recorder
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Starts writing <see cref="ICaptureState" /> instances to the Buffer
        /// </summary>
        void Start();

        /// <summary>
        /// Stops writing <see cref="ICaptureState" /> instances to the buffer
        /// </summary>
        void Stop();
    }
}
