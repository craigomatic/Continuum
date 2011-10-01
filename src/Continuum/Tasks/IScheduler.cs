using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Continuum.Tasks
{
    /// <summary>
    /// Represents a scheduler that accepts ITask instances to be executed at a point in the future
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Gets a boolean indicating if the scheduler has started.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the scheduler is paused.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is paused; otherwise, <c>false</c>.
        /// </value>
        bool IsPaused { get; set; }

        /// <summary>
        /// Gets a boolean indicating if the scheduler has tasks that are scheduled for execution in the future
        /// </summary>
        bool HasScheduledTasks { get; }

        /// <summary>
        /// Gets the count of tasks that are scheduled for execution in the future
        /// </summary>
        int TaskCount { get; }

         /// <summary>
        /// Gets the scheduler's current time in ms
        /// </summary>
        double Now { get; }

        /// <summary>
        /// Gets or sets the speed the scheduler should run at, where 1 is normal speed
        /// </summary>
        double Speed { get; set; }

        /// <summary>
        /// Adds an <see cref="ITask"/> to the scheduler
        /// </summary>
        /// <param name="task"></param>
        void Add(ITask task);

        /// <summary>
        /// Starts processing <see cref="ITask"/> instances according to their DesiredExecution times
        /// </summary>
        void Start();

        /// <summary>
        /// Stops processing <see cref="ITask"/> instances
        /// </summary>
        void Stop();

    }
}
