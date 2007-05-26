using System;
using Server;
using Server.Spells;
using Server.Network;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.Bushido
{
	public abstract class SamuraiSpell : Spell
	{
		public abstract double RequiredSkill{ get; }
		public abstract int RequiredMana{ get; }

		public override SkillName CastSkill{ get{ return SkillName.Bushido; } }

		public override bool ClearHandsOnCast{ get{ return false; } }
		public override bool BlocksMovement{ get{ return false; } }
		public override bool ShowHandMovement{ get{ return false; } }

		//public override int CastDelayBase{ get{ return 1; } }
		public override double CastDelayFastScalar{ get{ return 0; } }

		public override int CastRecoveryBase{ get{ return 7; } }

		public SamuraiSpell( Mobile caster, Item scroll, SpellInfo info ) : base( caster, scroll, info )
		{
		}

		public static bool CheckExpansion( Mobile from )
		{
			if ( !( from is PlayerMobile ) )
				return true;

			if ( from.NetState == null )
				return false;

			//return ( (from.NetState.Flags & 0x10) != 0 );
			return from.NetState.SupportsExpansion( Expansion.SE );
		}

		public override bool CheckCast()
		{
			if ( !base.CheckCast() )
				return false;

			if ( !CheckExpansion( Caster ) )
			{
				Caster.SendLocalizedMessage( 1063456 ); // You must upgrade to Samurai Empire in order to use that ability.
				return false;
			}

			if ( Caster.Skills[CastSkill].Value < RequiredSkill )
			{
				string args = String.Format( "{0}\t{1}\t ", RequiredSkill.ToString( "F1" ), CastSkill.ToString() );
				Caster.SendLocalizedMessage( 1063013, args ); // You need at least ~1_SKILL_REQUIREMENT~ ~2_SKILL_NAME~ skill to use that ability.
				return false;
			}
			else if ( Caster.Mana < ScaleMana( RequiredMana ) )
			{
				Caster.SendLocalizedMessage( 1060174, RequiredMana.ToString() ); // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
				return false;
			}

			return true;
		}

		public override bool CheckFizzle()
		{
			int mana = ScaleMana( RequiredMana );

			if ( Caster.Skills[CastSkill].Value < RequiredSkill )
			{
				Caster.SendLocalizedMessage( 1070768, RequiredSkill.ToString( "F1" ) ); // You need ~1_SKILL_REQUIREMENT~ Bushido skill to perform that attack!
				return false;
			}
			else if ( Caster.Mana < mana )
			{
				Caster.SendLocalizedMessage( 1060174, mana.ToString() ); // You must have at least ~1_MANA_REQUIREMENT~ Mana to use this ability.
				return false;
			}

			if ( !base.CheckFizzle() )
				return false;

			Caster.Mana -= mana;

			return true;
		}

		public override void GetCastSkills( out double min, out double max )
		{
			min = RequiredSkill - 12.5;	//per 5 on friday, 2/16/07
			max = RequiredSkill + 37.5;
		}

		public override int GetMana()
		{
			return 0;
		}

		public virtual void OnCastSuccessful( Mobile caster )
		{
			if ( Evasion.IsEvading( caster ) )
				Evasion.EndEvasion( caster );

			if ( Confidence.IsConfident( caster ) )
				Confidence.EndConfidence( caster );

			if ( CounterAttack.IsCountering( caster ) )
				CounterAttack.StopCountering( caster );

			int spellID = SpellRegistry.GetRegistryNumber( this );

			if ( spellID > 0 )
				caster.Send( new ToggleSpecialAbility( spellID + 1, true ) );
		}

		public static void OnEffectEnd( Mobile caster, Type type )
		{
			int spellID = SpellRegistry.GetRegistryNumber( type );

			if ( spellID > 0 )
				caster.Send( new ToggleSpecialAbility( spellID + 1, false ) );
		}
	}
}