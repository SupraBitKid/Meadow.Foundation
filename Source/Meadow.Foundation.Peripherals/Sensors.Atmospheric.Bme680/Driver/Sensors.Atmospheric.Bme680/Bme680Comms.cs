﻿using System;

namespace Meadow.Foundation.Sensors.Atmospheric
{
    internal abstract class Bme680Comms
    {
        public abstract void WriteRegister(byte register, byte value);
        public abstract void ReadRegisters(byte address, Span<byte> readBuffer);

        public abstract byte ReadRegister(byte address);
    }
}