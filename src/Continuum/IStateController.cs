using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;

namespace Continuum
{
    /// <summary>
    /// Base type that is capable of creating and executing ICaptureState instances
    /// </summary>
    public interface IStateController
    {
        /// <summary>
        /// Gets the GUID that uniquely identifies this IStateController instance
        /// </summary>
        Guid Guid { get; }

        /// <summary>
        /// Creates an ICaptureState instance given an array of data and a timestamp
        /// </summary>
        /// <param name="data"></param>
        /// <param name="timestamp"></param>
        /// <param name="offset">The number of milliseconds</param>
        /// <returns></returns>
        ICaptureState Create(byte[] data, DateTime timestamp, double offset);

        /// <summary>
        /// Excecutes the given ICaptureState instance
        /// </summary>
        void Execute(ICaptureState captureState);
    }
}
