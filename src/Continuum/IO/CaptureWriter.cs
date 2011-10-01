using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Continuum.IO
{
    /// <summary>
    /// Provides capabilities to write ICaptureState instances to a stream
    /// </summary>
    public class CaptureWriter : CaptureBase
    {
        public IStateResolver StateResolver { get; private set; }

        private long _CountPosition;
        private long _LengthPosition;

        public CaptureWriter(IStream baseStream, IStateResolver stateResolver)
            : base(baseStream)
        {
            this.StateResolver = stateResolver;
            this.Timestamp = DateTime.UtcNow;
            
            this.Version = new Version((int)Constants.CONTINUUM_VERSION[0], (int)Constants.CONTINUUM_VERSION[1], (int)Constants.CONTINUUM_VERSION[2], (int)Constants.CONTINUUM_VERSION[3]);

            try
            {
                //write the header
                this.BaseStream.Write(Constants.CONTINUUM_HEADER_SIGNATURE, 0, Constants.CONTINUUM_HEADER_SIGNATURE.Length);
                this.BaseStream.Write(Constants.CONTINUUM_VERSION, 0, Constants.CONTINUUM_VERSION.Length);
                this.BaseStream.Write(BitConverter.GetBytes(this.Timestamp.ToBinary()), 0, 8);

                _CountPosition = this.BaseStream.Position;

                //write a place holder for the count property
                this.BaseStream.Write(BitConverter.GetBytes(this.Count), 0, 8);

                _LengthPosition = this.BaseStream.Position;

                //write a place holder for the length property
                this.BaseStream.Write(BitConverter.GetBytes(this.Length.TotalMilliseconds), 0, 8);

                //write the state allocation table
                this.BaseStream.Write(BitConverter.GetBytes(this.StateResolver.AllocationTable.Count), 0, 4);

                foreach (var allocation in this.StateResolver.AllocationTable)
	            {
                    var idBytes = BitConverter.GetBytes(allocation.Value);
                    var guidBytes = allocation.Key.ToByteArray();

                    this.BaseStream.Write(idBytes, 0, idBytes.Length);
                    this.BaseStream.Write(guidBytes, 0, guidBytes.Length);
	            }
            }
            catch { }
        }

        public void Write(ICaptureState state)
        {
            if (!this.StateResolver.IsAllocated(state.Guid))
            {
                throw new Exception("Cannot resolve GUID in state allocation table");
            }

            //validate the given timestamp
            var binaryTimestamp = state.Timestamp.ToBinary();

            if (state.Timestamp < this.Timestamp)
            {
                binaryTimestamp = this.Timestamp.ToBinary();
            }

            //write data using this format:
            //stateId (2 bytes) timestamp (8 bytes) lengthOfState (4 bytes) state (<lengthOfState> bytes)

            byte[] stateId = BitConverter.GetBytes(this.StateResolver.GetAllocatedId(state.Guid));
            byte[] timestamp = BitConverter.GetBytes(binaryTimestamp);
            byte[] bytes = state.Data;
            byte[] lengthBytes = BitConverter.GetBytes(bytes.Length);

            this.BaseStream.Write(stateId, 0, stateId.Length);
            this.BaseStream.Write(timestamp, 0, timestamp.Length);
            this.BaseStream.Write(lengthBytes, 0, lengthBytes.Length);
            this.BaseStream.Write(bytes, 0, bytes.Length);

            this.Length = state.Timestamp.Subtract(this.Timestamp);
            this.Position++;
            this.Count++;

            //update the count in the header of the stream
            var currentPosition = this.BaseStream.Position;
            this.BaseStream.Position = _CountPosition;
            this.BaseStream.Write(BitConverter.GetBytes(this.Count), 0, 8);
            this.BaseStream.Position = currentPosition;

            //update the length in the header of the stream
            currentPosition = this.BaseStream.Position;
            this.BaseStream.Position = _LengthPosition;
            this.BaseStream.Write(BitConverter.GetBytes(this.Length.Ticks), 0, 8);
            this.BaseStream.Position = currentPosition;
        }        
    }
}
