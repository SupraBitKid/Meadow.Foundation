﻿using Meadow.Devices;
using Meadow.Hardware;
using Meadow.Units;
using System;

namespace Meadow.Foundation.Leds
{
    /// <summary>
    /// Represents APA102/Dotstar Led(s).
    /// </summary>
    /// <remarks>Based on logic from https://github.com/adafruit/Adafruit_CircuitPython_DotStar/blob/master/adafruit_dotstar.py </remarks>
    public partial class Apa102 : IApa102
    {
        /// <summary>
        /// Default SPI bus speed
        /// </summary>
        public static Frequency DefaultSpiBusSpeed = new Frequency(6000, Frequency.UnitType.Kilohertz);

        /// <summary>
        /// SpiPeripheral object
        /// </summary>
        protected ISpiPeripheral spiPeripheral;

        const short StartHeaderSize = 4;
        const byte LedStart = 0b11100000;

        static readonly byte[] RGB = { 0, 1, 2 };
        static readonly byte[] RBG = { 0, 2, 1 };
        static readonly byte[] GRB = { 1, 0, 2 };
        static readonly byte[] GBR = { 1, 2, 0 };
        static readonly byte[] BRG = { 2, 0, 1 };
        static readonly byte[] BGR = { 2, 1, 0 };

        readonly int numberOfLeds;
        readonly int endHeaderSize;
        readonly byte[] buffer;
        readonly int endHeaderIndex;
        readonly byte[] pixelOrder;

        /// <summary>
        /// Total number of leds 
        /// </summary>
        public int NumberOfLeds => numberOfLeds;

        /// <summary>
        /// Brightness 
        /// </summary>
        public float Brightness
        {
            get => brightness;
            set
            {
                if (value < 0) { brightness = 0; }
                else if (value > 1f) { brightness = 1f; }
                else { brightness = value; }
            }
        }
        float brightness;

        /// <summary>
        /// Creates a new APA102 object
        /// </summary>
        /// <param name="device">IMeadowDevice</param>
        /// <param name="spiBus">SPI bus</param>
        /// <param name="chipSelectPin">Chip select pin</param>
        /// <param name="numberOfLeds">Number of leds</param>
        /// <param name="pixelOrder">Pixel color order</param>
        public Apa102(IMeadowDevice device, ISpiBus spiBus, IPin chipSelectPin, int numberOfLeds, PixelOrder pixelOrder = PixelOrder.BGR)
        : this(spiBus, numberOfLeds, pixelOrder, device.CreateDigitalOutputPort(chipSelectPin))
        {
        }

        /// <summary>
        /// Creates a new APA102 object
        /// </summary>
        /// <param name="spiBus">SPI bus</param>
        /// <param name="numberOfLeds">Number of leds</param>
        /// <param name="pixelOrder">Pixel color order</param>
        /// <param name="chipSelectPort">SPI chip select port (optional)</param>
        public Apa102(ISpiBus spiBus, int numberOfLeds, PixelOrder pixelOrder = PixelOrder.BGR, IDigitalOutputPort chipSelectPort = null)
        {
            spiPeripheral = new SpiPeripheral(spiBus, chipSelectPort);
            this.numberOfLeds = numberOfLeds;
            endHeaderSize = this.numberOfLeds / 16;
            Brightness = 1.0f;

            if (this.numberOfLeds % 16 != 0)
            {
                endHeaderSize += 1;
            }

            buffer = new byte[this.numberOfLeds * 4 + StartHeaderSize + endHeaderSize];
            endHeaderIndex = buffer.Length - endHeaderSize;

            switch (pixelOrder)
            {
                case PixelOrder.RGB:
                    this.pixelOrder = RGB;
                    break;
                case PixelOrder.RBG:
                    this.pixelOrder = RBG;
                    break;
                case PixelOrder.GRB:
                    this.pixelOrder = GRB;
                    break;
                case PixelOrder.GBR:
                    this.pixelOrder = GBR;
                    break;
                case PixelOrder.BRG:
                    this.pixelOrder = BRG;
                    break;
                case PixelOrder.BGR:
                    this.pixelOrder = BGR;
                    break;
            }

            for (int i = 0; i < StartHeaderSize; i++)
            {
                buffer[i] = 0x00;
            }

            for (int i = StartHeaderSize; i < endHeaderIndex; i += 4)
            {
                buffer[i] = 0xFF;
            }

            for (int i = endHeaderIndex; i < buffer.Length; i++)
            {
                buffer[i] = 0xFF;
            }
        }

        /// <summary>
        /// Set the color of the specified LED
        /// </summary>
        /// <param name="index">Index of the LED to change</param>
        /// <param name="color">The color</param>
        public virtual void SetLed(int index, Color color)
        {
            SetLed(index, color, Brightness);
        }

        /// <summary>
        /// Set the color of the specified LED
        /// </summary>
        /// <param name="index">Index of the LED to change</param>
        /// <param name="color">The color</param>
        /// <param name="brightness">The brighrness 0.0 - 1.0f</param>
        public virtual void SetLed(int index, Color color, float brightness = 1f)
        {
            SetLed(index, new byte[] { color.R, color.G, color.B }, brightness);
        }

        /// <summary>
        /// Set the color of the specified LED
        /// </summary>
        /// <param name="index">Index of the LED to change</param>
        /// <param name="rgb">Byte array representing the color RGB values. byte[0] = Red, byte[1] = Green, byte[2] = Blue</param>
        public virtual void SetLed(int index, byte[] rgb)
        {
            SetLed(index, rgb, Brightness);
        }

        /// <summary>
        /// Set the color of the specified LED
        /// </summary>
        /// <param name="index">Index of the LED to change</param>
        /// <param name="rgb">Byte array representing the color RGB values. byte[0] = Red, byte[1] = Green, byte[2] = Blue</param>
        /// <param name="brightness">The brighrness 0.0 - 1.0f</param>
        public virtual void SetLed(int index, byte[] rgb, float brightness = 1f)
        {
            if (index > numberOfLeds)
            {
                throw new ArgumentOutOfRangeException("Index must be less than the number of leds specified");
            }

            // clamp
            if (brightness > 1) { brightness = 1; }
            if (brightness < 0) { brightness = 0; }

            var offset = index * 4 + StartHeaderSize;

            byte brightnessByte = (byte)(32 - (32 - (int)(brightness * 31)) & 0b00011111);
            buffer[offset] = (byte)(brightnessByte | LedStart);
            buffer[offset + 1] = rgb[pixelOrder[0]];
            buffer[offset + 2] = rgb[pixelOrder[1]];
            buffer[offset + 3] = rgb[pixelOrder[2]];
        }

        /// <summary>
        /// Turn off all the Leds
        /// </summary>
        public void Clear(bool update = false)
        {
            byte[] off = { 0, 0, 0 };

            for (int i = 0; i < NumberOfLeds; i++)
            {
                SetLed(i, off);
            }
        }

        /// <summary>
        /// Transmit the changes to the LEDs 
        /// </summary>
        public void Show()
        {
            spiPeripheral.Write(buffer);
        }

        /// <summary>
        /// Update APA102 with data in display buffer
        /// </summary>
        /// <param name="left">left</param>
        /// <param name="top">top</param>
        /// <param name="right">right</param>
        /// <param name="bottom">bottom</param>
        public void Show(int left, int top, int right, int bottom)
        {
            Show();
        }
    }
}