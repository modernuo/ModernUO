using System;
using System.Collections;
using Server;
using Server.Engines;
using Server.Engines.Help;

namespace Server.Engines.Reports
{
	public class ResponseInfo : PersistableObject
	{
		#region Type Identification
		public static readonly PersistableType ThisTypeID = new PersistableType( "rs", new ConstructCallback( Construct ) );

		private static PersistableObject Construct()
		{
			return new ResponseInfo();
		}

		public override PersistableType TypeID{ get{ return ThisTypeID; } }
		#endregion

		private DateTime m_TimeStamp;

		private string m_SentBy;
		private string m_Message;

		public DateTime TimeStamp{ get{ return m_TimeStamp; } set{ m_TimeStamp = value; } }

		public string SentBy{ get{ return m_SentBy; } set{ m_SentBy = value; } }
		public string Message{ get{ return m_Message; } set{ m_Message = value; } }

		public ResponseInfo()
		{
		}

		public ResponseInfo( string sentBy, string message )
		{
			m_TimeStamp = DateTime.Now;
			m_SentBy = sentBy;
			m_Message = message;
		}

		public override void SerializeAttributes( PersistanceWriter op )
		{
			op.SetDateTime( "t", m_TimeStamp );

			op.SetString( "s", m_SentBy );
			op.SetString( "m", m_Message );
		}

		public override void DeserializeAttributes( PersistanceReader ip )
		{
			m_TimeStamp = ip.GetDateTime( "t" );

			m_SentBy = ip.GetString( "s" );
			m_Message = ip.GetString( "m" );
		}
	}
}