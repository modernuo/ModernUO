using System;
using System.Collections;
using Server;
using Server.Engines;
using Server.Engines.Help;

namespace Server.Engines.Reports
{
	public class QueueStatus : PersistableObject
	{
		#region Type Identification
		public static readonly PersistableType ThisTypeID = new PersistableType( "qs", new ConstructCallback( Construct ) );

		private static PersistableObject Construct()
		{
			return new QueueStatus();
		}

		public override PersistableType TypeID{ get{ return ThisTypeID; } }
		#endregion

		private DateTime m_TimeStamp;
		private int m_Count;

		public DateTime TimeStamp{ get{ return m_TimeStamp; } set{ m_TimeStamp = value; } }
		public int Count{ get{ return m_Count; } set{ m_Count = value; } }

		public QueueStatus()
		{
		}

		public QueueStatus( int count )
		{
			m_TimeStamp = DateTime.Now;
			m_Count = count;
		}

		public override void SerializeAttributes( PersistanceWriter op )
		{
			op.SetDateTime( "t", m_TimeStamp );
			op.SetInt32( "c", m_Count );
		}

		public override void DeserializeAttributes( PersistanceReader ip )
		{
			m_TimeStamp = ip.GetDateTime( "t" );
			m_Count = ip.GetInt32( "c" );
		}
	}
}