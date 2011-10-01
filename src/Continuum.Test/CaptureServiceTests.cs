using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;
using System.IO;
using Moq;
using Xunit;
using Continuum.Test.Mock;
using VastPark.FrameworkBase.Threading;

namespace Continuum.Test
{
    public class CaptureServiceTests
    {
        [Fact]
        public void Multiple_IStateRecorders_Can_Be_Written_Into_The_Same_IStream()
        {
            var guid = Guid.NewGuid();
            var stateController = new Mock<IStateController>();
            stateController.Setup(s => s.Guid).Returns(guid);
            //stateController.Setup(s => s.Create(It.IsAny<byte[]>(), It.IsAny<DateTime>())).Returns(new Mock<ICaptureState>().Object);
            var stateResolver = new StateResolver();
            stateResolver.Add(stateController.Object);

            var captureService = new CaptureService 
            { 
                Stream = new ConcurrentStream(new MemoryStream()),
                StateResolver = stateResolver
            };

            var recorder1 = new Mock<IStateRecorder>();
            var recorder2 = new Mock<IStateRecorder>();

            var recorderBuffer1 = new SimpleBuffer<ICaptureState>();           
            var recorderBuffer2 = new SimpleBuffer<ICaptureState>();

            recorder1.Setup(b => b.Buffer).Returns(recorderBuffer1);
            recorder2.Setup(b => b.Buffer).Returns(recorderBuffer2);
            
            //push some dummy states into the buffers
            
            var state1 = new Mock<ICaptureState>();
            state1.Setup(s=>s.Timestamp).Returns(new DateTime(2011, 2, 3));
            state1.Setup(s => s.Guid).Returns(guid);

            var state2 = new Mock<ICaptureState>();
            state2.Setup(s => s.Timestamp).Returns(new DateTime(2011, 2, 4));
            state2.Setup(s => s.Guid).Returns(guid);

            recorderBuffer1.Enqueue(state1.Object);
            recorderBuffer2.Enqueue(state2.Object);

            captureService.Add(recorder1.Object);
            captureService.Add(recorder2.Object);

            captureService.Start();

            captureService.Flush();

            Assert.Equal(2, captureService.CaptureStream.Count);
        }

        [Fact]
        public void Multiple_IStateRecorders_Are_Written_In_Chronological_Order_Into_A_Shared_Stream()
        {
            var guid = Guid.NewGuid();
            var stream = new ConcurrentStream(new MemoryStream());

            var stateController = new SimpleStateController { Guid = guid };

            var stateResolver = new StateResolver();
            stateResolver.Add(stateController);

            var captureService = new CaptureService
            {
                Stream = stream,
                StateResolver = stateResolver
            };

            var recorder1 = new Mock<IStateRecorder>();
            var recorder2 = new Mock<IStateRecorder>();

            var recorderBuffer1 = new SimpleBuffer<ICaptureState>();
            var recorderBuffer2 = new SimpleBuffer<ICaptureState>();

            recorder1.Setup(b => b.Buffer).Returns(recorderBuffer1);
            recorder2.Setup(b => b.Buffer).Returns(recorderBuffer2);

            //push some dummy states into the buffers

            var state1 = new Mock<ICaptureState>();
            var firstTimestamp = DateTime.Now.AddDays(1);
            state1.Setup(s => s.Timestamp).Returns(firstTimestamp);
            state1.Setup(s => s.Guid).Returns(guid);

            var state2 = new Mock<ICaptureState>();
            var secondTimestamp = DateTime.Now.AddDays(2);
            state2.Setup(s => s.Timestamp).Returns(secondTimestamp);
            state2.Setup(s => s.Guid).Returns(guid);

            recorderBuffer1.Enqueue(state1.Object);
            recorderBuffer2.Enqueue(state2.Object);

            //add the 2nd recorder in first as it's timestamp is at a time in the future beyond the first recorder's state
            captureService.Add(recorder2.Object);

            captureService.Add(recorder1.Object);

            captureService.Start();
            captureService.Flush();

            Assert.Equal(2, captureService.CaptureStream.Count);

            //open the stream for reading now
            stream.Position = 0;

            var captureStream = new CaptureStream(stream, FileAccess.Read, stateResolver);

            //first value read back should be from state1
            Assert.Equal(firstTimestamp, captureStream.Read().Timestamp);
            Assert.Equal(secondTimestamp, captureStream.Read().Timestamp);
        }

        [Fact]
        public void CaptureService_Can_Write_A_Capture_To_Disk_When_Backed_By_A_FileStream()
        {
            var statesToWrite = 100;
            var simulator = new WeatherSimulator();
            var controller = new WeatherStateController(simulator);

            var stateResolver = new StateResolver();
            stateResolver.Add(controller);

            var tmpFile = System.IO.Path.GetTempFileName();

            try
            {
                using (var ms = new FileStream(tmpFile,FileMode.Create))
                {
                    var baseStream = new ConcurrentStream(ms);
                    var captureService = new CaptureService
                    {
                        Stream = baseStream,
                        StateResolver = stateResolver
                    };

                    captureService.Start();

                    controller.Initialise(captureService.CaptureStream);

                    simulator.Start();                    

                    var count = 0;
                    var resetEvent = new SlimResetEvent(20);

                    simulator.NewTemperature += delegate
                    {
                        count++;

                        if (count == statesToWrite)
                        {
                            simulator.Stop();
                            resetEvent.Set();
                        }
                    };

                    resetEvent.Wait();

                    captureService.Flush();
                }

                Assert.True(File.Exists(tmpFile));
                Assert.True(new FileInfo(tmpFile).Length > 0);

            }
            finally
            {
                File.Delete(tmpFile);
            }
        }
    }
}
