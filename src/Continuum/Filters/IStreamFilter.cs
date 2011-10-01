using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;

namespace Continuum.Filters
{
    /// <summary>
    /// Represents a filter that can manipulate a capture state.
    /// </summary>
    public interface IStreamFilter
    {
        /// <summary>
        /// Performs a filter operation on the specified capture state.
        /// </summary>
        /// <param name="captureState">Capture state.</param>
        /// <returns>True if the capture state should be dropped, false if not</returns>
        bool Filter(ICaptureState captureState);
    }
}
