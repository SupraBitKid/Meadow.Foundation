using System;
using Meadow.Foundation.Bitmap;
using Meadow.Hardware;

namespace Meadow.Foundation.Displays
{
    /// <summary>
    ///     Provide an interface to the SSD1306 family of OLED displays.
    /// </summary>
    public abstract class SSD1306 : DisplayBase1Bpp {
        #region Enums

        /// <summary>
        ///     Allow the programmer to set the scroll direction.
        /// </summary>
        public enum ScrollDirection
        {
            /// <summary>
            ///     Scroll the display to the left.
            /// </summary>
            Left,
            /// <summary>
            ///     Scroll the display to the right.
            /// </summary>
            Right,
            /// <summary>
            ///     Scroll the display from the bottom left and vertically.
            /// </summary>
            RightAndVertical,
            /// <summary>
            ///     Scroll the display from the bottom right and vertically.
            /// </summary>
            LeftAndVertical
        }

        public enum ConnectionType
        {
            SPI,
            I2C,
        }

        #endregion Enums

        #region Member variables / fields

        public override DisplayColorMode ColorMode => DisplayColorMode.Format1bpp;

        /// <summary>
        ///     SSD1306 SPI display
        /// </summary>
        protected ISpiPeripheral _spiPeripheral;

        protected IDigitalOutputPort dataCommandPort;
        protected IDigitalOutputPort resetPort;
        protected IDigitalOutputPort chipSelectPort;
        protected ConnectionType connectionType;
        protected const bool Data = true;
        protected const bool Command = false;

		/// <summary>
		///     SSD1306 I2C display
		/// </summary>
		protected readonly II2cPeripheral _I2cPeripheral;

		/// <summary>
		///     Buffer holding the pixels in the display.
		/// </summary>
		protected OneBppBitmap _backingStore;

		/// <summary>
		///     Sequence of command bytes that must be sent to the display before
		///     the Show method can send the data buffer.
		/// </summary>
		protected byte[] _showPreamble;

		protected abstract byte[] SetupSequence { get; }

		#endregion Member variables / fields

		#region Properties

		/// <summary>
		///     Backing variable for the InvertDisplay property.
		/// </summary>
		private bool _invertDisplay;

        /// <summary>
        ///     Invert the entire display (true) or return to normal mode (false).
        /// </summary>
        /// <remarks>
        ///     See section 10.1.10 in the datasheet.
        /// </remarks>
        public bool InvertDisplay
        {
            get { return _invertDisplay; }
            set
            {
                _invertDisplay = value;
                SendCommand((byte)(value ? 0xa7 : 0xa6));
            }
        }

        /// <summary>
        ///     Backing variable for the Contrast property.
        /// </summary>
        private byte _contrast;

        /// <summary>
        ///     Get / Set the contrast of the display.
        /// </summary>
        public byte Contrast
        {
            get { return _contrast; }

            set
            {
                _contrast = value;
                SendCommands(new byte[] { 0x81, _contrast });
            }
        }

        /// <summary>
        ///     Put the display to sleep (turns the display off).
        /// </summary>
        public bool Sleep
        {
            get { return(_sleep); }
            set
            {
                _sleep = value;
                SendCommand((byte)(_sleep ? 0xae : 0xaf));
            }
        }

        /// <summary>
        ///     Backing variable for the Sleep property.
        /// </summary>
        private bool _sleep;

        #endregion Properties

        #region Constructors

        /// <summary>
        ///     Default constructor is private to prevent it being used.
        /// </summary>
        private SSD1306() { }

        /// <summary>
        ///     Create a new SSD1306 object using the default parameters for
        /// </summary>
        /// <remarks>
        ///     Note that by default, any pixels out of bounds will throw and exception.
        ///     This can be changed by setting the <seealso cref="IgnoreOutOfBoundsPixels" />
        ///     property to true.
        /// </remarks>
        ///
        protected SSD1306(IIODevice device, ISpiBus spiBus, IPin chipSelectPin, IPin dcPin, IPin resetPin )
        {
            dataCommandPort = device.CreateDigitalOutputPort(dcPin, false);
            resetPort = device.CreateDigitalOutputPort(resetPin, true);
            chipSelectPort = device.CreateDigitalOutputPort(chipSelectPin, false);

            _spiPeripheral = new SpiPeripheral(spiBus, chipSelectPort);

            connectionType = ConnectionType.SPI;
        }

        /// <summary>
        ///     Create a new SSD1306 object using the default parameters for
        /// </summary>
        /// <remarks>
        ///     Note that by default, any pixels out of bounds will throw and exception.
        ///     This can be changed by setting the <seealso cref="IgnoreOutOfBoundsPixels" />
        ///     property to true.
        /// </remarks>
        /// <param name="address">Address of the bus on the I2C display.</param>
        protected SSD1306(II2cBus i2cBus, byte address = 0x3c )
        {
            _I2cPeripheral = new I2cPeripheral(i2cBus, address);

            connectionType = ConnectionType.I2C;
        }

        protected virtual void InitSSD1306()
        {
			SendCommands( this.SetupSequence );

			this._backingStore = OneBppBitmap.FromBuffer( this.Width
				, this.Height
				, new byte[ this.Width * this.Height / 8 ]
				, OneBppBitmap.ByteDirectionSpec.TopToBottomLsbFirst );

			_showPreamble = new byte[] { 0x21, 0x00, ( byte )( this.Width - 1 ), 0x22, 0x00, ( byte )( ( this.Height / 8 ) - 1 ) };

            IgnoreOutOfBoundsPixels = false;

            //
            //  Finally, put the display into a known state.
            //
            InvertDisplay = false;
            Sleep = false;
            Contrast = 0xff;
            StopScrolling();
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        ///     Send a command to the display.
        /// </summary>
        /// <param name="command">Command byte to send to the display.</param>
        protected virtual void SendCommand(byte command)
        {
            if (connectionType == ConnectionType.SPI)
            {
                dataCommandPort.State = Command;
                _spiPeripheral.WriteByte(command);
            }
            else
            {
                _I2cPeripheral.WriteBytes(new byte[] { 0x00, command });
            }
        }

		/// <summary>
		///     Send a sequence of commands to the display.
		/// </summary>
		/// <param name="commands">List of commands to send.</param>
		protected virtual void SendCommands(byte[] commands)
        {
            var data = new byte[commands.Length + 1];
            data[0] = 0x00;
            Array.Copy(commands, 0, data, 1, commands.Length);

            if (connectionType == ConnectionType.SPI)
            {
                dataCommandPort.State = Command;
                _spiPeripheral.WriteBytes(commands);
            }
            else
            {
                _I2cPeripheral.WriteBytes(data);
            }
        }

        /// <summary>
        ///     Send the internal pixel buffer to display.
        /// </summary>
        public override void Show()
        {
            SendCommands(_showPreamble);
            //
            //  Send the buffer page by page.
            //
            const int PAGE_SIZE = 16;
            var data = new byte[PAGE_SIZE + 1];
            data[0] = 0x40;

            if (connectionType == ConnectionType.SPI)
            {
                dataCommandPort.State = Data;
                _spiPeripheral.WriteBytes( this._backingStore.Buffer.ToArray() );
            }
            else
            {
				var buffer = this._backingStore.Buffer.ToArray();

                for (ushort index = 0; index < buffer.Length; index += PAGE_SIZE)
                {
                    Array.Copy( buffer, index, data, 1, PAGE_SIZE );
                    SendCommand(0x40);
                    _I2cPeripheral.WriteBytes(data);
                }
            }
        }

        /// <summary>
        ///     Clear the display buffer.
        /// </summary>
        /// <param name="updateDisplay">Immediately update the display when true.</param>
        public override void Clear(bool updateDisplay = false)
        {
			this._backingStore.Buffer.Span.Fill( 0 );

            if (updateDisplay)
                Show();
        }

        public override void DrawPixel(int x, int y, Color color)
        {
            var colored = (color == Color.Black) ? false : true;

            DrawPixel(x, y, colored);
        }

        /// <summary>
        ///     Coordinates start with index 0
        /// </summary>
        /// <param name="x">Abscissa of the pixel to the set / reset.</param>
        /// <param name="y">Ordinate of the pixel to the set / reset.</param>
        /// <param name="colored">True = turn on pixel, false = turn off pixel</param>
        public override void DrawPixel(int x, int y, bool colored)
        {
            if ((x >= this.Width) || (y >= this.Height))
            {
                if (!IgnoreOutOfBoundsPixels)
                {
                    throw new ArgumentException("DisplayPixel: co-ordinates out of bounds");
                }
                //  pixels to be thrown away if out of bounds of the display
                return;
            }

            int index = ( int )( ( y / 8 * this.Width ) + x );

            if (colored)
            {
				this._backingStore.Buffer.Span[ index ] |= (byte) (1 << (y % 8));
            }
            else
            {
				this._backingStore.Buffer.Span[ index ] &= ( byte )( ~( byte )( 1 << ( y % 8 ) ) );
            }
        }

		public override void DrawPixel( int x, int y ) => this.DrawPixel( x, y, true );

		public override void SetPenColor( Color pen ) => throw new NotImplementedException();

		/// <summary>
		///     Copy a bitmap to the display.
		/// </summary>
		/// <remarks>
		///     Currently, this method only supports copying the bitmap over the contents
		///     of the display buffer.
		/// </remarks>
		/// <param name="x">Abscissa of the top left corner of the bitmap.</param>
		/// <param name="y">Ordinate of the top left corner of the bitmap.</param>
		/// <param name="width">Width of the bitmap in bytes.</param>
		/// <param name="height">Height of the bitmap in bytes.</param>
		/// <param name="bitmap">Bitmap to transfer</param>
		/// <param name="bitmapMode">How should the bitmap be transferred to the display?</param>
		public override void DrawBitmap(int x, int y, OneBppBitmap bitmap, BitmapMode bitmapMode)
        {
			var mergeMode = OneBppBitmap.MergeMode.Copy;
			
			switch( bitmapMode ) {
				case BitmapMode.And:
					mergeMode = OneBppBitmap.MergeMode.And;
					break;
				case BitmapMode.Or:
					mergeMode = OneBppBitmap.MergeMode.Or;
					break;
				case BitmapMode.XOr:
					mergeMode = OneBppBitmap.MergeMode.XOr;
					break;
				case BitmapMode.Copy:
					mergeMode = OneBppBitmap.MergeMode.Copy;
					break;
			}

			this._backingStore.MergeInto( ( uint )x, ( uint )y, bitmap, mergeMode );
		}

		/// <summary>
		///     Start the display scrollling in the specified direction.
		/// </summary>
		/// <param name="direction">Direction that the display should scroll.</param>
		public virtual void StartScrolling(ScrollDirection direction)
        {
            StartScrolling(direction, 0x00, 0xff);
        }

        /// <summary>
        ///     Start the display scrolling.
        /// </summary>
        /// <remarks>
        ///     In most cases setting startPage to 0x00 and endPage to 0xff will achieve an
        ///     acceptable scrolling effect.
        /// </remarks>
        /// <param name="direction">Direction that the display should scroll.</param>
        /// <param name="startPage">Start page for the scroll.</param>
        /// <param name="endPage">End oage for the scroll.</param>
        public virtual void StartScrolling(ScrollDirection direction, byte startPage, byte endPage)
        {
            StopScrolling();
            byte[] commands;
            if ((direction == ScrollDirection.Left) || (direction == ScrollDirection.Right))
            {
                commands = new byte[] { 0x26, 0x00, startPage, 0x00, endPage, 0x00, 0xff, 0x2f };
                if (direction == ScrollDirection.Left)
                {
                    commands[0] = 0x27;
                }
            }
            else
            {
                byte scrollDirection;
                if (direction == ScrollDirection.LeftAndVertical)
                {
                    scrollDirection = 0x2a;
                }
                else
                {
                    scrollDirection = 0x29;
                }
                commands = new byte[]
                    { 0xa3, 0x00, (byte) this.Height, scrollDirection, 0x00, startPage, 0x00, endPage, 0x01, 0x2f };
            }
            SendCommands(commands);
        }

        /// <summary>
        ///     Turn off scrolling.
        /// </summary>
        /// <remarks>
        ///     Datasheet states that scrolling must be turned off before changing the
        ///     scroll direction in order to prevent RAM corruption.
        /// </remarks>
        public virtual void StopScrolling()
        {
            SendCommand(0x2e);
        }

		#endregion Methods
	}
}