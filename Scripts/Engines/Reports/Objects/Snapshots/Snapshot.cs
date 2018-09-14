using System;

namespace Server.Engines.Reports
{
	public class Snapshot : PersistableObject
	{
		#region Type Identification
		public static readonly PersistableType ThisTypeID = new PersistableType( "ss", Construct );

		private static PersistableObject Construct()
		{
			return new Snapshot();
		}

		public override PersistableType TypeID => ThisTypeID;
		#endregion

		public DateTime TimeStamp { get; set; }

		public ObjectCollection Children { get; set; }

		public Snapshot()
		{
			Children = new ObjectCollection();
		}

		public override void SerializeAttributes( PersistanceWriter op )
		{
			op.SetDateTime( "t", TimeStamp );
		}

		public override void DeserializeAttributes( PersistanceReader ip )
		{
			TimeStamp = ip.GetDateTime( "t" );
		}

		public override void SerializeChildren( PersistanceWriter op )
		{
			for ( int i = 0; i < Children.Count; ++i )
				Children[i].Serialize( op );
		}

		public override void DeserializeChildren( PersistanceReader ip )
		{
			while ( ip.HasChild )
				Children.Add( ip.GetChild() );
		}
	}
}
