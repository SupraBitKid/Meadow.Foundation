﻿using Meadow.Hardware;
using Meadow.Peripherals.Sensors;
using Meadow.Units;
using Meadow.Utilities;
using System;
using System.Threading.Tasks;

namespace Meadow.Foundation.Sensors.Atmospheric
{
    /// <summary>
    /// Device driver for the Adafruit MPRLS Ported Pressure Sensor Breakout
    /// https://www.adafruit.com/product/3965
    /// Device datasheets also available here: https://sensing.honeywell.com/micropressure-mpr-series
    /// </summary>
    public partial class AdafruitMPRLS : ByteCommsSensorBase<(Pressure? Pressure, Pressure? RawPsiMeasurement)>, IBarometricPressureSensor
    {
        //Defined in section 6.6.1 of the datasheet.
        private readonly byte[] mprlsMeasurementCommand = { 0xAA, 0x00, 0x00 };

        private const int MINIMUM_PSI = 0;
        private const int MAXIMUM_PSI = 25;

        /// <summary>
        /// Raised when a new reading has been made. Events will only be raised
        /// while the driver is updating. To start, call the `StartUpdating()`
        /// method.
        /// </summary>
        public event EventHandler<IChangeResult<Pressure>> PressureUpdated = delegate { };

        /// <summary>
        /// Set by the sensor, to tell us it has power.
        /// </summary>
        public bool IsDevicePowered { get; set; }

        /// <summary>
        /// Set by the sensor, to tell us it's busy.
        /// </summary>
        public bool IsDeviceBusy { get; set; }

        /// <summary>
        /// Set by the sensor, to tell us whether or not there's an issue with its own memory.
        /// </summary>
        public bool HasMemoryIntegrityFailed { get; set; }

        /// <summary>
        /// Returns the current raw pressure value in pounds per square inch (PSI)
        /// </summary>
        public Pressure? RawPsiMeasurement => Conditions.RawPsiMeasurement;

        /// <summary>
        /// Returns the current pressure reading
        /// </summary>
        public Pressure? Pressure => Conditions.Pressure;

        /// <summary>
        /// Indicates the sensor has reached its pressure limit.
        /// </summary>
        public bool InternalMathSaturated { get; set; }

        /// <summary>
        /// Represents an Adafruit MPRLS Ported Pressure Sensor
        /// </summary>
        /// <param name="i2cbus">I2Cbus connected to the sensor</param>
        public AdafruitMPRLS(II2cBus i2cbus)
            : base(i2cbus, (byte)Addresses.Default)
        { }

        /// <summary>
        /// Notify subscribers of PressureUpdated event handler
        /// </summary>
        /// <param name="changeResult"></param>
        protected override void RaiseEventsAndNotify(IChangeResult<(Pressure? Pressure, Pressure? RawPsiMeasurement)> changeResult)
        {
            if (changeResult.New.Pressure is { } pressure)
            {
                PressureUpdated?.Invoke(this, new ChangeResult<Pressure>(pressure, changeResult.Old?.Pressure));
            }
            base.RaiseEventsAndNotify(changeResult);
        }

        /// <summary>
        /// Convenience method to get the current Pressure. For frequent reads, use
        /// StartSampling() and StopSampling() in conjunction with the SampleBuffer.
        /// </summary>
        protected override async Task<(Pressure? Pressure, Pressure? RawPsiMeasurement)> ReadSensor()
        {
            return await Task.Run(async () =>
            {
                //Send the command to the sensor to tell it to do the thing.
                Peripheral.Write(mprlsMeasurementCommand);

                //Datasheet says wait 5 ms.
                await Task.Delay(5);

                while (true)
                {
                    Peripheral.Read(ReadBuffer.Span[0..1]);

                    //From section 6.5 of the datasheet.
                    IsDevicePowered = BitHelpers.GetBitValue(ReadBuffer.Span[0], 6);
                    IsDeviceBusy = BitHelpers.GetBitValue(ReadBuffer.Span[0], 5);
                    HasMemoryIntegrityFailed = BitHelpers.GetBitValue(ReadBuffer.Span[0], 2);
                    InternalMathSaturated = BitHelpers.GetBitValue(ReadBuffer.Span[0], 0);

                    if (InternalMathSaturated)
                    {
                        throw new InvalidOperationException("Sensor pressure has exceeded max value!");
                    }

                    if (HasMemoryIntegrityFailed)
                    {
                        throw new InvalidOperationException("Sensor internal memory integrity check failed!");
                    }

                    if (!(IsDeviceBusy))
                    {
                        break;
                    }
                }

                Peripheral.Read(ReadBuffer.Span[0..4]);

                var rawPSIMeasurement = (ReadBuffer.Span[1] << 16) | (ReadBuffer.Span[2] << 8) | ReadBuffer.Span[3];

                //From Section 8.0 of the datasheet.
                var calculatedPSIMeasurement = (rawPSIMeasurement - 1677722) * (MAXIMUM_PSI - MINIMUM_PSI);
                calculatedPSIMeasurement /= 15099494 - 1677722;
                calculatedPSIMeasurement += MINIMUM_PSI;

                (Pressure? Pressure, Pressure? RawPsiMeasurement) conditions;

                conditions.RawPsiMeasurement = new Pressure(rawPSIMeasurement, Units.Pressure.UnitType.Psi);
                conditions.Pressure = new Pressure(calculatedPSIMeasurement, Units.Pressure.UnitType.Psi);

                return conditions;
            });
        }

        async Task<Pressure> ISamplingSensor<Pressure>.Read()
            => (await ReadSensor()).Pressure.Value;
    }
}