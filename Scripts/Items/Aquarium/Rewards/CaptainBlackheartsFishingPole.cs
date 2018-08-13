using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public class CaptainBlackheartsFishingPole : FishingPole
	{
		public override int LabelNumber => 1074571; // Captain Blackheart's Fishing Pole

		[Constructible]
		public CaptainBlackheartsFishingPole()
		{
		}

		public CaptainBlackheartsFishingPole( Serial serial ) : base( serial )
		{
		}

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );

			list.Add( 1073634 ); // An aquarium decoration
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
