namespace Server.Engines.Reports
{
	public class PieChart : Chart
	{
		#region Type Identification
		public static readonly PersistableType ThisTypeID = new PersistableType( "pc", Construct );

		private static PersistableObject Construct()
		{
			return new PieChart();
		}

		public override PersistableType TypeID => ThisTypeID;
		#endregion

		public bool ShowPercents { get; set; }

		public PieChart( string name, string fileName, bool showPercents )
		{
			m_Name = name;
			m_FileName = fileName;
			ShowPercents = showPercents;
		}

		private PieChart()
		{
		}

		public override void SerializeAttributes( PersistanceWriter op )
		{
			base.SerializeAttributes( op );

			op.SetBoolean( "p", ShowPercents );
		}

		public override void DeserializeAttributes( PersistanceReader ip )
		{
			base.DeserializeAttributes( ip );

			ShowPercents = ip.GetBoolean( "p" );
		}
	}
}
