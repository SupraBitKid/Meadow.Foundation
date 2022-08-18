﻿using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.Sensors.HallEffect;
using System;
using System.Threading.Tasks;

namespace Sensors.HallEffect_Sample
{
    public class MeadowApp : App<F7FeatherV2>
    {
        //<!=SNIP=>

        LinearHallEffectTachometer hallSensor;

        public override Task Initialize()
        {
            Console.Write("Initializing...");

            hallSensor = new LinearHallEffectTachometer(
                inputPort: Device.CreateDigitalInputPort(Device.Pins.D02, Meadow.Hardware.InterruptMode.EdgeRising, Meadow.Hardware.ResistorMode.InternalPullUp, TimeSpan.Zero, TimeSpan.FromMilliseconds(1)),
                type: CircuitTerminationType.CommonGround,
                numberOfMagnets: 2,
                rpmChangeNotificationThreshold: 1);
            hallSensor.RPMsChanged += HallSensorRPMsChanged;

            Console.WriteLine("done");

            return Task.CompletedTask;
        }

        void HallSensorRPMsChanged(object sender, ChangeResult<float> e)
        {
            Console.WriteLine($"RPM: {e.New}");
        }

        //<!=SNOP=>
    }
}