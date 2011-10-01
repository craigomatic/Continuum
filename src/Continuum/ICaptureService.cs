using System;
using Continuum.IO;

namespace Continuum
{
    /// <summary>
    /// Provides a centralised structure that can marshal multiple <see cref="IStateRecorder"/> instances into the one <see cref="ICaptureStream"/> instance
    /// </summary>
    public interface ICaptureService
    {
        /// <summary>
        /// Starts all instances of <see cref="IStateRecorder" /> within the service
        /// </summary>
        void Start();

        /// <summary>
        /// Stops all instances of <see cref="IStateRecorder" /> within the service
        /// </summary>
        void Stop();

        bool IsStarted { get; }

        /// <summary>
        /// Adds an <see cref="IStateRecorder"/> to the service
        /// </summary>
        /// <param name="recorder">The recorder.</param>
        void Add(IStateRecorder recorder);

        /// <summary>
        /// Gets or sets the state resolver.
        /// </summary>
        /// <value>
        /// The state resolver.
        /// </value>
        IStateResolver StateResolver { get; set; }

        /// <summary>
        /// Gets or sets the stream to write to.
        /// </summary>
        /// <value>
        /// The stream.
        /// </value>
        IStream Stream { get; set; }

        /// <summary>
        /// Copies data from the <see cref="IStateRecorder" /> buffers into the backing stream
        /// </summary>
        void Flush();

        /// <summary>
        /// Removes an <see cref="IStateRecorder" /> from the service
        /// </summary>
        /// <param name="recorder"></param>
        void Remove(IStateRecorder recorder);
    }
}
