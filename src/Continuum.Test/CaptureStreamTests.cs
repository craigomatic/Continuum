using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Continuum.Test.Mock;
using VastPark.FrameworkBase.Threading;
using System.Threading;
using Moq;
using Xunit;
using Continuum.IO;
using System.Reflection;

namespace Continuum.Test
{
    public class CaptureStreamTests
    {
        private StateResolver _StateResolver;
        private List<IStateController> _GeneratedControllers;

        private List<Guid> _GenerateCaptureService(int count)
        {
            _GeneratedControllers = new List<IStateController>();
            _StateResolver = new StateResolver();

            //register the GUIDs with the capture service
            var validGuids = new List<Guid>();

            for (int i = 0; i < count; i++)
            {
                var stateController = new Mock<IStateController>();
                var guid = Guid.NewGuid();

                validGuids.Add(guid);

                stateController.Setup(c => c.Guid).Returns(guid);

                _StateResolver.Add(stateController.Object);
                
                _GeneratedControllers.Add(stateController.Object);
            }

            return validGuids;
        }

        [Fact]
        public void Count_Is_Incremented_Correctly_During_Writes()
        {
            var count = 100;
            var ms = new ConcurrentStream(new MemoryStream());

            var validGuids = _GenerateCaptureService(count);

            var captureStream = new CaptureStream(ms, FileAccess.Write, _StateResolver);
            
            for (int i = 0; i < count; i++)
            {
                var mock = new Mock<ICaptureState>();
                mock.Setup(p => p.Guid).Returns(validGuids[i]);
                mock.Setup(p => p.Data).Returns(Guid.NewGuid().ToByteArray());

                captureStream.Write(mock.Object);
            }
            
            Assert.Equal(count, captureStream.Count);
        }

        [Fact]
        public void Position_Is_Updated_Correctly_During_Writes()
        {
            var count = 100;
            var ms = new ConcurrentStream(new MemoryStream());

            var validGuids = _GenerateCaptureService(count);

            var captureStream = new CaptureStream(ms, FileAccess.Write, _StateResolver);

            for (int i = 0; i < count; i++)
            {
                var mock = new Mock<ICaptureState>();
                mock.Setup(p => p.Guid).Returns(validGuids[i]);
                mock.Setup(p => p.Data).Returns(Guid.NewGuid().ToByteArray());

                captureStream.Write(mock.Object);
                Assert.Equal(i + 1, captureStream.Position);
            }
        }

        [Fact]
        public void Length_Of_An_Existing_Stream_Is_Correctly_Reported_After_Construction()
        {
            var count = 100;
            var ms = new ConcurrentStream(new MemoryStream());

            var validGuids = _GenerateCaptureService(count);

            var captureStream = new CaptureStream(ms, FileAccess.Write, _StateResolver);

            for (int i = 0; i < count; i++)
            {
                var mock = new Mock<ICaptureState>();
                mock.Setup(p => p.Guid).Returns(validGuids[i]);
                mock.Setup(p => p.Data).Returns(Guid.NewGuid().ToByteArray());

                captureStream.Write(mock.Object);
                Assert.Equal(i + 1, captureStream.Position);
            }
        }

        [Fact]
        public void Selecting_A_Position_When_The_Current_Position_Is_Zero_Will_Read_That_Node()
        {
            var controller = new WeatherStateController(new WeatherSimulator());
            _StateResolver = new StateResolver();
            _StateResolver.Add(controller);
            
            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());

            var stream = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream = new CaptureStream(stream, System.IO.FileAccess.Read, _StateResolver);

            var desiredNode = Math.Round(Convert.ToDouble(captureStream.Count) / 2);

            captureStream.Position = (long)desiredNode - 1; //if the desired node is 50, its required to put the stream at position 49 in order to read it

            var seekNode = captureStream.Read();

            //manually read nodes, when the desired node is reached it should match the node pulled out previously
            stream = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            captureStream = new CaptureStream(stream, FileAccess.Read, _StateResolver);

            ICaptureState actualNode = null;

            for (int i = 0; i < desiredNode; i++)
            {
                actualNode = captureStream.Read();
            }

            Assert.Equal(actualNode.Guid, seekNode.Guid);
            Assert.Equal(actualNode.Offset, seekNode.Offset);
            Assert.Equal(actualNode.Timestamp, seekNode.Timestamp);

            for (int i = 0; i < actualNode.Data.Length; i++)
            {
                Assert.Equal(actualNode.Data[i], seekNode.Data[i]);
            }
        }

        [Fact]
        public void Selecting_A_Position_Forward_In_The_Stream_When_The_Current_Position_Is_Not_Zero_Will_Read_That_Node()
        {
            _StateResolver = new StateResolver();
            var controller = new WeatherStateController(new WeatherSimulator());

            _StateResolver.Add(controller);

            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());

            var stream = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream = new CaptureStream(stream, System.IO.FileAccess.Read, _StateResolver);

            var desiredNode = Math.Round(Convert.ToDouble(captureStream.Count) / 2);

            //read up the number of nodes to move the position pointer forwards in the stream
            for (int i = 0; i < desiredNode; i++)
            {
                captureStream.Read();
            }
            
            //seek to the new position
            captureStream.Position += 2;

            //this node is equivalent to node desiredNode + 3
            var seekNode = captureStream.Read();

            //manually read nodes, when the desired node is reached it should match the node pulled out previously
            stream = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            captureStream = new CaptureStream(stream, FileAccess.Read, _StateResolver);

            ICaptureState actualNode = null;

            for (int i = 0; i < desiredNode + 3; i++) 
            {
                actualNode = captureStream.Read();
            }            

            Assert.Equal(actualNode.Guid, seekNode.Guid);
            Assert.Equal(actualNode.Offset, seekNode.Offset);
            Assert.Equal(actualNode.Timestamp, seekNode.Timestamp);

            for (int i = 0; i < actualNode.Data.Length; i++)
            {
                Assert.Equal(actualNode.Data[i], seekNode.Data[i]);
            }
        }

        [Fact]
        public void Selecting_A_Position_Backward_In_The_Stream_When_The_Current_Position_Is_Not_Zero_Will_Read_That_Node()
        {
            _StateResolver = new StateResolver();
            var controller = new WeatherStateController(new WeatherSimulator());

            _StateResolver.Add(controller);

            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());

            var stream = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream = new CaptureStream(stream, System.IO.FileAccess.Read, _StateResolver);

            var desiredNode = Math.Round(Convert.ToDouble(captureStream.Count) / 2);

            //read up the number of nodes to move the position pointer forwards in the stream
            for (int i = 0; i < desiredNode; i++)
            {
                captureStream.Read();
            }

            //seek to the new position
            captureStream.Position -= 2;

            //this node is equivalent to node desiredNode - 2
            var seekNode = captureStream.Read();

            //manually read nodes, when the desired node is reached it should match the node pulled out previously
            stream = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            captureStream = new CaptureStream(stream, FileAccess.Read, _StateResolver);

            ICaptureState actualNode = null;

            for (int i = 0; i < desiredNode - 1; i++)
            {
                actualNode = captureStream.Read();
            }            

            Assert.Equal(actualNode.Guid, seekNode.Guid);
            Assert.Equal(actualNode.Offset, seekNode.Offset);
            Assert.Equal(actualNode.Timestamp, seekNode.Timestamp);

            for (int i = 0; i < actualNode.Data.Length; i++)
            {
                Assert.Equal(actualNode.Data[i], seekNode.Data[i]);
            }
        }

        [Fact]
        public void Selecting_A_Position_Beyond_The_Length_Of_The_Stream_Will_Throw_An_Exception()
        {
            _StateResolver = new StateResolver();

            var controller = new WeatherStateController(new WeatherSimulator());

            _StateResolver.Add(controller);

            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());

            var stream = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream = new CaptureStream(stream, System.IO.FileAccess.Read, _StateResolver);

            Assert.Throws(typeof(Exception), new Assert.ThrowsDelegate(delegate { captureStream.Position = captureStream.Count + 1; }));
        }

        [Fact]
        public void Custom_Capture_Formats_Are_Recorded_Correctly()
        {
            var resetEvent = new SlimResetEvent(20);

            var desiredStates = 200;
            var generatedStates = 0;

            var ms = new MemoryStream();
            var stream = new ConcurrentStream(ms);

            var weatherSimulator = new WeatherSimulator();

            _StateResolver = new StateResolver();
            //build the controller that manages weather states
            var controller = new WeatherStateController(weatherSimulator);

            //watch the simluator for the desired number of states
            weatherSimulator.NewTemperature += delegate
            {
                generatedStates++;

                if (generatedStates >= desiredStates)
                {
                    weatherSimulator.Stop();
                    resetEvent.Set();
                }
            };

            //register it with the capture service
            _StateResolver = new StateResolver();
            _StateResolver.Add(controller);

            //create the capture stream, will create an entry in the SAT for the WeatherStateController
            var captureStream = new CaptureStream(stream, FileAccess.Write, _StateResolver);

            //pass the controller the stream so that it can be written to
            controller.Initialise(captureStream);

            //start the simulator, the controller will watch for changes internally            
            weatherSimulator.Start();

            //wait until the event is raised [desiredStates] number of times
            resetEvent.Wait();

            Assert.Equal(generatedStates, captureStream.Count);

            //rewind the stream and open it for reading, verify the values match
            stream.Position = 0;

            captureStream = new CaptureStream(stream, FileAccess.Read, _StateResolver);

            for (int i = 0; i < captureStream.Count; i++)
            {
                var state = captureStream.Read();

                Assert.Equal(weatherSimulator.History[i], (state as WeatherCaptureState).Temperature);
            }
        }        
    
        [Fact]
        public void CaptureStream_Supports_Writing_A_State_And_Then_Reading_It()
        {
            var stateController = new WeatherStateController(new WeatherSimulator());
            var stateResolver = new StateResolver();
            stateResolver.Add(stateController);

            var baseStream = new ConcurrentStream(new MemoryStream());
            var captureStream = new CaptureStream(baseStream, FileAccess.ReadWrite, stateResolver);

            Assert.True(captureStream.CanRead);
            Assert.True(captureStream.CanWrite);

            //write a state then read it back
            var expectedBytes = Guid.NewGuid().ToByteArray();

            captureStream.Write(new WeatherCaptureState(expectedBytes, stateController.Guid, DateTime.UtcNow, 0));

            //the read pointer is still at the start of the stream
            var readState = captureStream.Read();

            for (int i = 0; i < readState.Data.Length; i++)
            {
                Assert.Equal(expectedBytes[i], readState.Data[i]);
            }
        }

        [Fact]
        public void CaptureStream_Supports_Reading_A_File_For_Playback_Prior_To_The_Entire_File_Becoming_Available()
        {
            //base bytes to feed into the stream at random
            var bytes = VastPark.FrameworkBase.IO.EmbeddedResource.GetBytes("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());
            var endOfHeader = 0L;

            using (var ms = new MemoryStream(bytes))
            {
                var reader = new CaptureReader(new ConcurrentStream(ms), new StateResolver());

                endOfHeader = reader.BaseStream.Position;
            }

            var stream = new MemoryStream();
            var baseStream = new ConcurrentStream(stream);
            var binaryWriter = new BinaryWriter(stream);
            var stateResolver = new StateResolver();
            stateResolver.Add(new Continuum.Test.Mock.WeatherStateController(new WeatherSimulator()));

            //push in some bytes to get it started, enough for the header so that the stream won't throw on construction
            binaryWriter.Write(bytes, 0, (int)endOfHeader);

            var offset = baseStream.Position;
            baseStream.Position = 0;

            var captureStream = new CaptureStream(baseStream, FileAccess.Read, stateResolver);

            Assert.Null(captureStream.Peek());

            //write some bytes in (not enough for a full state)
            binaryWriter.Seek((int)offset, SeekOrigin.Begin);
            binaryWriter.Write(bytes, (int)offset, 10);

            offset += 10;

            Assert.Null(captureStream.Peek());

            //write some more bytes so that there is now a full state that can be peeked at
            binaryWriter.Seek((int)offset, SeekOrigin.Begin);
            binaryWriter.Write(bytes, (int)offset, 200);

            Assert.NotNull(captureStream.Peek());
        }
        
        public void CaptureStream_Supports_Reading_A_File_While_It_Is_Being_Written_From_Another_Thread()
        {
            //base bytes to feed into the stream at random
            var bytes = VastPark.FrameworkBase.IO.EmbeddedResource.GetBytes("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());
            var endOfHeader = 0L;

            using(var ms = new MemoryStream(bytes))
            {
                var reader = new CaptureReader(new ConcurrentStream(ms), new StateResolver());
                
                endOfHeader = reader.BaseStream.Position;
            }

            var stream = new MemoryStream();
            var baseStream = new ConcurrentStream(stream);
            var binaryWriter = new BinaryWriter(stream);
            var stateResolver = new StateResolver();
            stateResolver.Add(new Continuum.Test.Mock.WeatherStateController(new WeatherSimulator()));

            //push in some bytes to get it started, enough for the header so that the stream won't throw on construction
            binaryWriter.Write(bytes, 0, (int)endOfHeader);            

            var offset = baseStream.Position;
            baseStream.Position = 0;

            var captureStream = new CaptureStream(baseStream, FileAccess.Read, stateResolver);

            //write in a little more so we don't have a full state yet
            binaryWriter.Seek((int)offset, SeekOrigin.Begin);
            //binaryWriter.Write(bytes, (int)offset, 5);

            //offset += 5;

            var readStates = 0;
            var lockObject = new object();

            //create a thread to randomly write chunks of bytes into the base stream
            ThreadPool.QueueUserWorkItem(w =>
            {
                while (offset < bytes.LongLength)
                {
                    var bytesToWrite = new Random().Next(1, 200);

                    if (bytesToWrite > bytes.Length - offset)
                    {
                        bytesToWrite = Convert.ToInt32(bytes.LongLength - offset);
                    }

                    lock (lockObject)
                    {
                        binaryWriter.Seek((int)offset, SeekOrigin.Begin);
                        binaryWriter.Write(bytes, (int)offset, bytesToWrite);
                        offset += bytesToWrite;
                    }

                    Thread.Sleep(200);
                }

                Assert.Equal(bytes.LongLength, baseStream.Length);

                lock (lockObject)
                {
                    binaryWriter.Seek(0, SeekOrigin.Begin);

                    for (int i = 0; i < bytes.LongLength; i++)
                    {
                        Assert.Equal(bytes[i], (byte)baseStream.ReadByte());
                    }
                }
            });

            while (captureStream.Position < captureStream.Count)
            {
                lock (lockObject)
                {
                    if (captureStream.Peek() != null)
                    {
                        captureStream.Read();
                        readStates++;
                    }
                }
            }

            Assert.Equal(captureStream.Count, readStates);
        }
    }
}
