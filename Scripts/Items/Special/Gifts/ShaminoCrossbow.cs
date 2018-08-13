using System;
using Server;

namespace Server.Items
{
	public class ShaminoCrossbow : RepeatingCrossbow
	{
		public override int LabelNumber => 1062915; // Shamino�s Best Crossbow

		public override int InitMinHits => 255;
		public override int InitMaxHits => 255;

		[Constructible]
		public ShaminoCrossbow()
		{
			Hue = 0x504;
			LootType = LootType.Blessed;

			Attributes.AttackChance = 15;
			Attributes.WeaponDamage = 40;
			WeaponAttributes.SelfRepair = 10;
			WeaponAttributes.LowerStatReq = 100;
		}

		public ShaminoCrossbow( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}
