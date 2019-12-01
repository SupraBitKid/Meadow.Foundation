using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.Foundation.Bitmap {
	internal class OneBppBitmapWithPages : OneBppBitmap {
		
		public bool MsbTop { get; protected set; }

		public override Memory<byte> Buffer { get; protected set; }

		public uint HeightInBytes { get; protected set; }

		protected OneBppBitmapWithPages( uint width, uint height ) : base( width, height ) {
			this.HeightInBytes = ( uint )( ( height / 8 ) + ( ( height % 8 ) == 0 ? 0 : 1 ) );
		}

		public OneBppBitmapWithPages( uint width, uint height, Memory<byte> buffer, bool msbTop ) 
				: this( width, height ) {

			this.MsbTop = msbTop;
			this.Buffer = buffer;
		}

		public OneBppBitmapWithPages( uint width, uint height, bool msbTop )
				: this( width, height ) {

			this.MsbTop = msbTop;
			this.Buffer = new Memory<byte>( new byte[ this.Width * this.HeightInBytes ] );
		}

		public override void MergeInto( uint x, uint y, OneBppBitmap sourceBitmap, MergeMode mergeMode ) {
			if( sourceBitmap.ByteDirection != ByteDirectionSpec.TopToBottomLsbFirst 
				|| sourceBitmap.ByteDirection != ByteDirectionSpec.TopToBottomMsbFirst )
					throw new NotImplementedException( "MergeInto with different ByteDirections in not implemented yet" );

			var from = sourceBitmap as OneBppBitmapWithPages;
			var fromWidth = x + from.Width < this.Width
				? from.Width
				: from.Width - ( this.Width - ( x + from.Width ) );

			var yPage = y / 8;
			var yBits = y % 8;

			if( yBits != 0 )
				throw new NotImplementedException( "MergeInto with y not on byte boundry not implemented yet" );

			bool flipByte = this.ByteDirection != from.ByteDirection;

			for( int yByte = 0; yByte < from.HeightInBytes; yByte++ ) {
				if( yPage + yByte < this.HeightInBytes ) {
					var destinationMemory = this.Buffer.Slice( ( int )( this.Width * ( yPage + yByte ) ), ( int )this.Width );
					var sourceMemory = from.Buffer.Slice( ( int )( from.Width * yByte ), ( int )fromWidth );

					OneBppBitmapWithPages.MergeInto( sourceMemory, destinationMemory, mergeMode );
				}
			}
		}

		static protected void MergeInto( Memory<byte> sourceMemory, Memory<byte> destinationMemory, MergeMode mergeMode ) {
			if( sourceMemory.Length > destinationMemory.Length )
				throw new ArgumentOutOfRangeException( nameof( sourceMemory ), "source length must be <= destination length" );

			switch( mergeMode ) {
				case MergeMode.Copy:
					if( !sourceMemory.TryCopyTo( destinationMemory ) )
						throw new InvalidOperationException( "something went wrong in BitmapWithPages.MergeInto" );
					break;

				case MergeMode.And:
					for( int index = 0; index < sourceMemory.Length; index++ )
						destinationMemory.Span[ index ] &= sourceMemory.Span[ index ];
					break;

				case MergeMode.Or:
					for( int index = 0; index < sourceMemory.Length; index++ )
						destinationMemory.Span[ index ] |= sourceMemory.Span[ index ];
					break;

				case MergeMode.XOr:
					for( int index = 0; index < sourceMemory.Length; index++ )
						destinationMemory.Span[ index ] ^= sourceMemory.Span[ index ];
					break;
			}
		}

		static protected byte InvertLsb( byte inputByte ) {
			return ( byte )(
				( ( inputByte & 0b00000001 ) == 0 ? 0 : 0b10000000 ) |
				( ( inputByte & 0b00000010 ) == 0 ? 0 : 0b01000000 ) |
				( ( inputByte & 0b00000100 ) == 0 ? 0 : 0b00100000 ) |
				( ( inputByte & 0b00001000 ) == 0 ? 0 : 0b00010000 ) |
				( ( inputByte & 0b00010000 ) == 0 ? 0 : 0b00001000 ) |
				( ( inputByte & 0b00100000 ) == 0 ? 0 : 0b00000100 ) |
				( ( inputByte & 0b01000000 ) == 0 ? 0 : 0b00000010 ) |
				( ( inputByte & 0b10000000 ) == 0 ? 0 : 0b00000001 ) );
		}
	}
}
