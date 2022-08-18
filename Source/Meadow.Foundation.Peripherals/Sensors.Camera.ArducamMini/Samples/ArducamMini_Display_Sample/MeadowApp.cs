﻿using System;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Sensors.Camera;
using Meadow.Foundation.Graphics;
using NanoJpeg;
using Meadow.Foundation.Displays.TftSpi;
using System.Threading.Tasks;

namespace MeadowApp
{
    public class MeadowApp : App<F7FeatherV2>
    {
        ArducamMini camera;
        MicroGraphics graphics;
        St7789 display;

        public override Task Initialize()
        {
            Console.WriteLine("Creating output ports...");

            var spiBus = Device.CreateSpiBus();

            camera = new ArducamMini(Device, spiBus, Device.Pins.D00, Device.CreateI2cBus());

            display = new St7789(Device, spiBus,
                Device.Pins.D04, Device.Pins.D03, Device.Pins.D02, 135, 240);

            graphics = new MicroGraphics(display)
            {
                CurrentFont = new Font12x20(),
                Rotation = RotationType._90Degrees
            };

            return Task.CompletedTask;
        }

        public override Task Run()
        {
            var data = CaptureImage();

            JpegTest(data);

            return Task.CompletedTask;
        }

        void JpegTest(byte[] data)
        {
            var nanoJpeg = new NanoJPEG();

            nanoJpeg.njDecode(data);

            Console.WriteLine("Jpg decoded");

            var jpg = nanoJpeg.GetImage();

            Console.WriteLine($"Jpeg decoded is {jpg.Length} bytes");
            Console.WriteLine($"Width {nanoJpeg.Width}");
            Console.WriteLine($"Height {nanoJpeg.Height}");

            graphics.Clear();

            int x = 0;
            int y = 0;
            byte r, g, b;

            for (int i = 0; i < jpg.Length; i += 3)
            {
                r = jpg[i];
                g = jpg[i + 1];
                b = jpg[i + 2];

                display.DrawPixel(x, y, r, g, b);

                x++;

                if (x % 240 == 0)
                {
                    y++;
                    x = 0;
                }

                if(y >= 135)
                {
                    break;
                }
            }

            Console.WriteLine("Jpeg show");

            display.Show();
        }

        byte[] CaptureImage()
        { 
            Thread.Sleep(200);

            Console.WriteLine("Attempting single capture");
            camera.FlushFifo();
            camera.CapturePhoto();

            Console.WriteLine("Capture started");

            byte[] data = null;

            Thread.Sleep(1000);

            if (camera.IsPhotoAvaliable())
            {
                Console.WriteLine("Capture complete");

                data = camera.GetImageData();

                Console.WriteLine($"Jpeg captured {data.Length}");
            }

            return data;
        }
    }
}