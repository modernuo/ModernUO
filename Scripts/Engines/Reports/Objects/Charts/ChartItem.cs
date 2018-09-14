namespace Server.Engines.Reports
{
	public class ChartItem : PersistableObject
	{
		#region Type Identification
		public static readonly PersistableType ThisTypeID = new PersistableType( "ci", Construct );

		private static PersistableObject Construct()
		{
			return new ChartItem();
		}

		public override PersistableType TypeID => ThisTypeID;
		#endregion

		public string Name { get; set; }

		public int Value { get; set; }

		private ChartItem()
		{
		}

		public ChartItem( string name, int value )
		{
			Name = name;
			Value = value;
		}

		public override void SerializeAttributes( PersistanceWriter op )
		{
			op.SetString( "n", Name );
			op.SetInt32( "v", Value );
		}

		public override void DeserializeAttributes( PersistanceReader ip )
		{
			Name = Utility.Intern( ip.GetString( "n" ) );
			Value = ip.GetInt32( "v" );
		}
	}
}
