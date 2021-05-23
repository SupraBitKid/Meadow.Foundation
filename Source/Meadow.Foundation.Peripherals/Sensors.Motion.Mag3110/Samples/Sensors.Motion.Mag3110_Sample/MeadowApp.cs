﻿using System;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Sensors.Motion;

namespace MeadowApp
{
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        Mag3110 sensor;

        public MeadowApp()
        {
            InitHardware();

            while (true)
            {
                sensor.Read();

                Console.WriteLine($"X: {sensor.MagneticField3d.X}, " +
                    $"Y: {sensor.MagneticField3d.Y}, " +
                    $"Z: {sensor.MagneticField3d.Z}");

                Console.WriteLine($"Temperature: {sensor.Temperature.Celsius}");

                Thread.Sleep(1000);
            }
        }

        public void InitHardware()
        {
            Console.WriteLine("Init...");

            sensor = new Mag3110(Device.CreateI2cBus());
        }
    }
}