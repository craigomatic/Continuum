using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Continuum.IO
{
    /// <summary>
    /// Stores an arbitrary state
    /// </summary>
    public interface ICaptureState
    {
        /// <summary>
        /// Gets or sets the offset (in milliseconds) this state occurred from the time the capture began
        /// </summary>
        double Offset { get; set; }

        /// <summary>
        /// Gets or sets UTC-0 timestamp as to when the state was created
        /// </summary>
        DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the GUID associated with the capture state 
        /// </summary>
        Guid Guid { get; set; }

        /// <summary>
        /// Gets the raw data associated with the capture state
        /// </summary>
        byte[] Data { get; }

        /// <summary>
        /// Gets or sets the length of the state in bytes.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        long Length { get; set; }
    }
}
