﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Meadow.Hardware;
using Meadow.Units;

namespace Meadow.Foundation.Sensors.Light
{
    public class Alspt19315C :
        FilterableChangeObservableBase<Voltage>
    {
        //==== Events
        public event EventHandler<IChangeResult<Voltage>> Updated;

        //==== internals
        // internal thread lock
        private object _lock = new object();
        private CancellationTokenSource SamplingTokenSource;

        /// <summary>
        ///     Analog port connected to the sensor.
        /// </summary>
        private readonly IAnalogInputPort sensor;

        //==== Properties
        public Voltage Voltage { get; protected set; } = new Voltage(0);

        /// <summary>
        /// Gets a value indicating whether the analog input port is currently
        /// sampling the ADC. Call StartSampling() to spin up the sampling process.
        /// </summary>
        /// <value><c>true</c> if sampling; otherwise, <c>false</c>.</value>
        public bool IsSampling { get; protected set; } = false;

        //==== constructors

        // TODO: we are missing a constructor here that takes a port

        /// <summary>
        ///     Create a new light sensor object using a static reference voltage.
        /// </summary>
        /// <param name="pin">AnalogChannel connected to the sensor.</param>
        public Alspt19315C(IAnalogInputController device, IPin pin)
        {
            sensor = device.CreateAnalogInputPort(pin);
        }

        //==== methods

        /// <summary>
        ///     Voltage being output by the sensor.
        /// </summary>
        public async Task<Voltage> Read()
        {
            await Update();

            return Voltage;
        }

        ///// <summary>
        ///// Starts continuously sampling the sensor.
        /////
        ///// This method also starts raising `Changed` events and IObservable
        ///// subscribers getting notified.
        ///// </summary>
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

                Voltage oldConditions;
                ChangeResult<Voltage> result;
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
                        oldConditions = Voltage;

                        // read
                        await Update();

                        // build a new result with the old and new conditions
                        result = new ChangeResult<Voltage>(oldConditions, Voltage);

                        // let everyone know
                        RaiseChangedAndNotify(result);

                        // sleep for the appropriate interval
                        await Task.Delay(standbyDuration);
                    }
                }, SamplingTokenSource.Token);
            }
        }

        protected void RaiseChangedAndNotify(IChangeResult<Voltage> changeResult)
        {
            Updated?.Invoke(this, changeResult);
            base.NotifyObservers(changeResult);
        }

        ///// <summary>
        ///// Stops sampling the acceleration.
        ///// </summary>
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
            Voltage = await sensor.Read();
        }
    }
}