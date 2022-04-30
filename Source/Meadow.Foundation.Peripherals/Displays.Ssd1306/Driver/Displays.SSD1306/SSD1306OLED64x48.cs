using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meadow.Hardware;

namespace Meadow.Foundation.Displays {
	class SSD1306OLED64x48 : SSD1306 {

		public override uint Width => 128; //?

		public override uint Height => 64; //?

		public SSD1306OLED64x48( II2cBus i2cBus, byte address = 0x3c )
			: base( i2cBus, address ) {
			this.InitSSD1306();
		}

		public SSD1306OLED64x48( IIODevice device, ISpiBus spiBus, IPin chipSelectPin, IPin dcPin, IPin resetPin )
			: base( device, spiBus, chipSelectPin, dcPin, resetPin ) {
			this.InitSSD1306();
		}

		protected override byte[] SetupSequence => SSD1306OLED64x48.SetupSequence_oled64x48;

		/// <summary>
		///     Sequence of bytes that should be sent to a 64x48 OLED display to setup the device.
		/// </summary>
		private static readonly byte[] SetupSequence_oled64x48 =
		{
			0xae, 0xd5, 0x80, 0xa8, 0x3f, 0xd3, 0x00, 0x40 | 0x0, 0x8d, 0x14, 0x20, 0x00, 0xa0 | 0x1, 0xc8,
			0xda, 0x12, 0x81, 0xcf, 0xd9, 0xf1, 0xdb, 0x40, 0xa4, 0xa6, 0xaf
		};


		public override void DrawPixel( int x, int y, bool colored ) {
			if( ( x >= 64 ) || ( y >= 48 ) ) {
				if( !IgnoreOutOfBoundsPixels ) {
					throw new ArgumentException( "DisplayPixel: co-ordinates out of bounds" );
				}
				//  pixels to be thrown away if out of bounds of the display
				return;
			}

			//offsets for landscape
			x += 32;
			y += 16;

			var index = ( int )( ( y / 8 * this.Width ) + x );

			if( colored ) {
				this._backingStore.Buffer.Span[ index ] |= ( byte )( 1 << ( y % 8 ) );
			}
			else {
				this._backingStore.Buffer.Span[ index ] &= ( byte )~( byte )( 1 << ( y % 8 ) );
			}
		}
	}
}
