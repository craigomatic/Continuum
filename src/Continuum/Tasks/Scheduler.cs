using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Continuum.Tasks
{
    /// <summary>
    /// Basic implementation of <see cref="IScheduler"/> 
    /// </summary>
    public class Scheduler : IScheduler
    {
        /// <summary>
        /// Gets the scheduler's current time in ms
        /// </summary>
        public double Now
        {
            get 
            {
                if (!this.IsStarted)
                {
                    return -1;
                }

                return DateTime.Now.Subtract(this.Started.Add(_TimeSpentPaused)).TotalMilliseconds * this.Speed; 
            }
        }

        private double _Speed;

        /// <summary>
        /// Gets or sets the speed the scheduler should run at, where 1 is normal speed, 2 is twice as fast as normal and 0.5 is half as fast as normal.
        /// </summary>
        public double Speed
        {
            get { return _Speed; }
            set
            {
                //prevent invalid speed values
                if (double.IsNaN(value) || double.IsInfinity(value) || value == 0)
                {
                    value = 1f;
                }

                _Speed = value;
            }
        }

        /// <summary>
        /// Gets the time the scheduler was started
        /// </summary>
        public DateTime Started { get; protected set; }

        /// <summary>
        /// Gets a boolean indicating if the scheduler has tasks that are scheduled for execution in the future
        /// </summary>
        public bool HasScheduledTasks
        {
            get { return this.TaskCount > 0; }
        }

        /// <summary>
        /// Gets the count of tasks that are scheduled for execution in the future
        /// </summary>
        public int TaskCount
        {
            get 
            {
                lock (_QueueLock)
                {
                    return _TaskQueue.Count;
                }
            }
        }

        /// <summary>
        /// Gets a boolean indicating if the scheduler has started
        /// </summary>
        public bool IsStarted { get; protected set; }

        private bool _IsPaused;

        /// <summary>
        /// Gets or sets a value indicating whether the scheduler is paused.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is paused; otherwise, <c>false</c>.
        /// </value>
        public bool IsPaused
        {
            get { return _IsPaused; }
            set 
            {
                if (_IsPaused == value)
                {
                    return;
                }

                _IsPaused = value;

                if (_IsPaused)
                {
                    _TimeWhenPaused = DateTime.Now;
                }
                else
                {
                    _TimeSpentPaused.Add(DateTime.Now - _TimeWhenPaused);
                }
            }
        }

        /// <summary>
        /// The time it was when the playback was last paused
        /// </summary>
        private DateTime _TimeWhenPaused;

        /// <summary>
        /// The cumulative amount of time spend in the paused state
        /// </summary>
        private TimeSpan _TimeSpentPaused;

        protected SortedList<TimeSpan, ITask> _TaskQueue;
        protected object _QueueLock;        

        public Scheduler()
        {
            _QueueLock = new object();
            _TaskQueue = new SortedList<TimeSpan, ITask>();

            this.Speed = 1;
        }

        public virtual void Add(ITask task)
        {
            lock (_QueueLock)
            {
                var executionTime = task.DesiredExecution;

                while (_TaskQueue.ContainsKey(executionTime))
                {
                    //bump the desired execution later by 1ms to avoid duplicate keys in the _TaskQueue collection
                    executionTime = TimeSpan.FromMilliseconds(executionTime.TotalMilliseconds + 1);
                }

                _TaskQueue.Add(executionTime, task);

                Monitor.Pulse(_QueueLock);
            }
        }

        public virtual void Start()
        {
            var thread = new Thread(new ThreadStart(_ProcessTasks));
            thread.Name = "Capture Scheduler";

            lock (_QueueLock)
            {
                thread.Start();
            }
        }

        public virtual void Stop()
        {
            lock (_QueueLock)
            {
                _Continue = false;
                _TaskQueue.Clear();
                _TaskQueue.Add(TimeSpan.MinValue, null);

                Monitor.Pulse(_QueueLock);
            }
        }

        private bool _Continue;

        private void _ProcessTasks()
        {
            _Continue = true;

            this.Started = DateTime.Now;
            this.IsStarted = true;

            while (_Continue)
            {
                lock (_QueueLock)
                {
                    //if the next task is due, execute it
                    var nextTask = _PeekNextTask();

                    if (nextTask != null)
                    {
                        if (this.Now > nextTask.DesiredExecution.TotalMilliseconds)
                        {
                            //run the task then remove it from the task queue
                            try
                            {
                                nextTask.Execute();
                            }
                            catch
                            {
                                System.Diagnostics.Debug.WriteLine(string.Format("The task scheduled at {0}ms did not execute correctly.", nextTask.DesiredExecution.TotalMilliseconds));
                            }

                            System.Diagnostics.Debug.WriteLine(string.Format("Task executed at {0}ms", this.Now));

                            _TaskQueue.RemoveAt(0);
                        }
                        else
                        {
                            var timeToNextTask = nextTask.DesiredExecution.TotalMilliseconds - this.Now;

                            if (timeToNextTask > 10)
                            {
                                Thread.Sleep((int)timeToNextTask - 2);//wait until 2ms before the next task to wake
                            }
                        }
                    }
                }
            }

            this.IsStarted = false;
        }

        private ITask _PeekNextTask()
        {
            lock (_QueueLock)
            {
                while (_TaskQueue.Count == 0)
                {
                    Monitor.Wait(_QueueLock);
                }

                return _TaskQueue.First().Value;
            }
        }        
    }
}
