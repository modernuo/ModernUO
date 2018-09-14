namespace Server.Engines.Reports
{
	public class ReportItem : PersistableObject
	{
		#region Type Identification
		public static readonly PersistableType ThisTypeID = new PersistableType( "ri", Construct );

		private static PersistableObject Construct()
		{
			return new ReportItem();
		}

		public override PersistableType TypeID => ThisTypeID;
		#endregion

		public ItemValueCollection Values { get; }

		public ReportItem()
		{
			Values = new ItemValueCollection();
		}

		public override void SerializeChildren( PersistanceWriter op )
		{
			for ( int i = 0; i < Values.Count; ++i )
				Values[i].Serialize( op );
		}

		public override void DeserializeChildren( PersistanceReader ip )
		{
			while ( ip.HasChild )
				Values.Add( ip.GetChild() as ItemValue );
		}
	}
}
