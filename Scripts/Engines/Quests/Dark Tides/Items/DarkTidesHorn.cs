using Server.Mobiles;

namespace Server.Engines.Quests.Necro
{
	public class DarkTidesHorn : HornOfRetreat
	{
		public override bool ValidateUse( Mobile from )
		{
			return ( from is PlayerMobile pm && pm.Quest is DarkTidesQuest );
		}

		[Constructible]
		public DarkTidesHorn()
		{
			DestLoc = new Point3D( 2103, 1319, -68 );
			DestMap = Map.Malas;
		}

		public DarkTidesHorn( Serial serial ) : base( serial )
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
