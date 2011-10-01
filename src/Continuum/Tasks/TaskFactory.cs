using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;

namespace Continuum.Tasks
{
    /// <summary>
    /// <see cref="ITaskFactory"/> implementation that 
    /// </summary>
    public class TaskFactory : ITaskFactory
    {
        public IStateResolver StateResolver { get; private set; }

        public TaskFactory(IStateResolver stateResolver)
        {
            this.StateResolver = stateResolver;
        }

        public ITask Create(ICaptureState captureState)
        {
            return new Task(this.StateResolver, captureState, TimeSpan.FromMilliseconds(captureState.Offset));
        }
    }
}
