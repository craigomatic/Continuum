using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;
using Moq;

namespace Continuum.Test.Mock
{
    public class SimpleStateController : IStateController
    {
        public Guid Guid { get; set; }

        public ICaptureState Create(byte[] data, DateTime timestamp, double offset)
        {
            var mock = new Mock<ICaptureState>();
            mock.Setup(c => c.Data).Returns(data);
            mock.Setup(c => c.Guid).Returns(this.Guid);
            mock.Setup(c => c.Timestamp).Returns(timestamp);
            mock.Setup(c => c.Offset).Returns(offset);

            return mock.Object;
        }


        public void Execute(ICaptureState captureState)
        {
            Console.WriteLine("Executed capture state");
        }
    }
}
