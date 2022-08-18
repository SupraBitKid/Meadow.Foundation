﻿using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Sensors.Hid;
using System;
using System.Threading.Tasks;

namespace SnesClassicController_Sample
{
    public class MeadowApp : App<F7FeatherV2>
    {
        //<!=SNIP=>

        SnesClassicController snesController;

        public override Task Initialize()
        {
            Console.WriteLine("Initialize...");
        
            var i2cBus = Device.CreateI2cBus(SnesClassicController.DefaultSpeed);

            snesController = new SnesClassicController(i2cBus: i2cBus);

            //onetime update - could be used in a game loop
            snesController.Update();

            //check the state of a button
            Console.WriteLine("X Button is " + (snesController.XButton.State == true ? "pressed" : "not pressed"));

            //.NET events
            snesController.AButton.Clicked += (s, e) => Console.WriteLine("A button clicked");
            snesController.BButton.Clicked += (s, e) => Console.WriteLine("B button clicked");
            snesController.XButton.Clicked += (s, e) => Console.WriteLine("X button clicked");
            snesController.YButton.Clicked += (s, e) => Console.WriteLine("Y button clicked");

            snesController.LButton.Clicked += (s, e) => Console.WriteLine("L button clicked");
            snesController.RButton.Clicked += (s, e) => Console.WriteLine("R button clicked");

            snesController.StartButton.Clicked += (s, e) => Console.WriteLine("+ button clicked");
            snesController.SelectButton.Clicked += (s, e) => Console.WriteLine("- button clicked");

            snesController.DPad.Updated += (s, e) => Console.WriteLine($"DPad {e.New}");

            return Task.CompletedTask;
        }

        public override Task Run()
        {
            snesController.StartUpdating(TimeSpan.FromMilliseconds(200));
            return Task.CompletedTask;
        }

        //<!=SNOP=>
    }
}