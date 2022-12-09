﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Meadow.Hardware;
using Meadow.Peripherals.Sensors;
using Meadow.Units;

namespace Meadow.Foundation.Sensors.Atmospheric
{
    /// <summary>
    /// Provide a mechanism for reading the Temperature and Humidity from
    /// a DHT temperature and Humidity sensor.
    /// </summary>
    public abstract partial class DhtBase : 
        ByteCommsSensorBase<(Units.Temperature? Temperature, RelativeHumidity? Humidity)>,
        ITemperatureSensor, IHumiditySensor
    {
        /// <summary>
        /// Raised when the temperature value changes
        /// </summary>
        public event EventHandler<IChangeResult<Units.Temperature>> TemperatureUpdated = delegate { };

        /// <summary>
        /// Raised when the humidity value changes
        /// </summary>
        public event EventHandler<IChangeResult<RelativeHumidity>> HumidityUpdated = delegate { };

        private readonly BusType protocol;
        private int lastMeasurement = 0;

        /// <summary>
        /// The temperature
        /// </summary>
        public Units.Temperature? Temperature => Conditions.Temperature;

        /// <summary>
        /// The relative humidity
        /// </summary>
        public RelativeHumidity? Humidity => Conditions.Humidity;

        /// <summary>
        /// How last read went, true for success, false for failure
        /// </summary>
        public bool WasLastReadSuccessful { get; internal set; }

        /// <summary>
        /// Create a DHT sensor through I2C (Only DHT12)
        /// </summary>
        /// <param name="i2cBus">The I2C bus connected to the sensor</param>
        /// <param name="address">The I2C address</param>
        public DhtBase(II2cBus i2cBus, byte address = (byte)Addresses.Default)
            : base(i2cBus, address, writeBufferSize: 8, readBufferSize: 6)
        {
            protocol = BusType.I2C;

            //give the device time to initialize
            Thread.Sleep(1000);
        }

        /// <summary>
        /// Start a reading
        /// </summary>
        internal virtual void ReadData()
        {
            if (protocol == BusType.OneWire) 
            {
                ReadDataOneWire();
            } 
            else 
            {
                ReadDataI2c();
            }
        }

        /// <summary>
        /// Read through One-Wire
        /// </summary>
        internal virtual void ReadDataOneWire()
        {
            throw new NotImplementedException("ReadDataOneWire()");
        }

        /// <summary>
        /// Read data via I2C (DHT12, etc.)
        /// </summary>
        internal virtual void ReadDataI2c()
        {
            Peripheral?.Write(0x00);
            Peripheral?.Read(ReadBuffer.Span[0..5]);

            lastMeasurement = Environment.TickCount;

            if ((ReadBuffer.Span[4] == ((ReadBuffer.Span[0] + ReadBuffer.Span[1] + ReadBuffer.Span[2] + ReadBuffer.Span[3]) & 0xFF))) 
            {
                WasLastReadSuccessful = (ReadBuffer.Span[0] != 0) || (ReadBuffer.Span[2] != 0);
            } 
            else 
            {
                WasLastReadSuccessful = false;
            }
        }

        /// <summary>
        /// Converting data to humidity
        /// </summary>
        /// <returns>Humidity</returns>
        internal abstract float GetHumidity();

        /// <summary>
        /// Converting data to Temperature
        /// </summary>
        /// <returns>Temperature</returns>
        internal abstract float GetTemperature();

        /// <summary>
        /// Raise events for subcribers and notify of value changes
        /// </summary>
        /// <param name="changeResult">The updated sensor data</param>
        protected override void RaiseEventsAndNotify(IChangeResult<(Units.Temperature? Temperature, RelativeHumidity? Humidity)> changeResult)
        {
            if (changeResult.New.Temperature is { } temp) {
                TemperatureUpdated?.Invoke(this, new ChangeResult<Units.Temperature>(temp, changeResult.Old?.Temperature));
            }
            if (changeResult.New.Humidity is { } humidity) {
                HumidityUpdated?.Invoke(this, new ChangeResult<Units.RelativeHumidity>(humidity, changeResult.Old?.Humidity));
            }
            base.RaiseEventsAndNotify(changeResult);
        }

        /// <summary>
        /// Reads data from the sensor
        /// </summary>
        /// <returns>The latest sensor reading</returns>
        protected override Task<(Units.Temperature? Temperature, RelativeHumidity? Humidity)> ReadSensor()
        {
            (Units.Temperature? Temperature, RelativeHumidity? Humidity) conditions;

            if (protocol == BusType.I2C) 
            {
                ReadDataI2c();
            } 
            else 
            {
                ReadDataOneWire();
            }

            conditions.Humidity = new RelativeHumidity(GetHumidity(), RelativeHumidity.UnitType.Percent);
            conditions.Temperature = new Units.Temperature(GetTemperature(), Units.Temperature.UnitType.Celsius);

            return Task.FromResult(conditions);
        }

        async Task<Units.Temperature> ISamplingSensor<Units.Temperature>.Read()
            => (await Read()).Temperature.Value;

        async Task<RelativeHumidity> ISamplingSensor<RelativeHumidity>.Read()
            => (await Read()).Humidity.Value;
    }
}