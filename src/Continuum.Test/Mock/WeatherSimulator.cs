using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using VastPark.FrameworkBase.ComponentModel;

namespace Continuum.Test.Mock
{
    public class WeatherSimulator
    {
        public event Event<int> NewTemperature;

        public List<int> History { get; set; }

        private object _Lock;

        public WeatherSimulator()
        {
            this.History = new List<int>();

            _Lock = new object();
        }

        public void Start()
        {
            var thread = new Thread(new ThreadStart(_UpdateLoop));
            thread.Start();
        }        

        public void Stop()
        {
            lock (_Lock)
            {
                _Continue = false;
            }
        }

        private bool _Continue;

        private void _UpdateLoop()
        {
            _Continue = true;

            while (_Continue)
            {
                lock (_Lock)
                {
                    this.NewTemperature.Raise(this, _GetNextTemperature());
                }
            }
        }

        private int _GetNextTemperature()
        {
            var random = new Random().Next(0, 50);
            Thread.Sleep(random);

            var temp = new Random().Next(0, 50);
            this.History.Add(temp);
            return temp;
        }
    }
}
