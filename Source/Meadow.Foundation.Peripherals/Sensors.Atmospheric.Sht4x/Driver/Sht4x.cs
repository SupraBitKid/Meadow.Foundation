﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Meadow.Hardware;
using Meadow.Peripherals.Sensors;
using Meadow.Units;

namespace Meadow.Foundation.Sensors.Atmospheric
{
    /// <summary>
    /// Provide a mechanism for reading the temperature and humidity from
    /// a SHT4x temperature / humidity sensor (SHT40, SHT41, SHT45, etc.)
    /// </summary>
    /// <remarks>
    /// Readings from the sensor are made in Single-shot mode.
    /// </remarks>
    public partial class Sht4x :
        ByteCommsSensorBase<(Units.Temperature? Temperature, RelativeHumidity? Humidity)>,
        ITemperatureSensor, IHumiditySensor
    {
        /// <summary>
        /// Precision of sensor reading
        /// </summary>
        public Precision ReadPrecision { get; protected set; } = Precision.HighPrecisionNoHeat;

        /// <summary>
        /// Temperature changed event handler
        /// </summary>
        public event EventHandler<IChangeResult<Units.Temperature>> TemperatureUpdated = delegate { };

        /// <summary>
        /// Humidity changed event handler
        /// </summary>
        public event EventHandler<IChangeResult<RelativeHumidity>> HumidityUpdated = delegate { };

        /// <summary>
        /// The curren temperature -from the last reading.
        /// </summary>
        public Units.Temperature? Temperature => Conditions.Temperature;

        /// <summary>
        /// The humidity, in percent relative humidity, from the last reading
        /// </summary>
        public RelativeHumidity? Humidity => Conditions.Humidity;

        /// <summary>
        /// Create a new SHT4x object
        /// </summary>
        /// <param name="address">Sensor address (0x44 or 0x45)</param>
        /// <param name="i2cBus">I2cBus</param>
        public Sht4x(II2cBus i2cBus, byte address = (byte)Addresses.Default)
            : base(i2cBus, address, readBufferSize: 6, writeBufferSize: 2)
        {
        }

        /// <summary>
        /// Raise events for subscribers
        /// </summary>
        /// <param name="changeResult"></param>
        protected override void RaiseEventsAndNotify(IChangeResult<(Units.Temperature? Temperature, RelativeHumidity? Humidity)> changeResult)
        {
            if (changeResult.New.Temperature is { } temp)
            {
                TemperatureUpdated?.Invoke(this, new ChangeResult<Units.Temperature>(temp, changeResult.Old?.Temperature));
            }
            if (changeResult.New.Humidity is { } humidity)
            {
                HumidityUpdated?.Invoke(this, new ChangeResult<Units.RelativeHumidity>(humidity, changeResult.Old?.Humidity));
            }
            base.RaiseEventsAndNotify(changeResult);
        }

        /// <summary>
        /// Returns the appropriate delay in ms for the set precision
        /// </summary>
        /// <param name="precision">Precision to calculate delay</param>
        /// <returns></returns>
        protected int GetDelayForPrecision(Precision precision)
        {
            int delay = 10;

            switch (precision)
            {
                case Precision.HighPrecisionNoHeat:
                    delay = 10;
                    break;
                case Precision.MediumPrecisionNoHeat:
                    delay = 5;
                    break;
                case Precision.LowPrecisionNoHeat:
                    delay = 2;
                    break;
                case Precision.HighPrecisionHighHeat1s:
                case Precision.HighPrecisionMediumHeat1s:
                case Precision.HighPrecisionLowHeat1s:
                    delay = 1100;
                    break;
                case Precision.HighPrecisionHighHeat100ms:
                case Precision.HighPrecisionMediumHeat100ms:
                case Precision.HighPrecisionLowHeat100ms:
                    delay = 110;
                    break;
            }

            return delay;
        }

        /// <summary>
        /// Get a reading from the sensor and set the Temperature and Humidity properties.
        /// </summary>
        protected async override Task<(Units.Temperature? Temperature, RelativeHumidity? Humidity)> ReadSensor()
        {
            (Units.Temperature Temperature, RelativeHumidity Humidity) conditions;

            return await Task.Run(() =>
            {
                Peripheral?.Write((byte)ReadPrecision);
                Thread.Sleep(GetDelayForPrecision(ReadPrecision));
                Peripheral?.Read(ReadBuffer.Span[0..5]);

                var temperature = (175 * (float)((ReadBuffer.Span[0] << 8) + ReadBuffer.Span[1]) / 65535) - 45;
                var humidity = (125 * (float)((ReadBuffer.Span[3] << 8) + ReadBuffer.Span[4]) / 65535) - 6;

                conditions.Humidity = new RelativeHumidity(humidity);
                conditions.Temperature = new Units.Temperature(temperature, Units.Temperature.UnitType.Celsius);

                return conditions;
            });
        }
    }
}