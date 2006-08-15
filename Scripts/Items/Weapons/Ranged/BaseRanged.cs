using System;
using Server.Items;
using Server.Network;
using Server.Spells;
using Server.Mobiles;

namespace Server.Items
{
	public abstract class BaseRanged : BaseMeleeWeapon
	{
		public abstract int EffectID{ get; }
		public abstract Type AmmoType{ get; }
		public abstract Item Ammo{ get; }

		public override int DefHitSound{ get{ return 0x234; } }
		public override int DefMissSound{ get{ return 0x238; } }

		public override SkillName DefSkill{ get{ return SkillName.Archery; } }
		public override WeaponType DefType{ get{ return WeaponType.Ranged; } }
		public override WeaponAnimation DefAnimation{ get{ return WeaponAnimation.ShootXBow; } }

		public override SkillName AccuracySkill{ get{ return SkillName.Archery; } }

		public BaseRanged( int itemID ) : base( itemID )
		{
		}

		public BaseRanged( Serial serial ) : base( serial )
		{
		}

		public override TimeSpan OnSwing( Mobile attacker, Mobile defender )
		{
			WeaponAbility a = WeaponAbility.GetCurrentAbility( attacker );

			// Make sure we've been standing still for .25/.5/1 second depending on Era
			if ( DateTime.Now > (attacker.LastMoveTime + TimeSpan.FromSeconds( Core.SE ? 0.25 : (Core.AOS ? 0.5 : 1.0) )) || (Core.AOS && WeaponAbility.GetCurrentAbility( attacker ) is MovingShot) )
			{
				bool canSwing = true;

				if ( Core.AOS )
				{
					canSwing = ( !attacker.Paralyzed && !attacker.Frozen );

					if ( canSwing )
					{
						Spell sp = attacker.Spell as Spell;

						canSwing = ( sp == null || !sp.IsCasting || !sp.BlocksMovement );
					}
				}

				if ( canSwing && attacker.HarmfulCheck( defender ) )
				{
					attacker.DisruptiveAction();
					attacker.Send( new Swing( 0, attacker, defender ) );

					if ( OnFired( attacker, defender ) )
					{
						if ( CheckHit( attacker, defender ) )
							OnHit( attacker, defender );
						else
							OnMiss( attacker, defender );
					}
				}

				attacker.RevealingAction();

				return GetDelay( attacker );
			}
			else
			{
				attacker.RevealingAction();

				return TimeSpan.FromSeconds( 0.25 );
			}
		}

		public override void OnHit( Mobile attacker, Mobile defender, double damageBonus )
		{
			if ( attacker.Player && !defender.Player && (defender.Body.IsAnimal || defender.Body.IsMonster) && 0.4 >= Utility.RandomDouble() )
				defender.AddToBackpack( Ammo );

			base.OnHit( attacker, defender, damageBonus );
		}

		public override void OnMiss( Mobile attacker, Mobile defender )
		{
			if ( attacker.Player && 0.4 >= Utility.RandomDouble() )
				Ammo.MoveToWorld( new Point3D( defender.X + Utility.RandomMinMax( -1, 1 ), defender.Y + Utility.RandomMinMax( -1, 1 ), defender.Z ), defender.Map );

			base.OnMiss( attacker, defender );
		}

		public virtual bool OnFired( Mobile attacker, Mobile defender )
		{
			Container pack = attacker.Backpack;

			if ( attacker.Player && (pack == null || !pack.ConsumeTotal( AmmoType, 1 )) )
				return false;

			attacker.MovingEffect( defender, EffectID, 18, 1, false, false );

			return true;
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 2 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 2:
				case 1:
				{
					break;
				}
				case 0:
				{
					/*m_EffectID =*/ reader.ReadInt();
					break;
				}
			}

			if ( version < 2 )
			{
				WeaponAttributes.MageWeapon = 0;
				WeaponAttributes.UseBestSkill = 0;
			}
		}
	}
}