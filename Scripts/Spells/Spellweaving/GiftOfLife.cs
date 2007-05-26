using System;
using System.Collections;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Gumps;
using Server.Targeting;

namespace Server.Spells.Spellweaving
{
	public class GiftOfLifeSpell : ArcanistSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Gift of Life", "Illorae",
				-1
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 4.0 ); } }

		public override double RequiredSkill { get { return 38.0; } }
		public override int RequiredMana { get { return 70; } }

		public GiftOfLifeSpell( Mobile caster, Item scroll )
			: base( caster, scroll, m_Info )
		{
		}

		public static void Initialize()
		{
			EventSink.PlayerDeath += new PlayerDeathEventHandler( delegate( PlayerDeathEventArgs e )
			{
				HandleDeath( e.Mobile );
			} );
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public void Target( Mobile m )
		{
			BaseCreature bc = m as BaseCreature;

			if( !Caster.CanSee( m ) )
			{
				Caster.SendLocalizedMessage( 500237 ); // Target can not be seen.
			}
			else if( m.IsDeadBondedPet || !m.Alive )
			{
				// As per Osi: Nothing happens.
			}
			else if( m != Caster && (bc == null || !bc.IsBonded || bc.ControlMaster != Caster) )
			{
				Caster.SendLocalizedMessage( 1072077 ); // You may only cast this spell on yourself or a bonded pet.
			}
			else if( m_Table.ContainsKey( m ) )
			{
				Caster.SendLocalizedMessage( 501775 ); // This spell is already in effect.
			}
			else if( CheckBSequence( m ) )
			{
				if( Caster == m )
				{
					Caster.SendLocalizedMessage( 1074774 ); // You weave powerful magic, protecting yourself from death.
				}
				else
				{
					Caster.SendLocalizedMessage( 1074775 ); // You weave powerful magic, protecting your pet from death.
					SpellHelper.Turn( Caster, m );
				}


				m.PlaySound( 0x244 );
				m.FixedParticles( 0x3709, 1, 30, 0x26ED, 5, 2, EffectLayer.Waist );
				m.FixedParticles( 0x376A, 1, 30, 0x251E, 5, 3, EffectLayer.Waist );

				double skill = Caster.Skills[SkillName.Spellweaving].Value;

				TimeSpan duration = TimeSpan.FromMinutes( ((int)(skill / 24))* 2 + FocusLevel );

				ExpireTimer t = new ExpireTimer( m, duration, this );
				t.Start();

				m_Table[m] = t;

				BuffInfo.AddBuff( m, new BuffInfo( BuffIcon.GiftOfLife, 1031615, 1075807, duration, m, null, true ) );
			}

			FinishSequence();
		}

		private static Dictionary<Mobile, ExpireTimer> m_Table = new Dictionary<Mobile, ExpireTimer>();

		public static void HandleDeath( Mobile m )
		{
			if( m_Table.ContainsKey( m ) )
				Timer.DelayCall<Mobile>( TimeSpan.FromSeconds( Utility.RandomMinMax( 2, 4 ) ), new TimerStateCallback<Mobile>( HandleDeath_OnCallback ), m );
		}

		private static void HandleDeath_OnCallback( Mobile m )
		{
			ExpireTimer timer;

			if( m_Table.TryGetValue( m, out timer ) )
			{
				double hitsScalar = timer.Spell.HitsScalar;

				if( m is BaseCreature && m.IsDeadBondedPet )
				{
					BaseCreature pet = (BaseCreature)m;
					Mobile master = pet.GetMaster();

					if( master != null && master.NetState != null && Utility.InUpdateRange( pet, master ) )
					{
						master.CloseGump( typeof( PetResurrectGump ) );
						master.SendGump( new PetResurrectGump( master, pet, hitsScalar ) );
					}
					else
					{
						List<Mobile> friends = pet.Friends;

						for( int i = 0; friends != null && i < friends.Count; i++ )
						{
							Mobile friend = friends[i];

							if( friend.NetState != null && Utility.InUpdateRange( pet, friend ) )
							{
								friend.CloseGump( typeof( PetResurrectGump ) );
								friend.SendGump( new PetResurrectGump( friend, pet ) );
								break;
							}
						}
					}
				}
				else
				{
					m.CloseGump( typeof( ResurrectGump ) );
					m.SendGump( new ResurrectGump( m, hitsScalar ) );
				}

				//Per OSI, buff is removed when gump sent, irregardless of online status or acceptence
				timer.DoExpire();
			}

		}

		public double HitsScalar { get { return ((Caster.Skills.Spellweaving.Value/2.4) + FocusLevel)/100; } }

		public static void OnLogin( LoginEventArgs e )
		{
			Mobile m = e.Mobile;

			if( m == null || m.Alive || m_Table[m] == null )
				return;

			HandleDeath_OnCallback( m );
		}

		private class ExpireTimer : Timer
		{
			private Mobile m_Mobile;

			private GiftOfLifeSpell m_Spell;

			public GiftOfLifeSpell Spell { get { return m_Spell; } }

			public ExpireTimer( Mobile m, TimeSpan delay, GiftOfLifeSpell spell )
				: base( delay )
			{
				m_Mobile = m;
				m_Spell = spell;
			}

			protected override void OnTick()
			{
				DoExpire();
			}

			public void DoExpire()
			{
				Stop();

				m_Mobile.SendLocalizedMessage( 1074776 ); // You are no longer protected with Gift of Life.
				m_Table.Remove( m_Mobile );

				BuffInfo.RemoveBuff( m_Mobile, BuffIcon.GiftOfLife );
			}
		}

		public class InternalTarget : Target
		{
			private GiftOfLifeSpell m_Owner;

			public InternalTarget( GiftOfLifeSpell owner )
				: base( 12, false, TargetFlags.Beneficial )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile m, object o )
			{
				if( o is Mobile )
				{
					m_Owner.Target( (Mobile)o );
				}
				else
				{
					m.SendLocalizedMessage( 1072077 ); // You may only cast this spell on yourself or a bonded pet.
				}
			}

			protected override void OnTargetFinish( Mobile m )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}