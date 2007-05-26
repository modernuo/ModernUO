using System;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Spellweaving
{
	public class NatureFurySpell : ArcanistSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Nature's Fury", "Rauvvrae",
				-1,
				false
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 1.5 ); } }

		public override double RequiredSkill { get { return 0.0; } }
		public override int RequiredMana { get { return 24; } }

		public NatureFurySpell( Mobile caster, Item scroll )
			: base( caster, scroll, m_Info )
		{
		}

		private bool m_MobileTarg;

		public override int GetMana()
		{
			int mana = base.GetMana();

			if( m_MobileTarg )
				mana *= 2;

			return mana;
		}



		public override bool CheckCast()
		{
			if( !base.CheckCast() )
				return false;

			if( (Caster.Followers + 1) > Caster.FollowersMax )
			{
				Caster.SendLocalizedMessage( 1049645 ); // You have too many followers to summon that creature.
				return false;
			}

			return true;
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public void Target( IPoint3D point )
		{
			//What happens if you cast it on yourself?	OSI ANSWER: You're an idiot for wasting the mana, and it'll just look for another target.
			Mobile m = point as Mobile;
			Point3D p = new Point3D( point );
			Map map = Caster.Map;

			m_MobileTarg = (m != null);

			

			if( !SpellHelper.FindValidSpawnLocation( map, ref p, m_MobileTarg ) )
			{
				Caster.SendLocalizedMessage( 501942 ); // That location is blocked.
			}
			else if( SpellHelper.CheckTown( p, Caster ) && (m_MobileTarg ? CheckHSequence( m ) : CheckSequence()) )
			{
				TimeSpan duration = TimeSpan.FromSeconds( Caster.Skills.Spellweaving.Value/24 + 25 + FocusLevel*2 );

				if( m == Caster )
					m = null;

				NatureFury nf = new NatureFury( m );
				BaseCreature.Summon( nf, false, Caster, p , 0x5CB, duration );

				Timer t = null;

				t = Timer.DelayCall( TimeSpan.FromSeconds( 5.0 ), TimeSpan.FromSeconds( 5.0 ), delegate
				 {
					 if( !nf.Alive || nf.Deleted || nf.DamageMin > 20 )	//sanity
						 t.Stop();

					 nf.DamageMin++;
					 nf.DamageMax++;
				 } );
			}

			FinishSequence();
		}

		private class InternalTarget : Target
		{
			private NatureFurySpell m_Owner;

			public InternalTarget( NatureFurySpell owner )
				: base( 12, true, TargetFlags.None )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				if( o is IPoint3D )
					m_Owner.Target( (IPoint3D)o );
			}


			protected override void OnTargetFinish( Mobile from )
			{
				if( m_Owner != null )
					m_Owner.FinishSequence();
			}
		}
	}
}