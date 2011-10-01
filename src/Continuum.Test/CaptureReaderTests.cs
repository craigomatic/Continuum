using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Continuum.IO;
using System.IO;
using Moq;
using Continuum.Test.Mock;
using System.Reflection;

namespace Continuum.Test
{
    public class CaptureReaderTests
    {
        private byte[] _CreateContinuumHeader(long count, long lengthTicks)
        {
            var ms = new MemoryStream();
            ms.Write(Constants.CONTINUUM_HEADER_SIGNATURE, 0, Constants.CONTINUUM_HEADER_SIGNATURE.Length);
            ms.Write(Constants.CONTINUUM_VERSION, 0, Constants.CONTINUUM_VERSION.Length);
            ms.Write(BitConverter.GetBytes(DateTime.UtcNow.ToBinary()), 0, 8);
            ms.Write(BitConverter.GetBytes(count), 0, 8);
            ms.Write(BitConverter.GetBytes(lengthTicks), 0, 8);

            return ms.ToArray();
        }

        private byte[] _CreateSAT(int numberOfStateAllocations)
        {
            var ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes(numberOfStateAllocations), 0, 4);

            for (short i = 0; i < numberOfStateAllocations; i++)
            {
                var idBytes = BitConverter.GetBytes(i);
                var guidBytes = Guid.NewGuid().ToByteArray();
                
                ms.Write(idBytes, 0, idBytes.Length);
                ms.Write(guidBytes, 0, guidBytes.Length);
            }

            return ms.ToArray();
        }

        [Fact]
        public void CaptureReader_Loads_A_Valid_Continuum_Stream()
        {
            var satCount = 5;
            var header = _CreateContinuumHeader(1000, 10000);
            var sat = _CreateSAT(satCount);

            var baseStream = new ConcurrentStream(new MemoryStream());
            baseStream.Write(header.ToArray(), 0, header.Length);
            baseStream.Write(sat, 0, sat.Length);
            baseStream.Position = 0;

            var stateController = new Mock<IStateController>();
            var stateResolver = new Mock<IStateResolver>();

            stateResolver.Setup(s => s.Find(It.IsAny<Guid>())).Returns(stateController.Object);

            var captureReader = new CaptureReader(baseStream, stateResolver.Object);
                        
            Assert.Equal(satCount, captureReader.StateAllocationTable.Count);
        }

        [Fact]
        public void CaptureReader_Throws_An_Exception_When_Loading_An_Invalid_Continuum_Stream()
        {
            //empty stream is considered invalid
            var baseStream = new ConcurrentStream(new MemoryStream());

            var stateController = new Mock<IStateController>();
            var stateResolver = new Mock<IStateResolver>();

            stateResolver.Setup(s => s.Find(It.IsAny<Guid>())).Returns(stateController.Object);

            try
            {
                var captureReader = new CaptureReader(baseStream, stateResolver.Object);
                
                //fail
                Assert.True(false);
            }
            catch
            {
                //pass
                Assert.True(true);
            }
        }

        [Fact]
        public void State_Allocation_Table_Is_Correctly_Populated()
        {
            var expectedId = (short)123;
            var expectedGuid = Guid.NewGuid();

            var ms = new ConcurrentStream(new MemoryStream());
            var idBytes = BitConverter.GetBytes(expectedId);
            var guidBytes = expectedGuid.ToByteArray();

            //write the header
            var header = _CreateContinuumHeader(1000, 10000);
            ms.Write(header.ToArray(), 0, header.Length);

            //write the SAT
            ms.Write(BitConverter.GetBytes(1), 0, 4);
            ms.Write(idBytes, 0, idBytes.Length);
            ms.Write(guidBytes, 0, guidBytes.Length);

            //rewind the stream
            ms.Position = 0;

            var stateController = new Mock<IStateController>();
            var stateResolver = new Mock<IStateResolver>();

            stateResolver.Setup(s => s.Find(It.IsAny<Guid>())).Returns(stateController.Object);

            var captureReader = new CaptureReader(ms, stateResolver.Object);

            Assert.NotEmpty(captureReader.StateAllocationTable);
            Assert.Equal(expectedId, captureReader.StateAllocationTable.First().Key);
            Assert.Equal(expectedGuid, captureReader.StateAllocationTable.First().Value);
            Assert.Equal(1, captureReader.StateAllocationTable.Count);
        }

        [Fact]
        public void CaptureReader_Selects_The_Correct_IStateController_During_Reads()
        {
            var immlStateController = new Mock<IStateController>();
            var immlStateControllerId = (short)100;
            var immlStateControllerGuid = Guid.NewGuid();
            immlStateController.Setup(g => g.Guid).Returns(immlStateControllerGuid);

            var boneNodeStateController = new Mock<IStateController>();
            var boneNodeStateControllerId = (short)123;
            var boneNodeStateControllerGuid = Guid.NewGuid();
            boneNodeStateController.Setup(g => g.Guid).Returns(boneNodeStateControllerGuid);

            //build 2 different state mappings
            var ms = new ConcurrentStream(new MemoryStream());

            //header
            var header = _CreateContinuumHeader(1000, 10000);
            ms.Write(header, 0, header.Length);
            
            //sat
            ms.Write(BitConverter.GetBytes(2), 0, 4); //count
            
            //mapping for imml
            ms.Write(BitConverter.GetBytes(immlStateControllerId), 0, 2);
            ms.Write(immlStateControllerGuid.ToByteArray(), 0, 16);
            
            //mapping for bones
            ms.Write(BitConverter.GetBytes(boneNodeStateControllerId), 0, 2);
            ms.Write(boneNodeStateControllerGuid.ToByteArray(), 0, 16);

            //generate 2 dummy states, one for each controller
            var firstRandomBytes = new byte[1024];
            new Random().NextBytes(firstRandomBytes);
            
            ms.Write(BitConverter.GetBytes(immlStateControllerId), 0, 2);
            ms.Write(BitConverter.GetBytes(DateTime.UtcNow.ToBinary()), 0, 8);
            ms.Write(BitConverter.GetBytes((int)firstRandomBytes.Length), 0, 4);
            ms.Write(firstRandomBytes, 0, firstRandomBytes.Length);
            
            var secondRandomBytes = new byte[2048];
            new Random().NextBytes(secondRandomBytes);

            ms.Write(BitConverter.GetBytes(boneNodeStateControllerId), 0, 2);
            ms.Write(BitConverter.GetBytes(DateTime.UtcNow.ToBinary()), 0, 8);
            ms.Write(BitConverter.GetBytes((int)secondRandomBytes.Length), 0, 4);
            ms.Write(secondRandomBytes, 0, secondRandomBytes.Length);

            ms.Position = 0;

            //perform the reads and verify
            var stateResolver = new StateResolver();
            stateResolver.Add(immlStateController.Object);
            stateResolver.Add(boneNodeStateController.Object);

            var captureReader = new CaptureReader(ms, stateResolver);
            captureReader.Read();
            captureReader.Read();

            boneNodeStateController.Verify(c => c.Create(It.IsAny<byte[]>(), It.IsAny<DateTime>(), It.IsAny<double>()), Times.Once());
            immlStateController.Verify(c => c.Create(It.IsAny<byte[]>(), It.IsAny<DateTime>(), It.IsAny<double>()), Times.Once());
        }

        [Fact]
        public void CaptureReader_Can_Read_Back_A_Stream_That_Was_Written_By_A_CaptureWriter()
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

            //write operations
            var captureWriter = new CaptureWriter(ms, stateResolver.Object);

            for (int i = 0; i < writeOperations; i++)
            {
                var timestamp = DateTime.UtcNow;
                var randomData = new byte[1024];
                new Random().NextBytes(randomData);

                var randomSat = (short)new Random().Next(24, 26);

                var captureState = new Mock<ICaptureState>();
                captureState.Setup(c => c.Data).Returns(randomData);
                captureState.Setup(c => c.Guid).Returns(sat.Where(f => f.Value == randomSat).First().Key);
                captureState.Setup(c => c.Timestamp).Returns(timestamp);

                captureWriter.Write(captureState.Object);                
            }

            //rewind the stream to the beginning
            ms.Position = 0;

            //read operations
            var readStateResolver = new StateResolver();
            readStateResolver.Add(new SimpleStateController { Guid = sat.Where(f => f.Value == 24).First().Key });
            readStateResolver.Add(new SimpleStateController { Guid = sat.Where(f => f.Value == 25).First().Key });
            readStateResolver.Add(new SimpleStateController { Guid = sat.Where(f => f.Value == 26).First().Key });

            var captureReader = new CaptureReader(ms, readStateResolver);

            while (captureReader.Position < captureReader.Count)
            {
                captureReader.Read();
            }

        }

        [Fact]
        public void CaptureReader_Correctly_Assigns_Offsets_To_ICaptureState_Instances_When_Read()
        {
            var stateController = new WeatherStateController(new WeatherSimulator());

            var stateResolver = new StateResolver();
            stateResolver.Add(stateController);

            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());
            ms.Position = 0;
            
            var stream = new ConcurrentStream(ms);
            var captureStream = new CaptureStream(stream, FileAccess.Read, stateResolver);

            var lastOffset = 0d;

            for (int i = 0; i < captureStream.Count; i++)
            {
                var state = captureStream.Read();
                
                Assert.True(state.Offset >= 0);
                Assert.True(state.Offset >= lastOffset);
                
                lastOffset = state.Offset;
            }
        }

        [Fact]
        public void CaptureReader_Length_Is_Correct_After_Construction()
        {
            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());
            ms.Position = 0;

            var stream = new ConcurrentStream(ms);
            var captureStream = new CaptureStream(stream, FileAccess.Read, new StateResolver());

            Assert.NotEqual(TimeSpan.Zero, captureStream.Length);
        }

        [Fact]
        public void Peek_Does_Not_Advance_The_Read_Pointer()
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

            //write operations
            var captureWriter = new CaptureWriter(ms, stateResolver.Object);

            for (int i = 0; i < writeOperations; i++)
            {
                var timestamp = DateTime.UtcNow;
                var randomData = new byte[1024];
                new Random().NextBytes(randomData);

                var randomSat = (short)new Random().Next(24, 27);

                var captureState = new Mock<ICaptureState>();
                captureState.Setup(c => c.Data).Returns(randomData);
                captureState.Setup(c => c.Guid).Returns(sat.Where(f => f.Value == randomSat).First().Key);
                captureState.Setup(c => c.Timestamp).Returns(timestamp);

                captureWriter.Write(captureState.Object);
            }

            //rewind the stream to the beginning
            ms.Position = 0;

            //read operations
            var readStateResolver = new StateResolver();
            readStateResolver.Add(new SimpleStateController { Guid = sat.Where(f => f.Value == 24).First().Key });
            readStateResolver.Add(new SimpleStateController { Guid = sat.Where(f => f.Value == 25).First().Key });
            readStateResolver.Add(new SimpleStateController { Guid = sat.Where(f => f.Value == 26).First().Key });

            var captureReader = new CaptureReader(ms, readStateResolver);

            var position = captureReader.Position;
            var state = captureReader.Peek(sat.Where(f => f.Value == (short)26).First().Key);

            Assert.NotNull(state);
            Assert.Equal(position, captureReader.Position);
        }

    }
}
