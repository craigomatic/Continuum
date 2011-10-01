using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Continuum.IO;
using VastPark.FrameworkBase;
using VastPark.FrameworkBase.ComponentModel;

namespace Continuum.Test.Mock
{
    public class WeatherStateController : IStateController
    {
        public event Event<ICaptureState> StateExecuted;

        public Guid Guid
        {
            get { return new Guid("D4DF6DA3-70F5-45BE-A9E6-0BE23088AD5F"); }
        }

        public WeatherSimulator Simulator { get; private set; }

        public ICaptureStream CaptureStream { get; private set; }

        public WeatherStateController(WeatherSimulator simulator)
        {
            Simulator = simulator;
            Simulator.NewTemperature += new VastPark.FrameworkBase.ComponentModel.Event<int>(_Simulator_NewTemperature);
        }

        public void Initialise(ICaptureStream captureStream)
        {
            this.CaptureStream = captureStream;            
        }

        void _Simulator_NewTemperature(object sender, VastPark.FrameworkBase.ComponentModel.TypedEventArgs<int> e)
        {
            //write the new ICaptureState into the stream
            this.CaptureStream.Write(new WeatherCaptureState(BitConverter.GetBytes(e.Value), this.Guid, DateTime.UtcNow, DateTime.UtcNow.Subtract(this.CaptureStream.Timestamp).TotalMilliseconds));
        }

        public ICaptureState Create(byte[] data, DateTime timestamp, double offset)
        {
            //used by the CaptureReader to build a state from the data
            return new WeatherCaptureState(data, this.Guid, timestamp, offset);
        }

        public void Execute(ICaptureState captureState)
        {
            //write the temperature out to the console and raise an event
            StateExecuted.Raise(this, captureState);
            Console.WriteLine(string.Format("At {0} the temperature was: {1} degrees celcius", captureState.Timestamp.ToString(), (captureState as WeatherCaptureState).Temperature));
        }
    }

    public class WeatherCaptureState : ICaptureState
    {
        public byte[] Data { get; private set; }

        public int Temperature { get; private set; }

        public double Offset { get; set; }

        public DateTime Timestamp { get; set; }

        public Guid Guid { get; set; }

        public long Length { get; set; }

        public WeatherCaptureState(byte[] data, Guid guid, DateTime timestamp, double offset)
        {
            this.Data = data;
            this.Guid = guid;
            this.Timestamp = timestamp;
            this.Offset = offset;

            this.Temperature = BitConverter.ToInt32(data, 0);
        }
    }
}
