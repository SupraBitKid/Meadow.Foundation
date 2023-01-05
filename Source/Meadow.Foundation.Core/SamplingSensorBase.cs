﻿using Meadow.Peripherals.Sensors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Foundation
{
    /// <summary>
    /// Base class for sensors and other updating classes that want to support
    /// having their updates consumed by observers that can optionally use filters
    /// </summary>
    /// <typeparam name="UNIT"></typeparam>
    public abstract class SamplingSensorBase<UNIT> : ObservableBase<UNIT>, ISensor<UNIT>
        where UNIT : struct
    {
        /// <summary>
        /// Lock for sampling
        /// </summary>
        protected object samplingLock = new object();

        /// <summary>
        /// Event handler for updated values
        /// </summary>
        public event EventHandler<IChangeResult<UNIT>> Updated = delegate { };

        /// <summary>
        /// Sampling cancellation token source
        /// </summary>
        protected CancellationTokenSource? SamplingTokenSource { get; set; }

        /// <summary>
        /// The last read conditions
        /// </summary>
        public UNIT Conditions { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the sensor is currently in a sampling
        /// loop. Call StartSampling() to spin up the sampling process.
        /// </summary>
        /// <value><c>true</c> if sampling; otherwise, <c>false</c>.</value>
        public bool IsSampling { get; protected set; } = false;

        /// <summary>
        /// A `TimeSpan` that specifies how long to
        /// wait between readings. This value influences how often `*Updated`
        /// events are raised and `IObservable` consumers are notified.
        /// </summary>
        public virtual TimeSpan UpdateInterval { get; protected set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Read value from sensor
        /// </summary>
        /// <returns>unitized value</returns>
        protected abstract Task<UNIT> ReadSensor();

        /// <summary>
        /// Notify observers
        /// </summary>
        /// <param name="changeResult">provides new and old values</param>
        protected virtual void RaiseEventsAndNotify(IChangeResult<UNIT> changeResult)
        {
            Updated.Invoke(this, changeResult);
            NotifyObservers(changeResult);
        }

        /// <summary>
        /// Convenience method to get the current sensor readings. For frequent reads, use
        /// StartSampling() and StopSampling() in conjunction with the SampleBuffer.
        /// </summary>
        public virtual async Task<UNIT> Read()
        {
            Conditions = await ReadSensor();
            return Conditions;
        }
    }
}