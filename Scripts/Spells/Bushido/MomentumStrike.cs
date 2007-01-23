using System;
using System.Collections.Generic;
using Server.Network;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Bushido
{
	public class MomentumStrike : SamuraiMove
	{
		public MomentumStrike()
		{
		}

		public override int BaseMana{ get{ return 10; } }
		public override double RequiredSkill{ get{ return 70.0; } }

		public override TextDefinition AbilityMessage{ get{ return new TextDefinition( 1070757 ); } } // You prepare to strike two enemies with one blow.

		public override void OnHit( Mobile attacker, Mobile defender, int damage )
		{
			if ( !Validate( attacker ) || !CheckMana( attacker, false ) )
				return;

			ClearCurrentMove( attacker );

			BaseWeapon weapon = attacker.Weapon as BaseWeapon;

			List<Mobile> targets = new List<Mobile>();

			foreach ( Mobile m in attacker.GetMobilesInRange( weapon.MaxRange ) )
			{
				if ( m == defender )
					continue;

				if ( m.Combatant != attacker )
					continue;

				targets.Add( m );
			}

			if ( targets.Count > 0 )
			{
				if ( !CheckMana( attacker, true ) )
					return;

				Mobile target = targets[Utility.Random( targets.Count )];

				double damageBonus = attacker.Skills[SkillName.Bushido].Value / 100.0;

				if ( !defender.Alive )
					damageBonus *= 1.5;

				attacker.SendLocalizedMessage( 1063171 ); // You transfer the momentum of your weapon into another enemy!
				target.SendLocalizedMessage( 1063172 ); // You were hit by the momentum of a Samurai's weapon!

				target.FixedParticles( 0x37B9, 1, 4, 0x251D, 0, 0, EffectLayer.Waist );

                attacker.PlaySound( 0x510 );

				weapon.OnSwing( attacker, target, damageBonus );

				CheckGain( attacker );
			}
			else
			{
				attacker.SendLocalizedMessage( 1063123 ); // There are no valid targets to attack!
			}
		}

		public override void CheckGain( Mobile m )
		{
			m.CheckSkill( MoveSkill, RequiredSkill, 120.0 );
		}
	}
}
