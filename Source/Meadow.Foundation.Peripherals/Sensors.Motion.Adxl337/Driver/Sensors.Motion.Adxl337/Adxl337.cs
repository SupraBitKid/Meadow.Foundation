﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Meadow.Foundation.Spatial;
using Meadow.Hardware;
using Meadow.Peripherals.Sensors.Motion;
using Meadow.Units;

namespace Meadow.Foundation.Sensors.Motion
{
    /// <summary>
    /// Driver for the ADXL337 triple axis accelerometer.
    /// +/- 5g
    /// </summary>
    public class Adxl337 :
        FilterableChangeObservableBase<Acceleration3D>,
        IAccelerometer
    {
        //==== events
        // [Bryan (2021.05.16)] commented this out, it's a duplicate of the other, no?
        //public event EventHandler<IChangeResult<Acceleration3D>> Updated;
        public event EventHandler<IChangeResult<Acceleration3D>> Acceleration3DUpdated;

        //==== internals
        /// <summary>
        /// Analog input channel connected to the x axis.
        /// </summary>
        private readonly IAnalogInputPort _xPort;

        /// <summary>
        /// Analog input channel connected to the x axis.
        /// </summary>
        private readonly IAnalogInputPort _yPort;

        /// <summary>
        /// Analog input channel connected to the x axis.
        /// </summary>
        private readonly IAnalogInputPort _zPort;

        /// <summary>
        /// Voltage that represents 0g.  This is the supply voltage / 2.
        /// </summary>
        private float _zeroGVoltage => SupplyVoltage / 2f;

        // internal thread lock
        private object _lock = new object();
        private CancellationTokenSource SamplingTokenSource;

        //==== properties
        /// <summary>
        /// Minimum value that can be used for the update interval when the
        /// sensor is being configured to generate interrupts.
        /// </summary>
        public const ushort MinimumPollingPeriod = 100;

        /// <summary>
        /// Volts per G for the X axis.
        /// </summary>
        public float XVoltsPerG { get; set; }

        /// <summary>
        /// Volts per G for the X axis.
        /// </summary>
        public float YVoltsPerG { get; set; }

        /// <summary>
        /// Volts per G for the X axis.
        /// </summary>
        public float ZVoltsPerG { get; set; }

        /// <summary>
        /// Power supply voltage applied to the sensor.  This will be set (in the constructor)
        /// to 3.3V by default.
        /// </summary>
        public float SupplyVoltage { get; set; }

        public Acceleration3D? Acceleration3D { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the analog input port is currently
        /// sampling the ADC. Call StartSampling() to spin up the sampling process.
        /// </summary>
        /// <value><c>true</c> if sampling; otherwise, <c>false</c>.</value>
        public bool IsSampling { get; protected set; } = false;

        /// <summary>
        /// Create a new ADXL337 sensor object.
        /// </summary>
        /// <param name="xPin">Analog pin connected to the X axis output from the ADXL337 sensor.</param>
        /// <param name="yPin">Analog pin connected to the Y axis output from the ADXL337 sensor.</param>
        /// <param name="zPin">Analog pin connected to the Z axis output from the ADXL337 sensor.</param>
        public Adxl337(IAnalogInputController device, IPin xPin, IPin yPin, IPin zPin)
        {
            _xPort = device.CreateAnalogInputPort(xPin);
            _yPort = device.CreateAnalogInputPort(yPin);
            _zPort = device.CreateAnalogInputPort(zPin);
            //
            //  Now set the default calibration data.
            //
            XVoltsPerG = 0.325f;
            YVoltsPerG = 0.325f;
            ZVoltsPerG = 0.550f;
            SupplyVoltage = 3.3f;
        }

        /// <summary>
        /// Convenience method to get the current temperature. For frequent reads, use
        /// StartSampling() and StopSampling() in conjunction with the SampleBuffer.
        /// </summary>
        public async Task<Acceleration3D> Read()
        {
            // TODO: why does this method return a `Task`? should `Update` be awaited
            // or something?
            // seems like the ADXL335 might have the right pattern here.

            await Update();

            return Acceleration3D.Value;
        }

        /// <summary>
        /// Starts continuously sampling the sensor.
        ///
        /// This method also starts raising `Changed` events and IObservable
        /// subscribers getting notified.
        /// </summary>
        public void StartUpdating(int standbyDuration = 1000)
        {
            // thread safety
            lock (_lock) 
            {
                if (IsSampling) { return; }

                // state muh-cheen
                IsSampling = true;

                SamplingTokenSource = new CancellationTokenSource();
                CancellationToken ct = SamplingTokenSource.Token;

                Acceleration3D? oldConditions;
                ChangeResult<Acceleration3D> result;
                Task.Factory.StartNew(async () => 
                {
                    while (true) 
                    {
                        if (ct.IsCancellationRequested) 
                        {   // do task clean up here
                            observers.ForEach(x => x.OnCompleted());
                            break;
                        }
                        // capture history
                        oldConditions = Acceleration3D;

                        // read
                        await Update();

                        // build a new result with the old and new conditions
                        result = new ChangeResult<Acceleration3D>(Acceleration3D.Value, oldConditions);

                        // let everyone know
                        RaiseChangedAndNotify(result);

                        // sleep for the appropriate interval
                        await Task.Delay(standbyDuration);
                    }
                }, SamplingTokenSource.Token);
            }
        }

        protected void RaiseChangedAndNotify(IChangeResult<Acceleration3D> changeResult)
        {
            //Updated?.Invoke(this, changeResult);
            Acceleration3DUpdated?.Invoke(this, changeResult);
            base.NotifyObservers(changeResult);
        }

        /// <summary>
        /// Stops sampling the acceleration.
        /// </summary>
        public void StopUpdating()
        {
            lock (_lock) 
            {
                if (!IsSampling) { return; }

                SamplingTokenSource?.Cancel();

                // state muh-cheen
                IsSampling = false;
            }
        }

        /// <summary>
        /// Read the sensor output and convert the sensor readings into acceleration values.
        /// </summary>
        public async Task Update()
        {
            var x = await _xPort.Read();
            var y = await _yPort.Read();
            var z = await _zPort.Read();

            Acceleration3D = new Acceleration3D(
                new Acceleration((x.Volts - _zeroGVoltage) / XVoltsPerG, Acceleration.UnitType.Gravity),
                new Acceleration((y.Volts - _zeroGVoltage) / YVoltsPerG, Acceleration.UnitType.Gravity),
                new Acceleration((z.Volts - _zeroGVoltage) / ZVoltsPerG, Acceleration.UnitType.Gravity)
                );
        }

        /// <summary>
        /// Get the raw analog input values from the sensor.
        /// </summary>
        /// <returns>Vector object containing the raw sensor data from the analog pins.</returns>
        public async Task<(Voltage XVolts, Voltage YVolts, Voltage ZVolts)> GetRawSensorData()
        {
            var x = await _xPort.Read();
            var y = await _yPort.Read();
            var z = await _zPort.Read();

            return (x, y, z);
        }
    }
}