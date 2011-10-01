using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Continuum.IO;
using System.IO;
using Moq;
using Continuum.Test.Mock;

namespace Continuum.Test
{
    public class CaptureWriterTests
    {
        [Fact]
        public void CaptureWriter_Buids_A_Valid_Continuum_Header()
        {
            var baseStream = new ConcurrentStream(new MemoryStream());
            var stateResolver = new StateResolver();

            var stateController = new WeatherStateController(new WeatherSimulator());

            stateResolver.Add(stateController);

            var captureWriter = new CaptureWriter(baseStream, stateResolver);

            baseStream.Position = 0;

            byte[] header = new byte[Constants.CONTINUUM_HEADER_SIGNATURE.Length];
            baseStream.Read(header, 0, header.Length);

            for (int i = 0; i < Constants.CONTINUUM_HEADER_SIGNATURE.Length; i++)
            {
                Assert.Equal(Constants.CONTINUUM_HEADER_SIGNATURE[i], header[i]);
            }

            //dont worry about 4 bytes reserved for version info at the moment
            baseStream.Position = baseStream.Position + 4;

            //ignore the timestamp for now
            baseStream.Position = baseStream.Position + 8;

            //ignore the count property for now
            baseStream.Position = baseStream.Position + 8;

            //ignore the length property for now
            baseStream.Position = baseStream.Position + 8;

            //read the SAT, there should be at least one entry for it to be valid
            //the SAT looks like this:
            //<lengthOfSac><firstId of type short><delimiter>

            var lengthOfSatBuffer = new byte[4];
            baseStream.Read(lengthOfSatBuffer, 0, lengthOfSatBuffer.Length);

            var lengthOfSat = BitConverter.ToInt32(lengthOfSatBuffer, 0);

            Assert.True(lengthOfSat > 0);

            //read back the sat
            var readSat = new Dictionary<short, Guid>();

            for (int i = 0; i < lengthOfSat; i++)
            {
                var idBuffer = new byte[2];
                baseStream.Read(idBuffer, 0, idBuffer.Length);

                var guid = new byte[16]; //guids are 16-bytes
                baseStream.Read(guid, 0, guid.Length);

                readSat.Add(BitConverter.ToInt16(idBuffer, 0), new Guid(guid));
            }

            //match the sat
            foreach (var kvp in stateResolver.AllocationTable)
            {
                Assert.True(readSat.ContainsKey(kvp.Value));
                Assert.True(readSat[kvp.Value] == kvp.Key);
            }
        }

        [Fact]
        public void CaptureWriter_Inserts_The_Correct_StateId_Into_The_Stream_During_Write_Operations()
        {
            var ms = new ConcurrentStream(new MemoryStream());
            
            var sat = new Dictionary<Guid, short>();
            sat.Add(Guid.NewGuid(), 24);
            sat.Add(Guid.NewGuid(), 25);
            sat.Add(Guid.NewGuid(), 26);

            var stateResolver = new Mock<IStateResolver>();
            stateResolver.SetupGet(s => s.AllocationTable).Returns(sat);
            stateResolver.Setup(s => s.IsAllocated(It.IsAny<Guid>())).Returns<Guid>(g => sat.ContainsKey(g));
            stateResolver.Setup(s => s.GetAllocatedId(It.IsAny<Guid>())).Returns<Guid>(g => sat[g]);

            var captureWriter = new CaptureWriter(ms, stateResolver.Object);
            var rewindPosition = ms.Position;
            var captureState = new Mock<ICaptureState>();
            captureState.Setup(c => c.Data).Returns(Guid.NewGuid().ToByteArray());
            captureState.Setup(c => c.Guid).Returns(sat.Where(f => f.Value == 24).First().Key);

            captureWriter.Write(captureState.Object);

            //rewind the stream and check the correct stateId was written
            ms.Position = rewindPosition;

            var stateBuffer = new byte[2];
            ms.Read(stateBuffer, 0, 2);

            Assert.Equal(24, BitConverter.ToInt16(stateBuffer, 0));
        }

        [Fact]
        public void CaptureWriter_Inserts_The_Correct_Timestamp_Into_The_Stream_During_Write_Operations()
        {
            var ms = new ConcurrentStream(new MemoryStream());

            var sat = new Dictionary<Guid, short>();
            sat.Add(Guid.NewGuid(), 24);
            sat.Add(Guid.NewGuid(), 25);
            sat.Add(Guid.NewGuid(), 26);

            var stateResolver = new Mock<IStateResolver>();
            stateResolver.SetupGet(s => s.AllocationTable).Returns(sat);
            stateResolver.Setup(s => s.IsAllocated(It.IsAny<Guid>())).Returns<Guid>(g => sat.ContainsKey(g));
            stateResolver.Setup(s => s.GetAllocatedId(It.IsAny<Guid>())).Returns<Guid>(g => sat[g]);

            var captureWriter = new CaptureWriter(ms, stateResolver.Object);
            var timestamp = DateTime.UtcNow;
            var rewindPosition = ms.Position;
            var captureState = new Mock<ICaptureState>();
            captureState.Setup(c => c.Data).Returns(Guid.NewGuid().ToByteArray());
            captureState.Setup(c => c.Guid).Returns(sat.Where(f => f.Value == 24).First().Key);
            captureState.Setup(c => c.Timestamp).Returns(timestamp);

            captureWriter.Write(captureState.Object);

            //rewind the stream and check the correct timestamp was written
            ms.Position = rewindPosition + 2;

            var timestampBuffer = new byte[8];
            ms.Read(timestampBuffer, 0, 8);

            Assert.Equal(timestamp, DateTime.FromBinary(BitConverter.ToInt64(timestampBuffer, 0)));
        }

        [Fact]
        public void CaptureWriter_Inserts_The_Correct_Data_Length_Into_The_Stream_During_Write_Operations()
        {
            var ms = new ConcurrentStream(new MemoryStream());

            var sat = new Dictionary<Guid, short>();
            sat.Add(Guid.NewGuid(), 24);
            sat.Add(Guid.NewGuid(), 25);
            sat.Add(Guid.NewGuid(), 26);

            var stateResolver = new Mock<IStateResolver>();
            stateResolver.SetupGet(s => s.AllocationTable).Returns(sat);
            stateResolver.Setup(s => s.IsAllocated(It.IsAny<Guid>())).Returns<Guid>(g => sat.ContainsKey(g));
            stateResolver.Setup(s => s.GetAllocatedId(It.IsAny<Guid>())).Returns<Guid>(g => sat[g]);

            var captureWriter = new CaptureWriter(ms, stateResolver.Object);
            var timestamp = DateTime.UtcNow;
            var rewindPosition = ms.Position;
            var captureState = new Mock<ICaptureState>();
            captureState.Setup(c => c.Data).Returns(Guid.NewGuid().ToByteArray());
            captureState.Setup(c => c.Guid).Returns(sat.Where(f => f.Value == 24).First().Key);
            captureState.Setup(c => c.Timestamp).Returns(timestamp);

            captureWriter.Write(captureState.Object);

            //rewind the stream and check the correct data length was written
            ms.Position = rewindPosition + 2 + 8;

            var dataLengthBuffer = new byte[4];
            ms.Read(dataLengthBuffer, 0, 4);

            Assert.Equal(Guid.NewGuid().ToByteArray().Length, BitConverter.ToInt32(dataLengthBuffer, 0));
        }

        [Fact]
        public void CaptureWriter_Inserts_The_Correct_Data_Into_The_Stream_During_Write_Operations()
        {
            var ms = new ConcurrentStream(new MemoryStream());

            var sat = new Dictionary<Guid, short>();
            sat.Add(Guid.NewGuid(), 24);
            sat.Add(Guid.NewGuid(), 25);
            sat.Add(Guid.NewGuid(), 26);

            var stateResolver = new Mock<IStateResolver>();
            stateResolver.SetupGet(s => s.AllocationTable).Returns(sat);
            stateResolver.Setup(s => s.IsAllocated(It.IsAny<Guid>())).Returns<Guid>(g => sat.ContainsKey(g));
            stateResolver.Setup(s => s.GetAllocatedId(It.IsAny<Guid>())).Returns<Guid>(g => sat[g]);

            var captureWriter = new CaptureWriter(ms, stateResolver.Object);
            var timestamp = DateTime.Now;
            var rewindPosition = ms.Position;

            var randomData = new byte[10000];
            new Random().NextBytes(randomData);

            var captureState = new Mock<ICaptureState>();
            captureState.Setup(c => c.Data).Returns(randomData);
            captureState.Setup(c => c.Guid).Returns(sat.Where(f => f.Value == 24).First().Key);
            captureState.Setup(c => c.Timestamp).Returns(timestamp);

            captureWriter.Write(captureState.Object);

            //rewind the stream and check the correct data length was written
            ms.Position = rewindPosition + 2 + 8;

            var dataLengthBuffer = new byte[4];
            ms.Read(dataLengthBuffer, 0, 4);
            var dataLength = BitConverter.ToInt32(dataLengthBuffer, 0);

            var dataBuffer = new byte[dataLength];
            ms.Read(dataBuffer, 0, dataLength);

            for (int i = 0; i < dataBuffer.Length; i++)
            {
                Assert.Equal(randomData[i], dataBuffer[i]);
            }
        }

        [Fact]
        public void CaptureWriter_Inserts_The_Correct_Data_Into_The_Stream_During_Multiple_Write_Operations()
        {
            var writeOperations = 100;
            var ms = new ConcurrentStream(new MemoryStream());

            var sat = new Dictionary<Guid, short>();
            sat.Add(Guid.NewGuid(), 24);
            sat.Add(Guid.NewGuid(), 25);
            sat.Add(Guid.NewGuid(), 26);

            var stateResolver = new Mock<IStateResolver>();
            stateResolver.SetupGet(s => s.AllocationTable).Returns(sat);
            stateResolver.Setup(s => s.IsAllocated(It.IsAny<Guid>())).Returns<Guid>(g => sat.ContainsKey(g));
            stateResolver.Setup(s => s.GetAllocatedId(It.IsAny<Guid>())).Returns<Guid>(g => sat[g]);

            var captureWriter = new CaptureWriter(ms, stateResolver.Object);

            for (int i = 0; i < writeOperations; i++)
            {
                var timestamp = DateTime.UtcNow;
                var rewindPosition = ms.Position;

                var randomData = new byte[1024];
                new Random().NextBytes(randomData);
                
                var randomSat = (short)new Random().Next(24, 26);

                var captureState = new Mock<ICaptureState>();
                captureState.Setup(c => c.Data).Returns(randomData);
                captureState.Setup(c => c.Guid).Returns(sat.Where(f => f.Value == randomSat).First().Key);
                captureState.Setup(c => c.Timestamp).Returns(timestamp);

                captureWriter.Write(captureState.Object);

                //rewind the stream and check the correct data length was written
                ms.Position = rewindPosition + 2 + 8;

                var dataLengthBuffer = new byte[4];
                ms.Read(dataLengthBuffer, 0, 4);
                var dataLength = BitConverter.ToInt32(dataLengthBuffer, 0);

                var dataBuffer = new byte[dataLength];
                ms.Read(dataBuffer, 0, dataLength);

                for (int j = 0; j < dataBuffer.Length; j++)
                {
                    Assert.Equal(randomData[j], dataBuffer[j]);
                }
            }
        }

        [Fact]
        public void CaptureWriter_Updates_The_Length_Of_The_Stream_During_Writes()
        {
            var ms = new ConcurrentStream(new MemoryStream());

            var sat = new Dictionary<Guid, short>();
            sat.Add(Guid.NewGuid(), 24);
            sat.Add(Guid.NewGuid(), 25);
            sat.Add(Guid.NewGuid(), 26);

            var stateResolver = new Mock<IStateResolver>();
            stateResolver.SetupGet(s => s.AllocationTable).Returns(sat);
            stateResolver.Setup(s => s.IsAllocated(It.IsAny<Guid>())).Returns<Guid>(g => sat.ContainsKey(g));
            stateResolver.Setup(s => s.GetAllocatedId(It.IsAny<Guid>())).Returns<Guid>(g => sat[g]);

            var captureWriter = new CaptureWriter(ms, stateResolver.Object);                        

            var lastLength = TimeSpan.Zero;

            for (int i = 0; i < 20; i++)
            {
                var timestamp = DateTime.UtcNow.AddSeconds(i * 10);
                var randomData = new byte[10000];
                new Random().NextBytes(randomData);

                var captureState = new Mock<ICaptureState>();
                captureState.Setup(c => c.Data).Returns(randomData);
                captureState.Setup(c => c.Guid).Returns(sat.Where(f => f.Value == 24).First().Key);
                captureState.Setup(c => c.Timestamp).Returns(timestamp);

                captureWriter.Write(captureState.Object);

                Assert.True(captureWriter.Length > lastLength);

                lastLength = captureWriter.Length;
            }
        }
    }
}
