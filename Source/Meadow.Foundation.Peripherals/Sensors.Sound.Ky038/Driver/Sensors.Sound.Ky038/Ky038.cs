﻿using Meadow.Hardware;
using System;
using System.Threading;

namespace Meadow.Foundation.Sensors.Sound
{
   public class Ky038
    {
        protected IAnalogInputPort analogPort;
        protected IDigitalInputPort digitalInputPort;

        private Ky038 () { }

        public Ky038(IIODevice device, IPin A0, IPin D0) : 
            this (device.CreateAnalogInputPort(A0), device.CreateDigitalInputPort(D0))
        {
            
        }

        public Ky038(IAnalogInputPort analogPort, IDigitalInputPort digitalInputPort)
        {
            this.analogPort = analogPort;
            this.digitalInputPort = digitalInputPort;

            digitalInputPort.Changed += DigitalInputPort_Changed;

            analogPort.StartSampling();

            while (true)
            {
                Console.WriteLine($"Analog: {analogPort.Voltage}");
                Thread.Sleep(250);
            }
        }

        private void DigitalInputPort_Changed(object sender, DigitalInputPortEventArgs e)
        {
           
        }
    }
}