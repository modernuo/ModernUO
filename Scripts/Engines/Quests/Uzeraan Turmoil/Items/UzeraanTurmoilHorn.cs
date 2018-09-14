using Server.Mobiles;

namespace Server.Engines.Quests.Haven
{
	public class UzeraanTurmoilHorn : HornOfRetreat
	{
		public override bool ValidateUse( Mobile from )
		{
			return ( from is PlayerMobile pm && pm.Quest is UzeraanTurmoilQuest );
		}

		[Constructible]
		public UzeraanTurmoilHorn()
		{
			DestLoc = new Point3D( 3597, 2582, 0 );
			DestMap = Map.Trammel;
		}

		public UzeraanTurmoilHorn( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
