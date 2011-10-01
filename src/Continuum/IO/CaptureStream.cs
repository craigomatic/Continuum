using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;

namespace Continuum.IO
{
    public class CaptureStream : ICaptureStream
    {
        #region Properties

        /// <summary>
        /// Gets the CanRead state of the stream
        /// </summary>
        public bool CanRead
        {
            get
            {
                return _CaptureReader != null && _CaptureReader.BaseStream.CanRead;
            }
        }

        /// <summary>
        /// Gets the CanWrite state of the stream
        /// </summary>
        public bool CanWrite
        {
            get
            {
                return _CaptureWriter != null && _CaptureWriter.BaseStream.CanWrite;
            }
        }

        /// <summary>
        /// Gets the codecs used in this stream.
        /// </summary>
        public IList<Guid> Codecs
        {
            get
            {
                if (_CaptureReader == null)
                {
                    return new List<Guid>();
                }

                return _CaptureReader.StateAllocationTable.Values.ToList();
            }
        }

        /// <summary>
        /// Gets the time the capture stream was created
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Gets the number of ICaptureState instances stored in the stream
        /// </summary>
        public long Count
        {
            get
            {
                if (_CaptureWriter != null)
                {
                    return _CaptureWriter.Count;
                }
                else
                {
                    return _CaptureReader.Count;
                }
            }
        }

        /// <summary>
        /// Gets the position of the stream
        /// </summary>
        public long Position
        {
            get
            {
                if (_CaptureWriter != null)
                {
                    return _CaptureWriter.Position;
                }
                else
                {
                    return _CaptureReader.Position;
                }
            }

            set
            {
                if (_CaptureWriter != null)
                {
                    _CaptureWriter.Position = value;

                    //update the read/write pointers
                    _ReadPointer = _CaptureWriter.BaseStream.Position;
                    _WritePointer = _CaptureWriter.BaseStream.Position;
                }
                else
                {
                    _CaptureReader.Position = value;

                    //update the read/write pointers
                    _ReadPointer = _CaptureReader.BaseStream.Position;
                    _WritePointer = _CaptureReader.BaseStream.Position;
                }                
            }
        }

        /// <summary>
        /// Gets the length of the stream
        /// </summary>
        public TimeSpan Length
        {
            get
            {
                if (_CaptureWriter != null)
                {
                    return _CaptureWriter.Length;
                }
                else
                {
                    return _CaptureReader.Length;
                }
            }
        }

        /// <summary>
        /// Gets the version of Continuum the stream is
        /// </summary>
        public Version Version
        {
            get
            {
                if (_CaptureWriter != null)
                {
                    return _CaptureWriter.Version;
                }
                else
                {
                    return _CaptureReader.Version;
                }
            }
        }

        public IStateResolver StateResolver { get; private set; }        

        #endregion

        private CaptureReader _CaptureReader;
        private CaptureWriter _CaptureWriter;
        private object _Lock;

        private long _ReadPointer;
        private long _WritePointer;
        
        /// <summary>
        /// Creates a new instance of the CaptureStream with the specified base stream and mode
        /// </summary>
        /// <param name="stream"></param>
        public CaptureStream(IStream baseStream, System.IO.FileAccess fileAccess, IStateResolver stateResolver)
        {
            switch (fileAccess)
            {
                case System.IO.FileAccess.Read:
                    {                        
                        _CaptureReader = new CaptureReader(baseStream, stateResolver);

                        this.Timestamp = _CaptureReader.Timestamp;
                        break;
                    }
                case System.IO.FileAccess.Write:
                    {
                        _CaptureWriter = new CaptureWriter(baseStream, stateResolver);

                        this.Timestamp = _CaptureWriter.Timestamp;
                        break;
                    }
                default:
                    {
                        var streamPosition = baseStream.Position;

                        //important to create writer first, it will create the header the reader expects (if required)
                        _CaptureWriter = new CaptureWriter(baseStream, stateResolver);

                        //put the position back to the original position
                        baseStream.Position = streamPosition;

                        //reader now has a valid continuum file to play
                        _CaptureReader = new CaptureReader(baseStream, stateResolver);                        
                        
                        this.Timestamp = _CaptureWriter.Timestamp;

                        break;
                    }
            }

            this.StateResolver = stateResolver;

            //store the read/write pointers
            _ReadPointer = baseStream.Position;
            _WritePointer = baseStream.Position;
            _Lock = new object();
        }

         /// <summary>
        /// Gets the next <see cref="ICaptureState"/>. Does not advance the read pointer from the current position.
        /// </summary>
        /// <returns></returns>
        public ICaptureState Peek()
        {
            lock (_Lock)
            {
                if (!this.CanRead)
                {
                    throw new Exception("Cannot read from underlying stream");
                }

                //put the stream at the correct read position, peek will put it back here after, no need for housekeeping
                _CaptureReader.BaseStream.Position = _ReadPointer;

                return _CaptureReader.Peek();
            }
        }

        /// <summary>
        /// Gets the next <see cref="ICaptureState"/> found that matches the specified Guid. Does not advance the read pointer from the current position.
        /// </summary>
        /// <returns></returns>
        public ICaptureState Peek(Guid guid)
        {
            lock (_Lock)
            {
                if (!this.CanRead)
                {
                    throw new Exception("Cannot read from underlying stream");
                }

                //put the stream at the correct read position, peek will put it back here after, no need for housekeeping
                _CaptureReader.BaseStream.Position = _ReadPointer;
                
                return _CaptureReader.Peek(guid);
            }
        }

        public ICaptureState Read()
        {
            lock (_Lock)
            {
                if (!this.CanRead)
                {
                    throw new Exception("Cannot read from underlying stream");
                }

                //put the stream at the correct read position, update the pointer to the new position after it has been read
                _CaptureReader.BaseStream.Position = _ReadPointer;

                var state = _CaptureReader.Read();

                _ReadPointer = _CaptureReader.BaseStream.Position;
                
                return state;
            }
        }

        public void Write(ICaptureState state)
        {
            lock (_Lock)
            {
                if (!this.CanWrite)
                {
                    throw new Exception("Cannot write to underlying stream");
                }

                //put the stream at the correct write position, a reader may have been reading from somewhere else in the stream
                _CaptureWriter.BaseStream.Position = _WritePointer;

                _CaptureWriter.Write(state);

                _WritePointer = _CaptureWriter.BaseStream.Position;
            }
        }

        public void Dispose()
        {
            lock (_Lock)
            {
                if (_CaptureReader != null)
                {
                    _CaptureReader.BaseStream.Dispose();
                }

                if (_CaptureWriter != null)
                {
                    _CaptureWriter.BaseStream.Dispose();
                }
            }
        }        
    }
}
