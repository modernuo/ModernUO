using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Spells.Fifth;
using Server.Spells.Seventh;

namespace Server.Spells.Necromancy
{
	public abstract class TransformationSpell : NecromancerSpell
	{
		public abstract int Body{ get; }
		public virtual int Hue{ get{ return 0; } }

		public virtual int PhysResistOffset{ get{ return 0; } }
		public virtual int FireResistOffset{ get{ return 0; } }
		public virtual int ColdResistOffset{ get{ return 0; } }
		public virtual int PoisResistOffset{ get{ return 0; } }
		public virtual int NrgyResistOffset{ get{ return 0; } }

		public TransformationSpell( Mobile caster, Item scroll, SpellInfo info ) : base( caster, scroll, info )
		{
		}

		public override bool BlockedByHorrificBeast{ get{ return false; } }

		public override bool CheckCast()
		{
			/*if ( Caster.Mounted )
			{
				Caster.SendLocalizedMessage( 1042561 ); // Please dismount first.
				return false;
			}
			else */
			if ( Factions.Sigil.ExistsOn( Caster ) )
			{
				Caster.SendLocalizedMessage( 1061632 ); // You can't do that while carrying the sigil.
				return false;
			}
			else if ( !Caster.CanBeginAction( typeof( PolymorphSpell ) ) )
			{
				Caster.SendLocalizedMessage( 1061628 ); // You can't do that while polymorphed.
				return false;
			}
			else if ( Spells.Ninjitsu.AnimalForm.UnderTransformation( Caster ) )
			{
				Caster.SendLocalizedMessage( 1061091 ); // You cannot cast that spell in this form.
				return false;
			}

			return base.CheckCast();
		}

		public override void OnCast()
		{
			Mobile caster = this.Caster;

			/*if ( caster.Mounted )
			{
				caster.SendLocalizedMessage( 1042561 ); // Please dismount first.
			}
			else */
			if ( Factions.Sigil.ExistsOn( Caster ) )
			{
				Caster.SendLocalizedMessage( 1061632 ); // You can't do that while carrying the sigil.
			}
			else if ( !caster.CanBeginAction( typeof( PolymorphSpell ) ) )
			{
				Caster.SendLocalizedMessage( 1061628 ); // You can't do that while polymorphed.
			}
			else if ( Spells.Ninjitsu.AnimalForm.UnderTransformation( Caster ) )
			{
				Caster.SendLocalizedMessage( 1061091 ); // You cannot cast that spell in this form.
			}
			else if ( !caster.CanBeginAction( typeof( IncognitoSpell ) ) || (caster.IsBodyMod && GetContext( caster ) == null) )
			{
				DoFizzle();
			}
			else if ( CheckSequence() )
			{
				TransformContext context = GetContext( caster );
				Type ourType = this.GetType();

				bool wasTransformed = ( context != null );
				bool ourTransform = ( wasTransformed && context.Type == ourType );

				if ( wasTransformed )
				{
					RemoveContext( caster, context, ourTransform );

					if ( ourTransform )
					{
						caster.PlaySound( 0xFA );
						caster.FixedParticles( 0x3728, 1, 13, 5042, EffectLayer.Waist );
					}
				}

				if ( !ourTransform )
				{
					List<ResistanceMod> mods = new List<ResistanceMod>();

					if ( PhysResistOffset != 0 )
						mods.Add( new ResistanceMod( ResistanceType.Physical, PhysResistOffset ) );

					if ( FireResistOffset != 0 )
						mods.Add( new ResistanceMod( ResistanceType.Fire, FireResistOffset ) );

					if ( ColdResistOffset != 0 )
						mods.Add( new ResistanceMod( ResistanceType.Cold, ColdResistOffset ) );

					if ( PoisResistOffset != 0 )
						mods.Add( new ResistanceMod( ResistanceType.Poison, PoisResistOffset ) );

					if ( NrgyResistOffset != 0 )
						mods.Add( new ResistanceMod( ResistanceType.Energy, NrgyResistOffset ) );

					if ( !((Body)this.Body).IsHuman )
					{
						Mobiles.IMount mt = Caster.Mount;

						if ( mt != null )
							mt.Rider = null;
					}

					caster.BodyMod = this.Body;
					caster.HueMod = this.Hue;

					for ( int i = 0; i < mods.Count; ++i )
						caster.AddResistanceMod( mods[i] );

					PlayEffect( caster );

					Timer timer = new TransformTimer( caster, this );
					timer.Start();

					AddContext( caster, new TransformContext( timer, mods, ourType ) );
				}
			}

			FinishSequence();
		}

		public virtual double TickRate{ get{ return 1.0; } }

		public virtual void OnTick( Mobile m )
		{
		}

		public virtual void PlayEffect( Mobile m )
		{
		}

		private static Hashtable m_Table = new Hashtable();

		public static void AddContext( Mobile m, TransformContext context )
		{
			m_Table[m] = context;

			if ( context.Type == typeof( HorrificBeastSpell ) )
				m.Delta( MobileDelta.WeaponDamage );
		}

		public static void RemoveContext( Mobile m, bool resetGraphics )
		{
			TransformContext context = GetContext( m );

			if ( context != null )
				RemoveContext( m, context, resetGraphics );
		}

		public static void RemoveContext( Mobile m, TransformContext context, bool resetGraphics )
		{
			m_Table.Remove( m );

			List<ResistanceMod> mods = context.Mods;

			for ( int i = 0; i < mods.Count; ++i )
				m.RemoveResistanceMod( mods[i] );

			if ( resetGraphics )
			{
				m.HueMod = -1;
				m.BodyMod = 0;
			}

			context.Timer.Stop();

			if ( context.Type == typeof( HorrificBeastSpell ) )
				m.Delta( MobileDelta.WeaponDamage );
		}

		public static TransformContext GetContext( Mobile m )
		{
			return ( m_Table[m] as TransformContext );
		}

		public static bool UnderTransformation( Mobile m )
		{
			return ( GetContext( m ) != null );
		}

		public static bool UnderTransformation( Mobile m, Type type )
		{
			TransformContext context = GetContext( m );

			return ( context != null && context.Type == type );
		}
	}

	public class TransformContext
	{
		private Timer m_Timer;
		private List<ResistanceMod> m_Mods;
		private Type m_Type;

		public Timer Timer{ get{ return m_Timer; } }
		public List<ResistanceMod> Mods{ get{ return m_Mods; } }
		public Type Type{ get{ return m_Type; } }

		public TransformContext( Timer timer, List<ResistanceMod> mods, Type type )
		{
			m_Timer = timer;
			m_Mods = mods;
			m_Type = type;
		}
	}

	public class TransformTimer : Timer
	{
		private Mobile m_Mobile;
		private TransformationSpell m_Spell;

		public TransformTimer( Mobile from, TransformationSpell spell ) : base( TimeSpan.FromSeconds( spell.TickRate ), TimeSpan.FromSeconds( spell.TickRate ) )
		{
			m_Mobile = from;
			m_Spell = spell;

			Priority = TimerPriority.TwoFiftyMS;
		}

		protected override void OnTick()
		{
			if ( m_Mobile.Deleted || !m_Mobile.Alive || m_Mobile.Body != m_Spell.Body || m_Mobile.Hue != m_Spell.Hue )
			{
				TransformationSpell.RemoveContext( m_Mobile, true );
				Stop();
			}
			else
			{
				m_Spell.OnTick( m_Mobile );
			}
		}
	}
}