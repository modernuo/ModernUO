using System;
using Server;
using Server.Targeting;

namespace Server.Items
{
	public class IDWand : BaseWand
	{
		public override TimeSpan GetUseDelay => TimeSpan.Zero;

		[Constructible]
		public IDWand() : base( WandEffect.Identification, 25, 175 )
		{
		}

		public IDWand( Serial serial ) : base( serial )
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

		public override bool OnWandTarget( Mobile from, object o )
		{
			if (o is Item item)
			{
				if ( item is BaseWeapon weapon )
					weapon.Identified = true;
				else if ( item is BaseArmor armor )
					armor.Identified = true;

				if (!Core.AOS)
					item.OnSingleClick( from );

				return true;
			}

			return false;
		}
	}
}
