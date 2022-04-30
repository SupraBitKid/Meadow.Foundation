using System;
using System.Collections.Generic;
using System.Linq;
using Meadow.Foundation.Bitmap;

namespace Meadow.Foundation.Font {
	public abstract class ProportionalFontBase {

		public abstract IEnumerable<OneBppBitmap> GetCharactersFromString( string text );

		public virtual OneBppBitmap GetBitmapFromString( string text ) {
			var bitmaps = this.GetCharactersFromString( text );
			var width = this.GetWidthFromString( text );

			if( width == 0 )
				return null;

			var result = OneBppBitmap.FromBitmap( width, this.Height, bitmaps.First() );
			
			uint xIndex = 0;

			foreach( var bitmap in bitmaps ) {
				result.MergeInto( xIndex, 0, bitmap, OneBppBitmap.MergeMode.Or );
				xIndex = bitmap.Width;
			}

			return result;
		}

		public abstract uint GetWidthFromString( string text );

		public virtual uint Height { get; protected set; }

		public virtual uint Spacing { get; protected set; }
	}
}
