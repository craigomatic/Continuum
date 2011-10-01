using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Continuum.Tasks
{
    /// <summary>
    /// Represents a task that is to be executed at a specific time
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// Gets the normalised time offset the task should be executed at, ie: the offset where time is zero
        /// </summary>
        TimeSpan DesiredExecution { get; }

        /// <summary>
        /// Executes the task
        /// </summary>
        void Execute();
    }
}
