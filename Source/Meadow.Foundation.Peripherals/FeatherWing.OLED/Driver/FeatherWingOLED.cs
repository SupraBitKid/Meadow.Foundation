using System;
using Meadow.Foundation.Sensors.Buttons;
using Meadow.Hardware;
using Meadow.Peripherals.Sensors.Buttons;

namespace Meadow.Foundation.Displays {
    public class FeatherWingOLED : SSD1306OLED128x32 {
		public event EventHandler OnButtonA;
		public event EventHandler OnButtonB;
		public event EventHandler OnButtonC;

		public IButton ButtonA { get; private set; }
		public IButton ButtonB { get; private set; }
		public IButton ButtonC { get; private set; }

		protected void SetupButtons( IIODevice device, IPin pinA, IPin pinB, IPin pinC ) {
			this.SetupButtons( device.CreateDigitalInputPort( pinA, InterruptMode.LevelHigh ),
				device.CreateDigitalInputPort( pinB, InterruptMode.LevelHigh ),
				device.CreateDigitalInputPort( pinC, InterruptMode.LevelHigh ) );
		}

		protected void SetupButtons(  IDigitalInputPort portA, IDigitalInputPort portB, IDigitalInputPort portC ) {
			this.ButtonA = new PushButton( portA );
			this.ButtonB = new PushButton( portB );
			this.ButtonC = new PushButton( portC );

			this.ButtonA.PressEnded += ( s, e ) => OnButtonA?.Invoke( s, e );
			this.ButtonB.PressEnded += ( s, e ) => OnButtonB?.Invoke( s, e );
			this.ButtonC.PressEnded += ( s, e ) => OnButtonC?.Invoke( s, e );
		}

		public FeatherWingOLED( IIODevice device, II2cBus i2cBus, IPin buttonPinA, IPin buttonPinB, IPin buttonPinC ) 
				: base( i2cBus, 0x3C ) {
			this.SetupButtons( device, buttonPinA, buttonPinB, buttonPinC );
			this.InitSSD1306();
		}
    }
}
