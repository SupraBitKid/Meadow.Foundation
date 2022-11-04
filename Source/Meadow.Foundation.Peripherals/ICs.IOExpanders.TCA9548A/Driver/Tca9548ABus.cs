﻿using Meadow.Hardware;
using Meadow.Units;
using System;

namespace Meadow.Foundation.ICs.IOExpanders
{
    public class Tca9548aBus : II2cBus
    {
        private readonly Tca9548a _tca9548a;
        private readonly byte _busIndex;

        private byte[] _sendBuffer = new byte[1];

        internal Tca9548aBus(Tca9548a tca9548A, int frequency, byte busIndex)
        {
            _tca9548a = tca9548A;
            Frequency = frequency;
            _busIndex = busIndex;
        }

        public int Frequency { get; set; }
        Frequency II2cBus.Frequency { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void WriteData(byte peripheralAddress, params byte[] data)
        {
            _tca9548a.BusSelectorSemaphore.Wait(TimeSpan.FromSeconds(10));
            try
            {
                _tca9548a.SelectBus(_busIndex);
                _tca9548a.Bus.Write(peripheralAddress, data);
            }
            finally
            {
                _tca9548a.BusSelectorSemaphore.Release();
            }
        }

        /*
        public void WriteData(byte peripheralAddress, IEnumerable<byte> data)
        {
            _tca9548a.BusSelectorSemaphore.Wait(TimeSpan.FromSeconds(10));
            try
            {
                _tca9548a.SelectBus(_busIndex);
                _tca9548a.Bus.Write(peripheralAddress, data);
            }
            finally
            {
                _tca9548a.BusSelectorSemaphore.Release();
            }
        }*/

        public void Write(byte peripheralAddress, Span<byte> data)
        {
            _tca9548a.BusSelectorSemaphore.Wait(TimeSpan.FromSeconds(10));
            try
            {
                _tca9548a.SelectBus(_busIndex);
                _tca9548a.Bus.Write(peripheralAddress, data);
            }
            finally
            {
                _tca9548a.BusSelectorSemaphore.Release();
            }
        }

        public void Exchange(byte peripheralAddress, Span<byte> writeBuffer, Span<byte> readBuffer)
        {
            _tca9548a.BusSelectorSemaphore.Wait(TimeSpan.FromSeconds(10));
            try
            {
                _tca9548a.SelectBus(_busIndex);
                _tca9548a.Bus.Exchange(peripheralAddress, writeBuffer, readBuffer);
            }
            finally
            {
                _tca9548a.BusSelectorSemaphore.Release();
            }
        }

        public byte[] ReadData(byte peripheralAddress, int numberOfBytes)
        {
            _tca9548a.BusSelectorSemaphore.Wait(TimeSpan.FromSeconds(10));
            try
            {
                _tca9548a.SelectBus(_busIndex);
                var data = new byte[numberOfBytes];
                _tca9548a.Bus.Read(peripheralAddress, data);
                return data;
            }
            finally
            {
                _tca9548a.BusSelectorSemaphore.Release();
            }
        }

        public void WriteData(byte peripheralAddress, Span<byte> data, int length)
        {
            _tca9548a.BusSelectorSemaphore.Wait(TimeSpan.FromSeconds(10));
            try 
            {
                _tca9548a.SelectBus(_busIndex);
                _tca9548a.Bus.Write(peripheralAddress, data[..length]);
            } 
            finally
            {
                _tca9548a.BusSelectorSemaphore.Release();
            }
        }

        public void ExchangeData(byte peripheralAddress, Span<byte> writeBuffer, int writeCount, Span<byte> readBuffer, int readCount)
        {
            _tca9548a.BusSelectorSemaphore.Wait(TimeSpan.FromSeconds(10));
            try
            {
                _tca9548a.SelectBus(_busIndex);
                _tca9548a.Bus.Exchange(peripheralAddress, writeBuffer[0..writeCount], readBuffer[0..readCount]);
            } 
            finally 
            {
                _tca9548a.BusSelectorSemaphore.Release();
            }
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public void Read(byte peripheralAddress, Span<byte> readBuffer)
        {
            _tca9548a.BusSelectorSemaphore.Wait(TimeSpan.FromSeconds(10));
            try {
                _tca9548a.SelectBus(_busIndex);
                _tca9548a.Bus.Read(peripheralAddress, readBuffer);
            } finally {
                _tca9548a.BusSelectorSemaphore.Release();
            }
        }
    }
}
