using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Continuum.IO;
using System.Reflection;
using System.IO;
using Continuum.Test.Mock;
using VastPark.FrameworkBase.Threading;
using Continuum.Filters;
using Continuum.Tasks;
using Moq;

namespace Continuum.Test
{
    /// <summary>
    /// Tests the functionality of the PlaybackService which includes:
    /// - The ability to load multiple playback streams to be played on a single timeline
    /// - 
    /// </summary>
    public class PlaybackTests
    {
        [Fact]
        public void PlaybackService_Supports_Multiple_ICaptureStream_Instances()
        {
            var resetEvent = new SlimResetEvent(20);
            var expectedStates = long.MaxValue;
            var executedStates = 0;
            var stateController = new WeatherStateController(new WeatherSimulator());

            var stateResolver = new StateResolver();
            stateResolver.Add(stateController);

            stateController.StateExecuted += delegate
            {
                executedStates++;

                if (executedStates == expectedStates)
                {                    
                    resetEvent.Set();
                }
            };

            var playbackService = new PlaybackService(stateResolver);

            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());
            
            var stream1 = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream1 = new CaptureStream(stream1, System.IO.FileAccess.Read, stateResolver);

            var stream2 = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream2 = new CaptureStream(stream2, System.IO.FileAccess.Read, stateResolver);

            expectedStates = captureStream1.Count + captureStream2.Count;

            playbackService.Add(captureStream1);
            playbackService.Add(captureStream2);            
            
            playbackService.Start();

            resetEvent.Wait();                    
        }

        [Fact]
        public void CaptureStream_Instances_Are_Played_Back_In_Chronological_Order()
        {
            var resetEvent = new SlimResetEvent(20);
            var expectedStates = long.MaxValue;
            var executedStates = 0;
            var stateController = new WeatherStateController(new WeatherSimulator());
            var stateResolver = new StateResolver();
            stateResolver.Add(stateController);

            stateController.StateExecuted += delegate
            {
                executedStates++;

                if (executedStates == expectedStates)
                {
                    resetEvent.Set();
                }
            };


            var playbackService = new PlaybackService(stateResolver);

            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());

            var stream1 = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream1 = new CaptureStream(stream1, System.IO.FileAccess.Read, stateResolver);

            var stream2 = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream2 = new CaptureStream(stream2, System.IO.FileAccess.Read, stateResolver);

            expectedStates = captureStream1.Count + captureStream2.Count;

            playbackService.Add(captureStream1);
            playbackService.Add(captureStream2);

            playbackService.Start();

            resetEvent.Wait();
        }

        [Fact]
        public void When_A_Codec_Is_Not_Available_An_Event_Is_Raised_Giving_The_Caller_An_Opportunity_To_Acquire_It()
        {
            var stateController = new WeatherStateController(new WeatherSimulator());

            var stateResolver = new StateResolver();

            var eventRaised = false;

            var playbackService = new PlaybackService(stateResolver);
            
            playbackService.CodecRequired += delegate
            {
                eventRaised = true;
            };

            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());

            var stream = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream = new CaptureStream(stream, System.IO.FileAccess.Read, stateResolver);

            playbackService.Add(captureStream);

            Assert.True(eventRaised);
        }

        [Fact]
        public void When_A_Codec_Is_Available_An_Event_Is_Not_Raised()
        {
            var stateController = new WeatherStateController(new WeatherSimulator());
            
            var stateResolver = new StateResolver();
            stateResolver.Add(stateController);

            var eventRaised = false;

            var playbackService = new PlaybackService(stateResolver);

            playbackService.CodecRequired += delegate
            {
                eventRaised = true;
            };

            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());

            var stream = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream = new CaptureStream(stream, System.IO.FileAccess.Read, stateResolver);

            playbackService.Add(captureStream);

            Assert.False(eventRaised);
        }

        [Fact]
        public void When_A_Codec_Is_Not_Available_An_Event_Is_Raised_With_The_Guid_Of_The_Required_Codec()
        {
            var stateController = new WeatherStateController(new WeatherSimulator());

            var stateResolver = new StateResolver();
            stateResolver.Add(stateController);

            var playbackService = new PlaybackService(stateResolver);

            playbackService.CodecRequired += delegate(object sender, CodecRequiredEventArgs e)
            {
                Assert.Equal(stateController.Guid, e.Guid);
            };

            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());

            var stream = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream = new CaptureStream(stream, System.IO.FileAccess.Read, stateResolver);

            playbackService.Add(captureStream);
        }
    
        [Fact]
        public void When_A_Filter_Is_Applied_That_Returns_True_The_State_Is_Not_Scheduled_For_Playback()
        {
            var stateController = new WeatherStateController(new WeatherSimulator());
            var stateResolver = new StateResolver();
            stateResolver.Add(stateController);

            var scheduler = new Mock<IScheduler>();
            var taskFactory = new Mock<ITaskFactory>();

            var playbackService = new PlaybackService(stateResolver, scheduler.Object, taskFactory.Object);

            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());

            var stream = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream = new CaptureStream(stream, System.IO.FileAccess.Read, stateResolver);

            playbackService.Add(captureStream);

            //add in a filter that will always return true to filter out the states
            playbackService.Add(new StreamFilter(f => true));

            playbackService.Start();

            //task factory should not have built any tasks
            taskFactory.Verify(t => t.Create(It.IsAny<ICaptureState>()), Times.Never());
            scheduler.Verify(s => s.Add(It.IsAny<ITask>()), Times.Never());
        }

        [Fact]
        public void When_A_Filter_Is_Applied_That_Returns_False_The_State_Is_Scheduled_For_Playback()
        {
            var stateController = new WeatherStateController(new WeatherSimulator());

            var stateResolver = new StateResolver();
            stateResolver.Add(stateController);

            var scheduler = new Mock<IScheduler>();
            var taskFactory = new Mock<ITaskFactory>();

            var playbackService = new PlaybackService(stateResolver, scheduler.Object, taskFactory.Object);

            var ms = VastPark.FrameworkBase.IO.EmbeddedResource.GetMemoryStream("Continuum.Test.Samples.weather-simulation.continuum", Assembly.GetExecutingAssembly());

            var stream = new ConcurrentStream(new MemoryStream(ms.ToArray()));
            var captureStream = new CaptureStream(stream, System.IO.FileAccess.Read, stateResolver);

            playbackService.Add(captureStream);

            //add in a filter that will always return true to filter out the states
            playbackService.Add(new StreamFilter(f => false));

            playbackService.Start();

            //task factory should not have built any tasks
            taskFactory.Verify(t => t.Create(It.IsAny<ICaptureState>()));
            scheduler.Verify(s => s.Add(It.IsAny<ITask>()));
        }
    
        [Fact]
        public void When_Streaming_A_Continuum_File_The_Completion_Event_Is_Not_Raised_While_The_File_Still_Has_More_Data()
        {
            var captureStream = new CaptureStream(new ConcurrentStream(new MemoryStream()), FileAccess.Write, new StateResolver());
            Assert.Equal(TimeSpan.Zero, captureStream.Length);
        }
    }
}
