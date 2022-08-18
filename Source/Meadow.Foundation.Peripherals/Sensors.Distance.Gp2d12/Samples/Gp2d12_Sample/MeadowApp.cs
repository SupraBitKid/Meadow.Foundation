﻿using System;
using System.Threading.Tasks;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Sensors.Distance;

namespace MeadowApp
{
    public class MeadowApp : App<F7FeatherV2>
    {
        //<!=SNIP=>

        Gp2d12 sensor;

        public override Task Initialize()
        {
            Console.WriteLine("Initializing...");

            sensor = new Gp2d12(Device, Device.Pins.A03);

            var consumer = Gp2d12.CreateObserver(
                handler: result => 
                {
                    Console.WriteLine($"Observer filter satisfied: {result.New.Centimeters:N2}cm, old: {result.Old?.Centimeters:N2}cm");
                },
                // only notify if the change is greater than 5cm
                filter: result => 
                {
                    if (result.Old is { } old)
                    {   //c# 8 pattern match syntax. checks for !null and assigns var.
                        return (result.New - old).Abs().Centimeters > 5;
                    }
                    return false;
                }
            );
            sensor.Subscribe(consumer);

            sensor.DistanceUpdated += (sender, result) => 
            {
                Console.WriteLine($"Temp Changed, temp: {result.New.Centimeters:N2}cm, old: {result.Old?.Centimeters:N2}cm");
            };

            return Task.CompletedTask;
        }

        public override async Task Run()
        {
            var temperature = await sensor.Read();
            Console.WriteLine($"Initial temp: {temperature.Centimeters:N2}cm");

            sensor.StartUpdating(TimeSpan.FromMilliseconds(1000));
        }

        //<!=SNOP=>
    }
}