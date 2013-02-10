using System;
using Server;

namespace Server.Items
{

	public class TwilightLantern : Lantern
	{
		public override string DefaultName { get { return "Twilight Lantern"; } }

		[Constructable]
		public TwilightLantern()
			: base()
		{
			Hue = Utility.RandomBool() ? 244 : 997;
		}

		public override bool AllowEquipedCast( Mobile from )
		{
			return true;
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( 1060482 ); // Spell Channeling
		}

		public TwilightLantern( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( ( int )0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
