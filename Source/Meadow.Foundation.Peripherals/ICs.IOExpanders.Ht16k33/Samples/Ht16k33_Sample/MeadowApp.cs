﻿using System;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation.ICs.IOExpanders;

namespace ICs.IOExpanders.HT16K33_Sample
{
    public class MeadowApp : App<F7MicroV2, MeadowApp>
    {
        //<!—SNIP—>

        protected Ht16k33 ht16k33;

        public MeadowApp()
        {
            Console.WriteLine("Initialize...");
            ht16k33 = new Ht16k33(Device.CreateI2cBus());

            int index = 0;
            bool on = true;

            while (true)
            {
                ht16k33.SetLed((byte)index, on);
                ht16k33.UpdateDisplay();
                index++;

                if (index >= 128)
                {
                    index = 0;
                    on = !on;
                }

                Thread.Sleep(100);
            }
        }

        //<!—SNOP—>
    }
}