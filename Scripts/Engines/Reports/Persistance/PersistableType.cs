using System;
using System.Collections;

namespace Server.Engines.Reports
{
	public delegate PersistableObject ConstructCallback();

	public sealed class PersistableTypeRegistry
	{
		private static Hashtable m_Table;

		public static PersistableType Find( string name )
		{
			return m_Table[name] as PersistableType;
		}

		public static void Register( PersistableType type )
		{
			if ( type != null )
				m_Table[type.Name] = type;
		}

		static PersistableTypeRegistry()
		{
			m_Table = new Hashtable( StringComparer.OrdinalIgnoreCase );

			Register( Report.ThisTypeID );
			Register( BarGraph.ThisTypeID );
			Register( PieChart.ThisTypeID );
			Register( Snapshot.ThisTypeID );
			Register( ItemValue.ThisTypeID );
			Register( ChartItem.ThisTypeID );
			Register( ReportItem.ThisTypeID );
			Register( ReportColumn.ThisTypeID );
			Register( SnapshotHistory.ThisTypeID );

			Register( PageInfo.ThisTypeID );
			Register( QueueStatus.ThisTypeID );
			Register( StaffHistory.ThisTypeID );
			Register( ResponseInfo.ThisTypeID );
		}
	}

	public sealed class PersistableType
	{
		public string Name { get; }

		public ConstructCallback Constructor { get; }

		public PersistableType( string name, ConstructCallback constructor )
		{
			Name = name;
			Constructor = constructor;
		}
	}
}