﻿using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Audio.Mp3;
using System;
using System.Threading.Tasks;

namespace MeadowApp
{
    public class MeadowApp : App<F7FeatherV2>
    {
        //<!=SNIP=>

        Yx5300 mp3Player;

        public override Task Initialize()
        {
            Console.WriteLine("Initialize...");

            mp3Player = new Yx5300(Device, Device.SerialPortNames.Com4);

            return Task.CompletedTask;
        }

        public override async Task Run()
        {
            mp3Player.SetVolume(15);

            var status = await mp3Player.GetStatus();
            Console.WriteLine($"Status: {status}");

            var count = await mp3Player.GetNumberOfTracksInFolder(0);
            Console.WriteLine($"Number of tracks: {count}");

            mp3Player.Play();

            await Task.Delay(5000); //leave playing for 5 seconds

            mp3Player.Next();

            await Task.Delay(5000); //leave playing for 5 seconds
        }

        //<!=SNOP=>
    }
}