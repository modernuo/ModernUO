using System;

namespace Server.Engines.Reports
{
	public class ReportItem : PersistableObject
	{
		#region Type Identification
		public static readonly PersistableType ThisTypeID = new PersistableType( "ri", new ConstructCallback( Construct ) );

		private static PersistableObject Construct()
		{
			return new ReportItem();
		}

		public override PersistableType TypeID{ get{ return ThisTypeID; } }
		#endregion

		private ItemValueCollection m_Values;

		public ItemValueCollection Values{ get{ return m_Values; } }

		public ReportItem()
		{
			m_Values = new ItemValueCollection();
		}

		public override void SerializeChildren( PersistanceWriter op )
		{
			for ( int i = 0; i < m_Values.Count; ++i )
				m_Values[i].Serialize( op );
		}

		public override void DeserializeChildren( PersistanceReader ip )
		{
			while ( ip.HasChild )
				m_Values.Add( ip.GetChild() as ItemValue );
		}
	}
}