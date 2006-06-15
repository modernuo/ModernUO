using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	public class SilvanisFeywoodBow : ElvenCompositeLongbow
	{
		public override int LabelNumber{ get{ return 1072955; } } // Silvani's Feywood Bow

		[Constructable]
		public SilvanisFeywoodBow()
		{
			Hue = 0x1A;

			Attributes.SpellChanneling = 1;
			Attributes.AttackChance = 12;
			Attributes.WeaponSpeed = 30;
			Attributes.WeaponDamage = 35;
		}

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy )
		{
			phys = fire = cold = pois = 0;
			nrgy = 100;
		}

		public SilvanisFeywoodBow( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}