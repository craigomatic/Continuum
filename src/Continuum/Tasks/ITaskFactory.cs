using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;

namespace Continuum.Tasks
{
    /// <summary>
    /// Builds <see cref="ITask"/> instances from <see cref="ICaptureState"/> instances
    /// </summary>
    public interface ITaskFactory
    {
        ITask Create(ICaptureState captureState);
    }
}
