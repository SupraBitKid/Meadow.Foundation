﻿using Meadow.Hardware;
using Meadow.Peripherals.Sensors.Motion;
using Meadow.Units;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Meadow.Foundation.Sensors.Motion
{
    // TODO:
    //  * no one-off `Read()` method.
    //  * needs to inherit from `FilterableChangeObservableI2CPeripheral`, currently using a bunch of allocating methods.

    public class Mpu6050 :
        FilterableChangeObservableBase<(Acceleration3D?, AngularAcceleration3D?)>,
        IAccelerometer, IAngularAccelerometer, IDisposable
    {
        /// <summary>
        ///     Valid addresses for the sensor.
        /// </summary>
        public enum Addresses : byte
        {
            Address0 = 0x68,
            Address1 = 0x69,
            Default = Address0
        }

        private enum Register : byte
        {
            Config = 0x1a,
            GyroConfig = 0x1b,
            AccelConfig = 0x1c,
            InterruptConfig = 0x37,
            InterruptEnable = 0x38,
            InterruptStatus = 0x3a,
            PowerManagement = 0x6b,
            AccelerometerX = 0x3b,
            AccelerometerY = 0x3d,
            AccelerometerZ = 0x3f,
            Temperature = 0x41,
            GyroX = 0x43,
            GyroY = 0x45,
            GyroZ = 0x47
        }

        public event EventHandler<IChangeResult<(Acceleration3D? Acceleration3D, AngularAcceleration3D? AngularAcceleration3D)>> Updated;
        public event EventHandler<IChangeResult<Acceleration3D>> Acceleration3DUpdated;
        public event EventHandler<IChangeResult<AngularAcceleration3D>> AngularAcceleration3DUpdated;

        /*
        /// <summary>
        ///     Acceleration along the X-axis.
        /// </summary>
        /// <remarks>
        ///     This property will only contain valid data after a call to Read or after
        ///     an interrupt has been generated.
        /// </remarks>
        public float AccelerationX {
            get {
                if (IsSampling) { return Conditions.XAcceleration.Value; } else { return ReadRegisterInt16(Register.AccelerometerX) * (1 << AccelerometerScale) / AccelScaleBase; }
            }
        }

        /// <summary>
        ///     Acceleration along the Y-axis.
        /// </summary>
        /// <remarks>
        ///     This property will only contain valid data after a call to Read or after
        ///     an interrupt has been generated.
        /// </remarks>
        public float AccelerationY {
            get {
                if (IsSampling) { return Conditions.YAcceleration.Value; } else { return ReadRegisterInt16(Register.AccelerometerY) * (1 << AccelerometerScale) / AccelScaleBase; }
            }
        }

        /// <summary>
        ///     Acceleration along the Z-axis.
        /// </summary>
        /// <remarks>
        ///     This property will only contain valid data after a call to Read or after
        ///     an interrupt has been generated.
        /// </remarks>
        public float AccelerationZ {
            get {
                if (IsSampling) { return Conditions.ZAcceleration.Value; } else { return ReadRegisterInt16(Register.AccelerometerZ) * (1 << AccelerometerScale) / AccelScaleBase; }
            }
        }*/

        public (Acceleration3D? Acceleration3d, AngularAcceleration3D? AngularAcceleration3d) Conditions;

        public Acceleration3D? Acceleration3D { get; protected set; }
        public AngularAcceleration3D? AngularAcceleration3D { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the analog input port is currently
        /// sampling the ADC. Call StartSampling() to spin up the sampling process.
        /// </summary>
        /// <value><c>true</c> if sampling; otherwise, <c>false</c>.</value>
        public bool IsSampling { get; protected set; } = false;

        private const float GyroScaleBase = 131f;
        private const float AccelScaleBase = 16384f;

        // internal thread lock
        private object _lock = new object();
        private CancellationTokenSource SamplingTokenSource;
               
        Units.Temperature _temperature;

        private int GyroScale { get; set; }
        private int AccelerometerScale { get; set; }
        private II2cBus Device { get; set; }

        // [Bryan 2021.05.16] removed: this is an old pattern, and the filterable
        // observerable handles it. also, these settings weren't used anywhere.
        //public float GyroChangeThreshold { get; set; }
        //public float AccelerationChangeThreshold { get; set; }
        public byte Address { get; private set; }

        public Mpu6050(II2cBus bus, byte address = 0x68)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));

            Device = bus;

            Initialize(address);
        }

        public Mpu6050(II2cBus bus, Addresses address)
            : this(bus, (byte)address)
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) {
                StopUpdating();
            }
        }

        /// <summary>
        /// Dispose managed resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
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
            lock (_lock) {
                if (IsSampling) { return; }

                // state muh-cheen
                IsSampling = true;

                SamplingTokenSource = new CancellationTokenSource();
                CancellationToken ct = SamplingTokenSource.Token;

                (Acceleration3D?, AngularAcceleration3D?) oldConditions;
                ChangeResult<(Acceleration3D?, AngularAcceleration3D?)> result;
                Task.Factory.StartNew(async () => {
                    while (true) {
                        if (ct.IsCancellationRequested) {
                            // do task clean up here
                            observers.ForEach(x => x.OnCompleted());
                            break;
                        }
                        // capture history
                        oldConditions = (Conditions.Acceleration3d, Conditions.AngularAcceleration3d);

                        // read
                        Update();

                        // build a new result with the old and new conditions
                        result = new ChangeResult<(Acceleration3D?, AngularAcceleration3D?)>(oldConditions, Conditions);

                        // let everyone know
                        RaiseChangedAndNotify(result);

                        // sleep for the appropriate interval
                        await Task.Delay(standbyDuration);
                    }
                }, SamplingTokenSource.Token);
            }
        }

        protected void RaiseChangedAndNotify(IChangeResult<(Acceleration3D? Acceleration3D, AngularAcceleration3D? AngularAcceleration3D)> changeResult)
        {
            Updated?.Invoke(this, changeResult);
            if (changeResult.New.AngularAcceleration3D is { } angular) {
                AngularAcceleration3DUpdated?.Invoke(this, new ChangeResult<Units.AngularAcceleration3D>(angular, changeResult.Old?.AngularAcceleration3D));
            }
            if (changeResult.New.Acceleration3D is { } accel) {
                Acceleration3DUpdated?.Invoke(this, new ChangeResult<Units.Acceleration3D>(accel, changeResult.Old?.Acceleration3D));
            }
            base.NotifyObservers(changeResult);
        }

        ///// <summary>
        ///// Stops sampling the temperature.
        ///// </summary>
        public void StopUpdating()
        {
            lock (_lock) {
                if (!IsSampling) { return; }

                SamplingTokenSource?.Cancel();

                // state muh-cheen
                IsSampling = false;
            }
        }

        private void Initialize(byte address)
        {
            switch (address) {
                case 0x68:
                case 0x69:
                    // valid;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("MPU6050 device address must be either 0x68 or 0x69");
            }

            Address = address;

            Wake();
        }

        /// <summary>
        /// Temperature of sensor
        /// </summary>
        public Units.Temperature Temperature {
            get {
                if (IsSampling) 
                {
                    return _temperature;
                }
                return new Units.Temperature(ReadRegisterInt16(Register.Temperature) * (1 << GyroScale) / GyroScaleBase, Units.Temperature.UnitType.Celsius);
            }
        }

        public void Wake()
        {
            Device.WriteData(Address, (byte)Register.PowerManagement, 0x00);

            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            // read all 3 config bytes
            var data = Device.WriteReadData(Address, 3, (byte)Register.Config);

            GyroScale = (data[1] & 0b00011000) >> 3;
            AccelerometerScale = (data[2] & 0b00011000) >> 3;
        }

        private short ReadRegisterInt16(Register register)
        {
            return ReadRegisterInt16((byte)register);
        }

        private short ReadRegisterInt16(byte register)
        {
            var data = Device.WriteReadData(Address, 2, register);
            unchecked {
                return (short)(data[0] << 8 | data[1]); ;
            }
        }

        private void Update()
        {
            lock (_lock) 
            {
                // we'll just read 14 bytes (7 registers), starting at 0x3b
                var data = Device.WriteReadData(Address, 14, (byte)Register.AccelerometerX);

                var a_scale = (1 << AccelerometerScale) / AccelScaleBase;
                var g_scale = (1 << GyroScale) / GyroScaleBase;
                Acceleration3D newAccel = new Acceleration3D(
                    new Acceleration(ScaleAndOffset(data, 0, a_scale)),
                    new Acceleration(ScaleAndOffset(data, 2, a_scale)),
                    new Acceleration(ScaleAndOffset(data, 4, a_scale))
                    );
                Conditions.Acceleration3d = newAccel;
                _temperature = new Units.Temperature(ScaleAndOffset(data, 6, 1 / 340f, 36.53f), Units.Temperature.UnitType.Celsius);
                AngularAcceleration3D angularAccel = new AngularAcceleration3D(
                    new AngularAcceleration(ScaleAndOffset(data, 8, g_scale)),
                    new AngularAcceleration(ScaleAndOffset(data, 10, g_scale)),
                    new AngularAcceleration(ScaleAndOffset(data, 12, g_scale))
                    );
                Conditions.AngularAcceleration3d = angularAccel;
            }
        }

        private float ScaleAndOffset(Span<byte> data, int index, float scale, float offset = 0)
        {
            // convert to a signed number
            unchecked {
                var s = (short)(data[index] << 8 | data[index + 1]);
                return (s * scale) + offset;
            }
        }

        /// <summary>
        /// Creates a `FilterableChangeObserver` that has a handler and a filter.
        /// </summary>
        /// <param name="handler">The action that is invoked when the filter is satisifed.</param>
        /// <param name="filter">An optional filter that determines whether or not the
        /// consumer should be notified.</param>
        /// <returns></returns>
        /// <returns></returns>
        // Implementor Notes:
        //  This is a convenience method that provides named tuple elements. It's not strictly
        //  necessary, as the `FilterableChangeObservableBase` class provides a default implementation,
        //  but if you use it, then the parameters are named `Item1`, `Item2`, etc. instead of
        //  `Temperature`, `Pressure`, etc.
        public static new
            FilterableChangeObserver<(Acceleration3D?, AngularAcceleration3D?)>
            CreateObserver(
                Action<IChangeResult<(Acceleration3D? Acceleration3D, AngularAcceleration3D? AngularAcceleration3D)>> handler,
                Predicate<IChangeResult<(Acceleration3D? Acceleration3D, AngularAcceleration3D? AngularAcceleration3D)>>? filter = null
            )
        {
            return new FilterableChangeObserver<(Acceleration3D?, AngularAcceleration3D?)>(
                handler: handler, filter: filter
                );
        }

    }
}
