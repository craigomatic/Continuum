using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Continuum.IO
{
    /// <summary>
    /// Provides capabilities to read data from a stream into ICaptureState instances
    /// </summary>
    public class CaptureReader : CaptureBase
    {
        public override long Position
        {
            get { return base.Position; }
            set
            {
                if (this.Position == value)
                {
                    return;
                }

                if (value > this.Count)
                {
                    throw new Exception("Position is outside the range of valid values.");
                }

                //moving to position x means that it is expected to read the node at that position
                //ie: Position = 0 reads the first node in the stream
                //Position = 1 reads the 2nd node in the stream
                //Position = 2 reads the 3rd node in the stream
                // and so on
                
                //TODO: improve this naive implementation

                if (this.Position > value)
                {
                    //TODO:read backwards to the desired position

                    //for now, just point the base stream back to the end of the header and read forwards from there
                    this.BaseStream.Position = _PositionOfFirstState;
                    base.Position = 0;
                }
                
                while (this.Position < value)
                {
                    var state = this.Read(); //reads update the position of the stream

                    //TODO: consider a way that the IStateController can decide if the states need to be executed when seeking
                    var controller = this.StateResolver.Find(state.Guid);
                    controller.Execute(state);
                }
            }
        }

        public IStateResolver StateResolver { get; private set; }

        public Dictionary<short, Guid> StateAllocationTable { get; private set; }

        private long _PositionOfFirstState;

        /// <summary>
        /// Creates a new instance of a CaptureReader for the specified baseStream instance using the specified state resolver
        /// </summary>
        /// <param name="baseStream">Base stream to read data from</param>
        /// <param name="stateResolver">IStateResolver instance to use when resolving ICaptureStates from the stream</param>
        public CaptureReader(IStream baseStream, IStateResolver stateResolver)
            :base(baseStream)
        {
            this.StateResolver = stateResolver;

            this.StateAllocationTable = new Dictionary<short, Guid>();

            //test if the stream contains a valid continuum stream by looking for the header signature
            try
            {
                byte[] header = new byte[Constants.CONTINUUM_HEADER_SIGNATURE.Length];
                this.BaseStream.Read(header, 0, header.Length);

                for (int i = 0; i < Constants.CONTINUUM_HEADER_SIGNATURE.Length; i++)
                {
                    if (header[i] != Constants.CONTINUUM_HEADER_SIGNATURE[i])
                        throw new Exception("Stream does not contain a valid continuum header");
                }

                //read the version
                byte[] version = new byte[4];
                this.BaseStream.Read(version, 0, version.Length);
                this.Version = new Version((int)version[0], (int)version[1], (int)version[2], (int)version[3]);

                //read the timestamp
                byte[] timestamp = new byte[8];
                this.BaseStream.Read(timestamp, 0, timestamp.Length);
                this.Timestamp = DateTime.FromBinary(BitConverter.ToInt64(timestamp, 0));

                //read the count 
                var count = new byte[8];
                this.BaseStream.Read(count, 0, count.Length);
                this.Count = BitConverter.ToInt64(count, 0);

                //read the length
                var length = new byte[8];
                this.BaseStream.Read(length, 0, length.Length);
                this.Length = TimeSpan.FromTicks(BitConverter.ToInt64(length, 0));

                //read the SAT, there should be at least one entry for it to be valid
                //the SAT looks like this:
                //<lengthOfSac><firstId of type short><delimiter>
                
                var lengthOfSatBuffer = new byte[4];
                this.BaseStream.Read(lengthOfSatBuffer, 0, lengthOfSatBuffer.Length);

                var lengthOfSat = BitConverter.ToInt32(lengthOfSatBuffer, 0);

                if (lengthOfSat <= 0)
                {
                    throw new Exception("Stream does not contain a valid State Allocation Table");
                }

                //build out the SAT
                for (int i = 0; i < lengthOfSat; i++)
                {
                    var idBuffer = new byte[2];
                    this.BaseStream.Read(idBuffer, 0, idBuffer.Length);

                    var guid = new byte[16]; //guids are 16-bytes
                    this.BaseStream.Read(guid, 0, guid.Length);

                    this.StateAllocationTable.Add(BitConverter.ToInt16(idBuffer, 0), new Guid(guid));
                }

                _PositionOfFirstState = this.BaseStream.Position;
            }
            catch
            {
                this.BaseStream.Close();
                throw new Exception("Stream does not contain a valid continuum header");
            }
        }

        /// <summary>
        /// Reads the next capture state in the sequence given the current position
        /// </summary>
        /// <returns>An ICaptureState instance</returns>
        public ICaptureState Read()
        {
            //TODO: sync the position of the base stream with this.Position, currently only supports sequential reads

            //read data expected in this format
            //stateId (2 byte) timestamp (8 bytes) lengthOfState (4 bytes) state (<lengthOfState> bytes)
            var stateId = BitConverter.ToInt16(ByteReader.Read(this.BaseStream, 2), 0);
            var timestamp = DateTime.FromBinary(BitConverter.ToInt64(ByteReader.Read(this.BaseStream, 8), 0));
            var lengthToRead = BitConverter.ToInt32(ByteReader.Read(this.BaseStream, 4), 0);

            if (lengthToRead > this.BaseStream.Length - this.BaseStream.Position)
            {
                throw new Exception("Invalid length specified in header of state");
            }

            var stateBuffer = ByteReader.Read(this.BaseStream, lengthToRead);

            base.Position++;

            if (!StateAllocationTable.ContainsKey(stateId))
            {
                throw new Exception(string.Format("Unexpected state identifier encountered: ", stateId));
            }

            var stateGuid = StateAllocationTable[stateId];

            //find the correct IStateController to build the ICaptureState for this data
            var controller = this.StateResolver.Find(stateGuid);

            if (controller == null)
            {
                throw new Exception(string.Format("State controller was not found for GUID: {0}", stateGuid));
            }

            //offset is the difference between the capture's timestamp and this state
            var offset = timestamp.Subtract(this.Timestamp).TotalMilliseconds;

            System.Diagnostics.Debug.Assert(offset >= 0);

            var state = controller.Create(stateBuffer, timestamp, offset);
            state.Length = lengthToRead;
            return state;
        }

        /// <summary>
        /// Gets the next <see cref="ICaptureState"/>. Does not advance the read pointer from the current position.
        /// </summary>
        /// <returns></returns>
        public ICaptureState Peek()
        {
            var intialPosition = this.BaseStream.Position;

            try
            {
                while (this.BaseStream.Position < this.BaseStream.Length)
                {
                    var stateId = BitConverter.ToInt16(ByteReader.Read(this.BaseStream, 2), 0);
                    var timestamp = DateTime.FromBinary(BitConverter.ToInt64(ByteReader.Read(this.BaseStream, 8), 0));
                    var lengthToRead = BitConverter.ToInt32(ByteReader.Read(this.BaseStream, 4), 0);

                    if (lengthToRead > this.BaseStream.Length - this.BaseStream.Position)
                    {
                        return null;
                    }

                    var stateBuffer = ByteReader.Read(this.BaseStream, lengthToRead);

                    if (StateAllocationTable.ContainsKey(stateId))
                    {
                        var stateGuid = StateAllocationTable[stateId];
                        var controller = this.StateResolver.Find(stateGuid);

                        if (controller != null)
                        {
                            var offset = timestamp.Subtract(this.Timestamp).TotalMilliseconds;

                            var state = controller.Create(stateBuffer, timestamp, offset);
                            state.Length = lengthToRead;
                            return state;
                        }
                    }
                }
            }
            catch { } //suppress anything nasty that happens in here
            finally
            {
                this.BaseStream.Position = intialPosition;
            }

            return null;
        }

        /// <summary>
        /// Gets the next <see cref="ICaptureState"/> found that matches the specified Guid. Does not advance the read pointer from the current position.
        /// </summary>
        /// <returns></returns>
        public ICaptureState Peek(Guid guid)
        {
            var intialPosition = this.BaseStream.Position;

            try
            {
                while (this.BaseStream.Position < this.BaseStream.Length)
                {
                    var stateId = BitConverter.ToInt16(ByteReader.Read(this.BaseStream, 2), 0);
                    var timestamp = DateTime.FromBinary(BitConverter.ToInt64(ByteReader.Read(this.BaseStream, 8), 0));
                    var lengthToRead = BitConverter.ToInt32(ByteReader.Read(this.BaseStream, 4), 0);

                    if (lengthToRead > this.BaseStream.Length - this.BaseStream.Position)
                    {
                        return null;
                    }

                    var stateBuffer = ByteReader.Read(this.BaseStream, lengthToRead);

                    if (StateAllocationTable.ContainsKey(stateId) && StateAllocationTable[stateId] == guid)
                    {
                        var controller = this.StateResolver.Find(guid);

                        if (controller != null)
                        {
                            var offset = timestamp.Subtract(this.Timestamp).TotalMilliseconds;

                            var state = controller.Create(stateBuffer, timestamp, offset);
                            state.Length = lengthToRead;
                            return state;
                        }
                    }
                }
            }
            catch { } //suppress anything nasty that happens in here
            finally
            {
                this.BaseStream.Position = intialPosition;
            }

            return null;
        }
    }
}
