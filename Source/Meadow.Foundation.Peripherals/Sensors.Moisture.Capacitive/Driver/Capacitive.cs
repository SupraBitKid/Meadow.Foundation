﻿using Meadow.Hardware;
using Meadow.Peripherals.Sensors.Moisture;
using Meadow.Units;
using System;
using System.Threading.Tasks;

namespace Meadow.Foundation.Sensors.Moisture
{
    /// <summary>
    /// Capacitive Soil Moisture Sensor
    /// </summary>
    public class Capacitive : SensorBase<double>, IMoistureSensor
    {
        /// <summary>
        /// Raised when a new sensor reading has been made. To enable, call StartUpdating().
        /// </summary>
        public event EventHandler<IChangeResult<double>> HumidityUpdated = delegate { };

        /// <summary>
        /// Returns the analog input port
        /// </summary>
        public IAnalogInputPort AnalogInputPort { get; protected set; }

        /// <summary>
        /// Last value read from the moisture sensor.
        /// </summary>
        public double? Moisture { get; protected set; }

        /// <summary>
        /// Voltage value of most dry soil. Default of `0V`.
        /// </summary>
        public Voltage MinimumVoltageCalibration { get; set; } = new Voltage(0);

        /// <summary>
        /// Voltage value of most moist soil. Default of `3.3V`.
        /// </summary>
        public Voltage MaximumVoltageCalibration { get; set; } = new Voltage(3.3);

        /// <summary>
        /// Creates a Capacitive soil moisture sensor object with the specified analog pin and a IO device.
        /// </summary>
        /// <param name="device">The `IAnalogInputController` to create the port on.</param>
        /// <param name="analogInputPin">Analog pin the temperature sensor is connected to.</param>
        /// <param name="minimumVoltageCalibration">Minimum calibration voltage</param>
        /// <param name="maximumVoltageCalibration">Maximum calibration voltage</param>
        /// <param name="updateInterval">The time, to wait between sets of sample readings. 
        /// This value determines how often`Changed` events are raised and `IObservable` consumers are notified.</param>
        /// <param name="sampleCount">How many samples to take during a given
        /// reading. These are automatically averaged to reduce noise.</param>
        /// <param name="sampleInterval">The time, to wait in between samples during a reading.</param>
        public Capacitive(
            IAnalogInputController device, 
            IPin analogInputPin,
            Voltage? minimumVoltageCalibration, 
            Voltage? maximumVoltageCalibration,
            TimeSpan? updateInterval = null,
            int sampleCount = 5, 
            TimeSpan? sampleInterval = null)
                : this(
                    device.CreateAnalogInputPort(analogInputPin, sampleCount, sampleInterval ?? TimeSpan.FromMilliseconds(40), new Voltage(3.3)),
                    minimumVoltageCalibration, 
                    maximumVoltageCalibration)
        {
            UpdateInterval = updateInterval ?? TimeSpan.FromSeconds(5);
        }

        /// <summary>
        /// Creates a Capacitive soil moisture sensor object with the especified AnalogInputPort
        /// </summary>
        /// <param name="analogInputPort">The port for the analog input pin</param>
        /// <param name="minimumVoltageCalibration">Minimum calibration voltage</param>
        /// <param name="maximumVoltageCalibration">Maximum calibration voltage</param>
        public Capacitive(
            IAnalogInputPort analogInputPort,
            Voltage? minimumVoltageCalibration, 
            Voltage? maximumVoltageCalibration)
        {
            AnalogInputPort = analogInputPort;
            if(minimumVoltageCalibration is { } min) { MinimumVoltageCalibration = min; }
            if(maximumVoltageCalibration is { } max) { MaximumVoltageCalibration = max; }
        }

        /// <summary>
        /// Reads data from the sensor
        /// </summary>
        /// <returns>The latest sensor reading</returns>
        protected override async Task<double> ReadSensor()
        {
            var voltage = await AnalogInputPort.Read();
            var newMoisture = VoltageToMoisture(voltage);
            Moisture = newMoisture;
            return newMoisture;
        }

        /// <summary>
        /// Starts continuously sampling the sensor
        /// </summary>
        public void StartUpdating(TimeSpan? updateInterval)
        {
            AnalogInputPort.StartUpdating(updateInterval);
        }

        /// <summary>
        /// Stops sampling the sensor.
        /// </summary>
        public void StopUpdating()
        {
            AnalogInputPort.StopUpdating();
        }

        /// <summary>
        /// Raise change events for subscribers
        /// </summary>
        /// <param name="changeResult">The change result with the current sensor data</param>
        protected void RaiseChangedAndNotify(IChangeResult<double> changeResult)
        {
            HumidityUpdated?.Invoke(this, changeResult);
            base.RaiseEventsAndNotify(changeResult);
        }

        /// <summary>
        /// Converts voltage to moisture value, ranging from 0 (most dry) to 1 (most wet)
        /// </summary>
        /// <param name="voltage"></param>
        protected double VoltageToMoisture(Voltage voltage)
        {
            if (MinimumVoltageCalibration > MaximumVoltageCalibration) 
            {
                return (1f - voltage.Volts.Map(MaximumVoltageCalibration.Volts, MinimumVoltageCalibration.Volts, 0f, 1.0f));
            }

            return (1f - voltage.Volts.Map(MinimumVoltageCalibration.Volts, MaximumVoltageCalibration.Volts, 0f, 1.0f));
        }
    }
}