using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;

namespace Continuum
{
    /// <summary>
    /// Manages one or many <see cref="IStateRecorder" /> instances by routing their buffers into a shared <see cref="ICaptureStream" />
    /// </summary>
    public class CaptureService : ICaptureService
    {
        /// <summary>
        /// Gets or sets the stream to write to.
        /// </summary>
        /// <value>
        /// The stream.
        /// </value>
        public IStream Stream { get; set; }

        /// <summary>
        /// Gets or sets the state resolver.
        /// </summary>
        /// <value>
        /// The state resolver.
        /// </value>
        public IStateResolver StateResolver { get; set; }

        /// <summary>
        /// Gets the capture stream.
        /// </summary>
        public ICaptureStream CaptureStream { get; private set; }

        private object _Lock;
        private List<IStateRecorder> _Recorders;
        private List<IStateController> _DummyControllers;

        /// <summary>
        /// Creates a new CaptureService backed by the specified <see cref="ICaptureStream" /> instance
        /// </summary>
        /// <param name="captureStream"></param>
        public CaptureService()
        {
            _Lock = new object();
            _Recorders = new List<IStateRecorder>();
            _DummyControllers = new List<IStateController>();

            this.StateResolver = new StateResolver();
        }

        private TimeSpan _OffsetDelay;

        public void Start()
        {
            //check if any dummy controllers need to be created to satisfy the CaptureStream
            foreach (var recorder in _Recorders)
            {
                if (this.StateResolver.Find(recorder.Guid) == null)
                {
                    var dummyController = new DummyController(recorder.Guid);
                    this.StateResolver.Add(dummyController);
                    _DummyControllers.Add(dummyController);
                }
            }

            CaptureStream = new CaptureStream(this.Stream, System.IO.FileAccess.Write, this.StateResolver);

            lock (_Lock)
            {
                var maxDelay = TimeSpan.Zero;
                
                foreach (var recorder in _Recorders)
                {
                    var start = DateTime.Now;
                    
                    recorder.Start();

                    var delay = DateTime.Now.Subtract(start);

                    if (delay > maxDelay)
                    {
                        maxDelay = delay;
                    }
                }

                _OffsetDelay = maxDelay;
            }

            this.IsStarted = true;
        }        

        public void Stop()
        {
            lock (_Lock)
            {
                foreach (var recorder in _Recorders)
                {
                    recorder.Stop();
                }
            }

            if (CaptureStream != null)
            {
                this.Flush();
            }

            lock (_Lock)
            {
                _Recorders.Clear();
            }

            _DummyControllers.ForEach(d => this.StateResolver.Remove(d));
            _DummyControllers.Clear();

            this.IsStarted = false;
        }

        public bool IsStarted { get; private set; }

        /// <summary>
        /// Adds an <see cref="IStateRecorder"/> to the service
        /// </summary>
        /// <param name="recorder">The recorder.</param>
        public void Add(IStateRecorder recorder)
        {
            lock (_Lock)
            {
                _Recorders.Add(recorder);                

                if (this.IsStarted)
                    recorder.Start();
            }
        }

        /// <summary>
        /// Removes an <see cref="IStateRecorder" /> from the service
        /// </summary>
        /// <param name="recorder"></param>
        public void Remove(IStateRecorder recorder)
        {
            lock (_Lock)
            {
                _Recorders.Remove(recorder);
            }
        }        

        /// <summary>
        /// Copies data from the <see cref="IStateRecorder" /> buffers into the backing stream
        /// </summary>
        public void Flush()
        {
            if (CaptureStream == null)
            {
                throw new Exception("Unable to flush buffers to a null CaptureStream");
            }

            lock (_Lock)
            {
                var toWrite = new List<ICaptureState>();

                foreach (var recorder in _Recorders)
                {
                    ICaptureState captureState = null;

                    while (recorder.Buffer.TryDequeue(out captureState))
                    {
                        if (TimeSpan.FromTicks(captureState.Timestamp.Ticks).Subtract(_OffsetDelay).TotalMilliseconds > 0)
                        {
                            captureState.Timestamp = captureState.Timestamp.Subtract(_OffsetDelay);
                        }

                        toWrite.Add(captureState);
                    }
                }

                if (toWrite.Any())
                {
                    //sort them by timestamp before writing to the stream
                    var orderedWrites = toWrite.OrderBy(o => o.Timestamp).ToArray();

                    foreach (var state in orderedWrites)
                    {
                        CaptureStream.Write(state);
                    }
                }
            }
        }        

        private class DummyController : IStateController
        {
            public Guid Guid { get; private set; }

            public DummyController(Guid guid)
            {
                this.Guid = guid;
            }

            public ICaptureState Create(byte[] data, DateTime timestamp, double offset)
            {
                throw new NotImplementedException();
            }

            public void Execute(ICaptureState captureState)
            {
                throw new NotImplementedException();
            }
        }
    }
}
