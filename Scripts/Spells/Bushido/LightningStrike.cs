using System;
using System.Collections;
using Server.Network;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Bushido
{
	public class LightningStrike : SamuraiMove
	{
		public LightningStrike()
		{
		}

		public override int BaseMana{ get{ return 5; } }
		public override double RequiredSkill{ get{ return 50.0; } }

		public override TextDefinition AbilityMessage{ get{ return new TextDefinition( 1063167 ); } } // You prepare to strike quickly.

		public override bool DelayedContext{ get{ return true; } }

		public override double GetAccuracyScalar( Mobile attacker )
		{
			if ( GetContext( attacker, typeof( Bushido.LightningStrike ) ) )
				return 1.1;

			return 1.5;
		}

		public override bool IgnoreArmor( Mobile attacker )
		{
			double bushido = attacker.Skills[SkillName.Bushido].Value;

			double criticalChance = (bushido * bushido) / 72000.0;

			return ( criticalChance >= Utility.RandomDouble() );
		}

		public override bool OnBeforeSwing( Mobile attacker, Mobile defender )
		{
			return Validate( attacker ) && CheckMana( attacker, true );
		}

		public override bool ValidatesDuringHit { get { return false; } }

		public override void OnHit( Mobile attacker, Mobile defender, int damage )
		{
			//Validation in OnBeforeSwing

			ClearCurrentMove( attacker );

			attacker.SendLocalizedMessage( 1063168 ); // You attack with lightning precision!
			defender.SendLocalizedMessage( 1063169 ); // Your opponent's quick strike causes extra damage!

            defender.FixedParticles( 0x3818, 1, 11, 0x13A8, 0, 0, EffectLayer.Waist );
            defender.PlaySound( 0x51D );

			CheckGain( attacker );

			SetContext( attacker );
		}

		public override void OnMiss( Mobile attacker, Mobile defender )
		{
			ClearCurrentMove( attacker );

			SetContext( attacker );
		}
	}
}
