using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public abstract class BaseSpear : BaseMeleeWeapon
	{
		public override int DefHitSound{ get{ return 0x23C; } }
		public override int DefMissSound{ get{ return 0x238; } }

		public override SkillName DefSkill{ get{ return SkillName.Fencing; } }
		public override WeaponType DefType{ get{ return WeaponType.Piercing; } }
		public override WeaponAnimation DefAnimation{ get{ return WeaponAnimation.Pierce2H; } }

		public BaseSpear( int itemID ) : base( itemID )
		{
		}

		public BaseSpear( Serial serial ) : base( serial )
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

		public override void OnHit( Mobile attacker, Mobile defender, double damageBonus )
		{
			base.OnHit( attacker, defender, damageBonus );

			if ( !Core.AOS && Layer == Layer.TwoHanded && (attacker.Skills[SkillName.Anatomy].Value / 400.0) >= Utility.RandomDouble() )
			{
				defender.SendMessage( "You receive a paralyzing blow!" ); // Is this not localized?
				defender.Freeze( TimeSpan.FromSeconds( 2.0 ) );

				attacker.SendMessage( "You deliver a paralyzing blow!" ); // Is this not localized?
				attacker.PlaySound( 0x11C );
			}

			if ( !Core.AOS && Poison != null && PoisonCharges > 0 )
			{
				--PoisonCharges;

				if ( Utility.RandomDouble() >= 0.5 ) // 50% chance to poison
					defender.ApplyPoison( attacker, Poison );
			}
		}
	}
}