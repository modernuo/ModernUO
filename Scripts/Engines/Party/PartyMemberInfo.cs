namespace Server.Engines.PartySystem
{
	public class PartyMemberInfo
	{
		public Mobile Mobile { get; }

		public bool CanLoot { get; set; }

		public PartyMemberInfo( Mobile m )
		{
			Mobile = m;
			CanLoot = !Core.ML;
		}
	}
}