using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Ninjitsu;
using Server.Spells.Bushido;

namespace Server.Spells
{
	public abstract class SpecialMove
	{
		public virtual int BaseMana{ get{ return 0; } }

		public virtual SkillName MoveSkill{ get{ return SkillName.Bushido; } }
		public virtual double RequiredSkill{ get{ return 0.0; } }

		public virtual TextDefinition AbilityMessage{ get{ return 0; } }

		public virtual bool BlockedByAnimalForm{ get{ return true; } }
		public virtual bool DelayedContext{ get{ return false; } }

		public virtual double GetAccuracyScalar( Mobile attacker )
		{
			return 1.0;
		}

		public virtual double GetDamageScalar( Mobile attacker, Mobile defender )
		{
			return 1.0;
		}

		// Called before swinging, to make sure the accuracy scalar is to be computed.
		public virtual bool OnBeforeSwing( Mobile attacker, Mobile defender )
		{
			return true;
		}

		// Called when a hit connects, but before damage is calculated.
		public virtual bool OnBeforeDamage( Mobile attacker, Mobile defender )
		{
			return true;
		}

		// Called as soon as the ability is used.
		public virtual void OnUse( Mobile from )
		{
		}

		// Called when a hit connects, at the end of the weapon.OnHit() method.
		public virtual void OnHit( Mobile attacker, Mobile defender, int damage )
		{
		}

		// Called when a hit misses.
		public virtual void OnMiss( Mobile attacker, Mobile defender )
		{
		}

		// Called when the move is cleared.
		public virtual void OnClearMove( Mobile from )
		{
		}

		public virtual bool IgnoreArmor( Mobile attacker )
		{
			return false;
		}

		public virtual double GetPropertyBonus( Mobile attacker )
		{
			return 1.0;
		}

		public virtual bool CheckSkills( Mobile m )
		{
			if ( m.Skills[MoveSkill].Value < RequiredSkill )
			{
				string args = String.Format( "{0}\t{1}\t ", RequiredSkill.ToString( "F1" ), MoveSkill.ToString() );
				m.SendLocalizedMessage( 1063013, args ); // You need at least ~1_SKILL_REQUIREMENT~ ~2_SKILL_NAME~ skill to use that ability.
				return false;
			}

			return true;
		}

		public virtual int ScaleMana( Mobile m, int mana )
		{
			double scalar = 1.0;

			if ( !Server.Spells.Necromancy.MindRotSpell.GetMindRotScalar( m, ref scalar ) )
				scalar = 1.0;

			// Lower Mana Cost = 40%
			int lmc = Math.Min( AosAttributes.GetValue( m, AosAttribute.LowerManaCost ), 40 );

			scalar -= (double)lmc / 100;

			int total = (int)(mana * scalar);

			if ( m.Skills[MoveSkill].Value < 50.0 && GetContext( m ) != null )
				total *= 2;

			return total;
		}

		public virtual bool CheckMana( Mobile from, bool consume )
		{
			int mana = ScaleMana( from, BaseMana );

			if ( from.Mana < mana )
			{
				from.SendLocalizedMessage( 1060181, mana.ToString() ); // You need ~1_MANA_REQUIREMENT~ mana to perform that attack
				return false;
			}

			if ( consume )
			{
				if ( !DelayedContext )
					SetContext( from );

				from.Mana -= mana;
			}

			return true;
		}

		public virtual void SetContext( Mobile from )
		{
			if ( GetContext( from ) == null )
			{
				if ( DelayedContext || from.Skills[MoveSkill].Value < 50.0 )
				{
					Timer timer = new SpecialMoveTimer( from );
					timer.Start();

					AddContext( from, new SpecialMoveContext( timer, this.GetType() ) );
				}
			}
		}

		public virtual bool Validate( Mobile from )
		{
			if ( !from.Player )
				return true;

			if ( Bushido.HonorableExecution.IsUnderPenalty( from ) )
			{
				from.SendLocalizedMessage( 1063024 ); // You cannot perform this special move right now.
				return false;
			}

			if ( Ninjitsu.AnimalForm.UnderTransformation( from ) )
			{
				from.SendLocalizedMessage( 1063024 ); // You cannot perform this special move right now.
				return false;
			}

			return CheckSkills( from ) && CheckMana( from, false );
		}

		public virtual void CheckGain( Mobile m )
		{
			m.CheckSkill( MoveSkill, RequiredSkill, RequiredSkill + 37.5 );
		}

		private static Dictionary<Mobile, SpecialMove> m_Table = new Dictionary<Mobile, SpecialMove>();

		public static Dictionary<Mobile, SpecialMove> Table{ get{ return m_Table; } }

		public static void ClearAllMoves( Mobile m )
		{
			foreach ( KeyValuePair<Int32, SpecialMove> kvp in SpellRegistry.SpecialMoves )
			{
				int moveID = kvp.Key;

				if ( moveID != -1 )
					m.Send( new ToggleSpecialAbility( moveID + 1, false ) );
			}
		}

		public virtual bool ValidatesDuringHit{ get { return true; } }

		public static SpecialMove GetCurrentMove( Mobile m )
		{
			if ( m == null )
				return null;

			if ( !Core.SE )
			{
				ClearCurrentMove( m );
				return null;
			}

			SpecialMove move = null;
			m_Table.TryGetValue( m, out move );

			if ( move != null && move.ValidatesDuringHit && !move.Validate( m ) )
			{
				ClearCurrentMove( m );
				return null;
			}

			return move;
		}

		public static bool SetCurrentMove( Mobile m, SpecialMove move )
		{
			if ( !Core.SE )
			{
				ClearCurrentMove( m );
				return false;
			}

			if ( move != null && !move.Validate( m ) )
			{
				ClearCurrentMove( m );
				return false;
			}

			bool sameMove = ( move == GetCurrentMove( m ) );

			ClearCurrentMove( m );

			if ( sameMove )
				return true;

			if ( move != null )
			{
				WeaponAbility.ClearCurrentAbility( m );

				m_Table[m] = move;

				move.OnUse( m );

				int moveID = SpellRegistry.GetRegistryNumber( move );

				if ( moveID > 0 )
					m.Send( new ToggleSpecialAbility( moveID + 1, true ) );

				TextDefinition.SendMessageTo( m, move.AbilityMessage );
			}

			return true;
		}

		public static void ClearCurrentMove( Mobile m )
		{
			SpecialMove move = null;
			m_Table.TryGetValue( m, out move );

			if ( move != null )
			{
				move.OnClearMove( m );

				int moveID = SpellRegistry.GetRegistryNumber( move );

				if ( moveID > 0 )
					m.Send( new ToggleSpecialAbility( moveID + 1, false ) );
			}

			m_Table.Remove( m );
		}

		public SpecialMove()
		{
		}


		private static Dictionary<Mobile, SpecialMoveContext> m_PlayersTable = new Dictionary<Mobile, SpecialMoveContext>();

		private static void AddContext( Mobile m, SpecialMoveContext context )
		{
			m_PlayersTable[m] = context;
		}

		private static void RemoveContext( Mobile m )
		{
			SpecialMoveContext context = GetContext( m );

			if ( context != null )
			{
				m_PlayersTable.Remove( m );

				context.Timer.Stop();
			}
		}

		private static SpecialMoveContext GetContext( Mobile m )
		{
			return ( m_PlayersTable.ContainsKey( m ) ? m_PlayersTable[m] : null );
		}

		public static bool GetContext( Mobile m, Type type )
		{
			SpecialMoveContext context = null;
			m_PlayersTable.TryGetValue( m, out context );

			if ( context == null )
				return false;

			return ( context.Type == type );
		}

		private class SpecialMoveTimer : Timer
		{
			private Mobile m_Mobile;

			public SpecialMoveTimer( Mobile from ) : base ( TimeSpan.FromSeconds( 3.0 ) )
			{
				m_Mobile = from;

				Priority = TimerPriority.TwentyFiveMS;
			}

			protected override void OnTick()
			{
				RemoveContext( m_Mobile );
			}
		}

		public class SpecialMoveContext
		{
			private Timer m_Timer;
			private Type m_Type;

			public Timer Timer{ get{ return m_Timer; } }
			public Type Type{ get{ return m_Type; } }

			public SpecialMoveContext( Timer timer, Type type )
			{
				m_Timer = timer;
				m_Type = type;
			}
		}
	}
}