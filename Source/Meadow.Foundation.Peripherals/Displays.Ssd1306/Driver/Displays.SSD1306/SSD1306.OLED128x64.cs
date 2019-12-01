using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meadow.Hardware;

namespace Meadow.Foundation.Displays {
	public class SSD1306OLED128x64 : SSD1306 {

		public override uint Width => 128; //?

		public override uint Height => 64; //?

		public SSD1306OLED128x64( II2cBus i2cBus, byte address = 0x3c )
			: base( i2cBus, address ) {
			this.InitSSD1306();
		}

		public SSD1306OLED128x64( IIODevice device, ISpiBus spiBus, IPin chipSelectPin, IPin dcPin, IPin resetPin )
			: base( device, spiBus, chipSelectPin, dcPin, resetPin ) {
			this.InitSSD1306();
		}

		protected override byte[] SetupSequence => SSD1306OLED128x64.SetupSequence_oled128x64;
		/// <summary>
		///     Sequence of bytes that should be sent to a 128x64 OLED display to setup the device.
		/// </summary>
		private static readonly byte[] SetupSequence_oled128x64 =
		{
			0xae, 0xd5, 0x80, 0xa8, 0x3f, 0xd3, 0x00, 0x40 | 0x0, 0x8d, 0x14, 0x20, 0x00, 0xa0 | 0x1, 0xc8,
			0xda, 0x12, 0x81, 0xcf, 0xd9, 0xf1, 0xdb, 0x40, 0xa4, 0xa6, 0xaf
		};

	}
}
