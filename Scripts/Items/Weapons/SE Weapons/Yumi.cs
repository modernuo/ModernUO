using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	[FlipableAttribute( 0x27A5, 0x27F0 )]
	public class Yumi : BaseRanged
	{
		public override int EffectID{ get{ return 0xF42; } }
		public override Type AmmoType{ get{ return typeof( Arrow ); } }
		public override Item Ammo{ get{ return new Arrow(); } }

		public override WeaponAbility PrimaryAbility{ get{ return WeaponAbility.ArmorPierce; } }
		public override WeaponAbility SecondaryAbility{ get{ return WeaponAbility.DoubleShot; } }

		public override int AosStrengthReq{ get{ return 35; } }
		public override int AosMinDamage{ get{ return 18; } }
		public override int AosMaxDamage{ get{ return 20; } }
		public override int AosSpeed{ get{ return 25; } }

		public override int OldStrengthReq{ get{ return 35; } }
		public override int OldMinDamage{ get{ return 18; } }
		public override int OldMaxDamage{ get{ return 20; } }
		public override int OldSpeed{ get{ return 25; } }

		public override int DefMaxRange{ get{ return 10; } }

		public override int InitMinHits{ get{ return 55; } }
		public override int InitMaxHits{ get{ return 60; } }

		public override WeaponAnimation DefAnimation{ get{ return WeaponAnimation.ShootBow; } }

		[Constructable]
		public Yumi() : base( 0x27A5 )
		{
			Weight = 9.0;
			Layer = Layer.TwoHanded;
		}

		public Yumi( Serial serial ) : base( serial )
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

			if ( Weight == 7.0 )
				Weight = 6.0;
		}
	}
}