﻿using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Leds;
using Meadow.Peripherals.Leds;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Leds.Led_Sample
{
    public class MeadowApp : App<F7FeatherV2>
    {
        //<!=SNIP=>

        List<Led> leds;

        public override Task Initialize()
        {
            var onRgbLed = new RgbLed(
                device: Device,
                redPin: Device.Pins.OnboardLedRed,
                greenPin: Device.Pins.OnboardLedGreen,
                bluePin: Device.Pins.OnboardLedBlue);
            onRgbLed.SetColor(RgbLedColors.Red);

            leds = new List<Led>
            {
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D00, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D01, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D02, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D03, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D04, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D05, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D06, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D07, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D08, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D09, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D10, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D11, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D12, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D13, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D14, false)),
                new Led(Device.CreateDigitalOutputPort(Device.Pins.D15, false))
            };

            onRgbLed.SetColor(RgbLedColors.Green);

            return Task.CompletedTask;
        }

        public override async Task Run()
        {
            Console.WriteLine("TestLeds...");

            while (true)
            {
                Console.WriteLine("Turning on each led every 100ms");
                foreach (var led in leds)
                {
                    led.IsOn = true;
                    await Task.Delay(100);
                }

                await Task.Delay(1000);

                Console.WriteLine("Turning off each led every 100ms");
                foreach (var led in leds)
                {
                    led.IsOn = false;
                    await Task.Delay(100);
                }

                await Task.Delay(1000);

                Console.WriteLine("Blinking the LEDs for a second each");
                foreach (var led in leds)
                {
                    led.StartBlink();
                    await Task.Delay(3000);
                    led.Stop();
                }

                Console.WriteLine("Blinking the LEDs for a second each with on (1s) and off (1s)");
                foreach (var led in leds)
                {
                    led.StartBlink(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                    await Task.Delay(3000);
                    led.Stop();
                }

                await Task.Delay(3000);
            }
        }

        //<!=SNOP=>
    }
}