using System;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Fifth
{
	public class SummonCreatureSpell : MagerySpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Summon Creature", "Kal Xen",
				266,
				9040,
				Reagent.Bloodmoss,
				Reagent.MandrakeRoot,
				Reagent.SpidersSilk
			);

		public override SpellCircle Circle { get { return SpellCircle.Fifth; } }

		public SummonCreatureSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		// TODO: Get real list
		private static Type[] m_Types = new Type[]
			{
				typeof( PolarBear ),
				typeof( GrizzlyBear ),
				typeof( BlackBear ),
				typeof( BrownBear ),
				typeof( Horse ),
				typeof( Walrus ),
				typeof( GreatHart ),
				typeof( Hind ),
				typeof( Dog ),
				typeof( Boar ),
				typeof( Chicken ),
				typeof( Rabbit )
			};

		public override bool CheckCast()
		{
			if ( !base.CheckCast() )
				return false;

			if ( (Caster.Followers + 2) > Caster.FollowersMax )
			{
				Caster.SendLocalizedMessage( 1049645 ); // You have too many followers to summon that creature.
				return false;
			}

			return true;
		}

		public override void OnCast()
		{
			if ( CheckSequence() )
			{
				try
				{
					BaseCreature creature = (BaseCreature)Activator.CreateInstance( m_Types[Utility.Random( m_Types.Length )] );

					creature.ControlSlots = 2;

					TimeSpan duration;

					if ( Core.AOS )
						duration = TimeSpan.FromSeconds( (2 * Caster.Skills.Magery.Fixed) / 5 );
					else
						duration = TimeSpan.FromSeconds( 4.0 * Caster.Skills[SkillName.Magery].Value );

					SpellHelper.Summon( creature, Caster, 0x215, duration, false, false );
				}
				catch
				{
				}
			}

			FinishSequence();
		}

		public override TimeSpan GetCastDelay()
		{
			if ( Core.AOS )
				return TimeSpan.FromTicks( base.GetCastDelay().Ticks * 5 );

			return base.GetCastDelay() + TimeSpan.FromSeconds( 6.0 );
		}
	}
}