using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;
using Continuum.Tasks;
using Continuum.Filters;

namespace Continuum
{
    /// <summary>
    /// Supports playback of <see cref="ICaptureStream" /> instances
    /// </summary>
    public class PlaybackService
    {
        #region Events
        
        public event EventHandler<CodecRequiredEventArgs> CodecRequired;

        protected virtual void OnCodecRequired(CodecRequiredEventArgs e)
        {
            if (CodecRequired != null)
            {
                CodecRequired(this, e);
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="PlaybackService"/> should loop.
        /// </summary>
        /// <value>
        ///   <c>true</c> if loop; otherwise, <c>false</c>.
        /// </value>
        public bool Loop { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is paused.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is paused; otherwise, <c>false</c>.
        /// </value>
        public bool IsPaused
        {
            get { return this.Scheduler.IsPaused; }
            set { this.Scheduler.IsPaused = value; }
        }

        /// <summary>
        /// Gets the length of time playback will take at normal speed.
        /// </summary>
        public TimeSpan Length
        {
            get
            {
                return _Captures.Max(s => s.Length);
            }
        }

        /// <summary>
        /// Gets the state resolver.
        /// </summary>
        public IStateResolver StateResolver { get; private set; }

        /// <summary>
        /// Gets the scheduler.
        /// </summary>
        public IScheduler Scheduler { get; private set; }

        /// <summary>
        /// Gets the task factory.
        /// </summary>
        public ITaskFactory TaskFactory { get; private set; }

        private List<ICaptureStream> _Captures;
        private List<IStreamFilter> _Filters;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaybackService"/> class.
        /// </summary>
        /// <param name="stateResolver">The state resolver.</param>
        public PlaybackService(IStateResolver stateResolver)
        {
            _Captures = new List<ICaptureStream>();
            _Filters = new List<IStreamFilter>();

            this.Scheduler = new Scheduler();
            this.StateResolver = stateResolver;
            this.TaskFactory = new TaskFactory(stateResolver);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaybackService"/> class.
        /// </summary>
        /// <param name="stateResolver">The state resolver.</param>
        /// <param name="scheduler">The scheduler.</param>
        /// <param name="taskFactory">The task factory.</param>
        public PlaybackService(IStateResolver stateResolver, IScheduler scheduler, ITaskFactory taskFactory)
            :this(stateResolver)
        {
            this.Scheduler = scheduler;
            this.TaskFactory = taskFactory;
        }

        /// <summary>
        /// Adds the specified stream for playback.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void Add(ICaptureStream stream)
        {
            if (!stream.CanRead)
            {
                throw new Exception("Cannot read from stream");
            }

            _Captures.Add(stream);

            //check we have all of the required codec's loaded

            foreach (var codec in stream.Codecs)
	        {
                if (this.StateResolver.Find(codec) == null)
                {
                    //not found, need to acquire it
                    OnCodecRequired(new CodecRequiredEventArgs(codec));
                }
	        }                      
        }

        /// <summary>
        /// Adds the specified filter to be applied during playback.
        /// </summary>
        /// <param name="filter">The filter.</param>
        public void Add(IStreamFilter filter)
        {
            _Filters.Add(filter);            
        }

        /// <summary>
        /// Removes the specified filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        public void Remove(IStreamFilter filter)
        {
            _Filters.Remove(filter);
        }

        /// <summary>
        /// Starts playback.
        /// </summary>
        public void Start()
        {
            //basic implementation for now, queue all of the capture states so that the scheduler will process them
            foreach (var capture in _Captures)
            {
                for (long i = capture.Position; i < capture.Count; i++)
                {
                    try
                    {
                        var state = capture.Read();
                        var skipState = false;

                        foreach (var filter in _Filters)
                        {
                            if (filter.Filter(state))
                            {
                                skipState = true;
                                break;
                            }
                        }

                        if (!skipState)
                        {
                            var task = this.TaskFactory.Create(state);
                            this.Scheduler.Add(task);
                        }
                    }
                    catch { }
                }
            }

            this.Scheduler.Start();
        }

        /// <summary>
        /// Stops playback.
        /// </summary>
        public void Stop()
        {
            this.Scheduler.Stop();
        }
    }

    public class CodecRequiredEventArgs : EventArgs
    {
        public Guid Guid { get; private set; }
        
        public CodecRequiredEventArgs(Guid guid)
        {
            this.Guid = guid;
        }
    }
}
