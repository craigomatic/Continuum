using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;

namespace Continuum.Tasks
{
    /// <summary>
    /// <see cref="ITask"/> implementation that executes an <see cref="ICaptureState"/> instance
    /// </summary>
    public class Task : ITask
    {
        public TimeSpan DesiredExecution { get; private set; }
        public IStateResolver StateResolver { get; private set; }
        public ICaptureState CaptureState { get; private set; }

        public Task(IStateResolver stateResolver, ICaptureState captureState, TimeSpan desiredExecution)
        {
            this.StateResolver = stateResolver;
            this.CaptureState = captureState;
            this.DesiredExecution = desiredExecution;
        }

        public void Execute()
        {
            var stateController = this.StateResolver.Find(this.CaptureState.Guid);

            if (stateController != null)
            {
                stateController.Execute(this.CaptureState);
            }
        }
    }
}
