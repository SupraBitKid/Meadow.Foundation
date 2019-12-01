using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.Foundation.Bitmap {
	public abstract class OneBppBitmap {
		public enum ByteDirectionSpec {
			LeftToRightLsbFirst = 0,
			LeftToRightMsbFirst,
			TopToBottomLsbFirst,
			TopToBottomMsbFirst
		};

		public enum MergeMode {
			Copy = 0,
			And,
			Or,
			XOr
		};

		protected OneBppBitmap( uint width, uint height ) {
			this.Width = width;
			this.Height = height;
		}

		public virtual uint Width { get; protected set; }

		public virtual uint Height { get; protected set; }

		public virtual ByteDirectionSpec ByteDirection { get; protected set; }

		public abstract Memory<byte> Buffer { get; protected set; }

		public static OneBppBitmap FromBuffer( uint width, uint height, Memory<byte> buffer, ByteDirectionSpec byteDirection ) {
			OneBppBitmap result = null;

			switch( byteDirection ) {
				case ByteDirectionSpec.LeftToRightLsbFirst:
				case ByteDirectionSpec.LeftToRightMsbFirst:
					throw new NotImplementedException();

				case ByteDirectionSpec.TopToBottomLsbFirst:
					result = new OneBppBitmapWithPages( width, height, buffer, false );
					result.ByteDirection = byteDirection;
					break;

				case ByteDirectionSpec.TopToBottomMsbFirst:
					result = new OneBppBitmapWithPages( width, height, buffer, true );
					result.ByteDirection = byteDirection;
					break;
			}
			
			return result;
		}

		public static OneBppBitmap FromBitmap( uint width, uint height, OneBppBitmap bitmap ) {
			OneBppBitmap result = null;

			switch( bitmap.ByteDirection ) {
				case ByteDirectionSpec.LeftToRightLsbFirst:
				case ByteDirectionSpec.LeftToRightMsbFirst:
					throw new NotImplementedException();

				case ByteDirectionSpec.TopToBottomLsbFirst:
					result = new OneBppBitmapWithPages( width, height, false );
					result.ByteDirection = bitmap.ByteDirection;
					break;

				case ByteDirectionSpec.TopToBottomMsbFirst:
					result = new OneBppBitmapWithPages( width, height, true );
					result.ByteDirection = bitmap.ByteDirection;
					break;
			}

			return result;
		}

		public virtual void MergeInto( uint x, uint y, OneBppBitmap sourceBitmap, MergeMode mergeMode ) {
			if( sourceBitmap.ByteDirection != this.ByteDirection )
				throw new NotImplementedException( "OrInto with different ByteDirections in not implemented yet" );

		}
	}
}
