﻿using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Sensors.Switches;
using Meadow.Hardware;
using System;
using System.Threading.Tasks;

namespace Sensors.Switches.SpdtSwitch_Sample
{
    public class MeadowApp : App<F7FeatherV2>
    {
        //<!=SNIP=>

        protected SpdtSwitch spdtSwitch;

        public override Task Initialize()
        {
            Console.WriteLine("Initializing...");

            spdtSwitch = new SpdtSwitch(Device.CreateDigitalInputPort(Device.Pins.D15, InterruptMode.EdgeBoth, ResistorMode.InternalPullDown));
            spdtSwitch.Changed += (s, e) =>
            {
                Console.WriteLine(spdtSwitch.IsOn ? "Switch is on" : "Switch is off");
            };

            Console.WriteLine("SpdtSwitch ready...");

            return Task.CompletedTask;
        }

        //<!=SNOP=>
    }
}