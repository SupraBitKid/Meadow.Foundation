using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meadow.Foundation.Bitmap;

namespace Meadow.Foundation.Displays {
	public abstract class DisplayBase1Bpp : DisplayBase {

		public override void DrawBitmap( int x, int y, OneBppBitmap bitmap, Color color ) => throw new NotImplementedException();

		public override void DrawBitmap( int x, int y, int width, int height, byte[] bitmap, BitmapMode bitmapMode ) => throw new NotImplementedException();

		public override void DrawBitmap( int x, int y, int width, int height, byte[] bitmap, Color color ) => throw new NotImplementedException();

		/// <summary>
		/// Copy a 1bpp bitmap to the display.
		/// </summary>
		/// <param name="x">horizontal top left corner of the bitmap.</param>
		/// <param name="y">vertical top left corner of the bitmap.</param>
		/// <param name="bitmap">Bitmap to transfer</param>
		/// <param name="bitmapMode">How should the bitmap be transferred to the display?</param>
		public abstract void DrawBitmap( int x, int y, OneBppBitmap bitmap, BitmapMode bitmapMode );
	}
}
