using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Continuum.Tasks;
using Continuum.IO;
using Moq;
using VastPark.FrameworkBase.Threading;
using VastPark.FrameworkBase;

namespace Continuum.Test
{
    public class SchedulerTests
    {
        [Fact]
        public void Scheduler_Executes_Tasks_Chronologically()
        {
            var tasks = 3;
            var executedTasks = 0;
            var guid = Guid.NewGuid();

            var resetEvent = new SlimResetEvent(20);
            var lastTimestamp = DateTime.MinValue;

            var stateController = new Mock<IStateController>();
            stateController.Setup(m => m.Guid).Returns(guid);
            stateController.Setup(m => m.Execute(It.IsAny<ICaptureState>())).Callback <ICaptureState>(delegate(ICaptureState state)
            {
                //validate the order of execution
                Assert.True(state.Timestamp > lastTimestamp);

                lastTimestamp = state.Timestamp;

                executedTasks++;

                if (executedTasks == tasks)
                {
                    resetEvent.Set();
                }
            });

            var stateResolver = new Mock<IStateResolver>();
            stateResolver.Setup(m => m.Find(It.IsAny<Guid>())).Returns(stateController.Object);
            
            var scheduler = new Scheduler();
            var taskFactory = new TaskFactory(stateResolver.Object);

            for (int i = 0; i < tasks; i++)
            {
                var mock = new Mock<ICaptureState>();
                mock.Setup(m => m.Guid).Returns(guid);
                mock.Setup(m => m.Timestamp).Returns(DateTime.UtcNow.AddMilliseconds(i + 1));
                
                var task = taskFactory.Create(mock.Object);
                scheduler.Add(task);
            }

            scheduler.Start();

            resetEvent.Wait();

            Assert.Equal(tasks, executedTasks);

            scheduler.Stop();
        }

        [Fact]
        public void Scheduler_Executes_Tasks_Within_Fifty_Milliseconds_Of_The_Desired_Time()
        {
            var marginForError = 50; //ms that the execution can be out by
            var tasks = 3;
            var executedTasks = 0;
            var guid = Guid.NewGuid();
            var start = DateTime.MinValue;
            var scheduler = new Scheduler();

            var resetEvent = new SlimResetEvent(20);
            var expectedExecutionOffset = new List<double>();

            var stateController = new Mock<IStateController>();
            stateController.Setup(m => m.Guid).Returns(guid);
            stateController.Setup(m => m.Execute(It.IsAny<ICaptureState>())).Callback<ICaptureState>(delegate(ICaptureState state)
            {
                //validate the time offset as being roughly simlar to the expected offset
                var actualOffset = HighResClock.Now.Subtract(scheduler.Started).TotalMilliseconds;
                var expectedOffset = expectedExecutionOffset[executedTasks];
                
                Assert.True(actualOffset <= expectedOffset + marginForError); //ok if its late by the marginForError

                executedTasks++;

                if (executedTasks == tasks)
                {
                    resetEvent.Set();
                }
            });

            var stateResolver = new Mock<IStateResolver>();
            stateResolver.Setup(m => m.Find(It.IsAny<Guid>())).Returns(stateController.Object);

            var taskFactory = new TaskFactory(stateResolver.Object);

            for (int i = 0; i < tasks; i++)
            {
                var mock = new Mock<ICaptureState>();
                mock.Setup(m => m.Guid).Returns(guid);
                mock.Setup(m => m.Timestamp).Returns(DateTime.UtcNow.AddMilliseconds(i * 30));
                mock.Setup(m => m.Offset).Returns(i * 30);

                var task = taskFactory.Create(mock.Object);
                scheduler.Add(task);

                expectedExecutionOffset.Add(task.DesiredExecution.TotalMilliseconds);
            }
            
            scheduler.Start();

            resetEvent.Wait();

            Assert.Equal(tasks, executedTasks);

            scheduler.Stop();
        }
    }
}
