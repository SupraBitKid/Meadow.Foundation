using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meadow.Foundation.Bitmap {
	[Serializable]
	public class BitmapException : Exception {
		private BitmapException() { }

		public BitmapException( string format, params object[] args ) 
			: base( string.Format( format, args ) ) { }

		public BitmapException( Exception innerException, string format, params object[] args ) 
			: base( string.Format( format, args ), innerException ) {}

		protected BitmapException( System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext ) 
			: base( serializationInfo, streamingContext ){ }
	}
}
