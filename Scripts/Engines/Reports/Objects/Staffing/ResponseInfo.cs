using System;

namespace Server.Engines.Reports
{
	public class ResponseInfo : PersistableObject
	{
		#region Type Identification
		public static readonly PersistableType ThisTypeID = new PersistableType( "rs", Construct );

		private static PersistableObject Construct()
		{
			return new ResponseInfo();
		}

		public override PersistableType TypeID => ThisTypeID;
		#endregion

		public DateTime TimeStamp { get; set; }

		public string SentBy { get; set; }

		public string Message { get; set; }

		public ResponseInfo()
		{
		}

		public ResponseInfo( string sentBy, string message )
		{
			TimeStamp = DateTime.UtcNow;
			SentBy = sentBy;
			Message = message;
		}

		public override void SerializeAttributes( PersistanceWriter op )
		{
			op.SetDateTime( "t", TimeStamp );

			op.SetString( "s", SentBy );
			op.SetString( "m", Message );
		}

		public override void DeserializeAttributes( PersistanceReader ip )
		{
			TimeStamp = ip.GetDateTime( "t" );

			SentBy = ip.GetString( "s" );
			Message = ip.GetString( "m" );
		}
	}
}
