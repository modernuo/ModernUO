using System;
using System.Collections;
using Server;
using Server.Targeting;
using Server.Network;
using Server.Mobiles;
using Server.Factions;

namespace Server.SkillHandlers
{
	public class AnimalTaming
	{
		private static Hashtable m_BeingTamed = new Hashtable();

		public static void Initialize()
		{
			SkillInfo.Table[(int)SkillName.AnimalTaming].Callback = new SkillUseCallback( OnUse );
		}

		private static bool m_DisableMessage;

		public static bool DisableMessage
		{
			get{ return m_DisableMessage; }
			set{ m_DisableMessage = value; }
		}

		public static TimeSpan OnUse( Mobile m )
		{
			m.RevealingAction();

			m.Target = new InternalTarget();
			m.RevealingAction();

			if ( !m_DisableMessage )
				m.SendLocalizedMessage( 502789 ); // Tame which animal?

			return TimeSpan.FromHours( 6.0 );
		}

		public static bool CheckMastery( Mobile tamer, BaseCreature creature )
		{
			BaseCreature familiar = (BaseCreature)Spells.Necromancy.SummonFamiliarSpell.Table[tamer];

			if ( familiar != null && !familiar.Deleted && familiar is DarkWolfFamiliar )
			{
				if ( creature is DireWolf || creature is GreyWolf || creature is TimberWolf || creature is WhiteWolf )
					return true;
			}

			return false;
		}

		public static bool MustBeSubdued( BaseCreature bc )
		{
			return bc.SubdueBeforeTame && (bc.Hits > (bc.HitsMax / 10));
		}

		public static void ScaleStats( BaseCreature bc, double scalar )
		{
			if ( bc.RawStr > 0 )
				bc.RawStr = (int)Math.Max( 1, bc.RawStr * scalar );

			if ( bc.RawDex > 0 )
				bc.RawDex = (int)Math.Max( 1, bc.RawDex * scalar );

			if ( bc.RawInt > 0 )
				bc.RawInt = (int)Math.Max( 1, bc.RawInt * scalar );

			if ( bc.HitsMaxSeed > 0 )
			{
				bc.HitsMaxSeed = (int)Math.Max( 1, bc.HitsMaxSeed * scalar );
				bc.Hits = bc.Hits;
				}

			if ( bc.StamMaxSeed > 0 )
			{
				bc.StamMaxSeed = (int)Math.Max( 1, bc.StamMaxSeed * scalar );
				bc.Stam = bc.Stam;
			}
		}

		public static void ScaleSkills( BaseCreature bc, double scalar )
		{
			for ( int i = 0; i < bc.Skills.Length; ++i )
			{
				bc.Skills[i].Base *= scalar;
				if ( bc.Skills[i].Base > 100.0 )
					bc.Skills[i].Cap = bc.Skills[i].Base;
			}
		}

		private class InternalTarget : Target
		{
			private bool m_SetSkillTime = true;

			public InternalTarget() :  base ( 2, false, TargetFlags.None )
			{
			}

			protected override void OnTargetFinish( Mobile from )
			{
				if ( m_SetSkillTime )
					from.NextSkillTime = DateTime.Now;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				from.RevealingAction();

				if ( targeted is Mobile )
				{
					if ( targeted is BaseCreature )
					{
						BaseCreature creature = (BaseCreature)targeted;

						if ( !creature.Tamable )
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1049655, from.NetState ); // That creature cannot be tamed.
						}
						else if ( creature.Controlled )
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502804, from.NetState ); // That animal looks tame already.
						}
						else if ( from.Female && !creature.AllowFemaleTamer )
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1049653, from.NetState ); // That creature can only be tamed by males.
						}
						else if ( !from.Female && !creature.AllowMaleTamer )
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1049652, from.NetState ); // That creature can only be tamed by females.
						}
						else if ( from.Followers + creature.ControlSlots > from.FollowersMax )
						{
							from.SendLocalizedMessage( 1049611 ); // You have too many followers to tame that creature.
						}
						else if ( creature.Owners.Count >= BaseCreature.MaxOwners && !creature.Owners.Contains( from ) )
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1005615, from.NetState ); // This animal has had too many owners and is too upset for you to tame.
						}
						else if ( MustBeSubdued( creature ) )
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1054025, from.NetState ); // You must subdue this creature before you can tame it!
						}
						else if ( CheckMastery( from, creature ) || from.Skills[SkillName.AnimalTaming].Value >= creature.MinTameSkill )
						{
							FactionWarHorse warHorse = creature as FactionWarHorse;

							if ( warHorse != null )
							{
								Faction faction = Faction.Find( from );

								if ( faction == null || faction != warHorse.Faction )
								{
									creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1042590, from.NetState ); // You cannot tame this creature.
									return;
								}
							}

							if ( m_BeingTamed.Contains( targeted ) )
							{
								creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502802, from.NetState ); // Someone else is already taming this.
							}
							else if ( creature.CanAngerOnTame && 0.95 >= Utility.RandomDouble() )
							{
								creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502805, from.NetState ); // You seem to anger the beast!
								creature.PlaySound( creature.GetAngerSound() );
								creature.Direction = creature.GetDirectionTo( from );
								creature.Combatant = from;
							}
							else
							{
								m_BeingTamed[targeted] = from;

								from.LocalOverheadMessage( MessageType.Emote, 0x59, 1010597 ); // You start to tame the creature.
								from.NonlocalOverheadMessage( MessageType.Emote, 0x59, 1010598 ); // *begins taming a creature.*

								new InternalTimer( from, creature, Utility.Random( 3, 2 ) ).Start();

								m_SetSkillTime = false;
							}
						}
						else
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502806, from.NetState ); // You have no chance of taming this creature.
						}
					}
					else
					{
						((Mobile)targeted).PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502469, from.NetState ); // That being cannot be tamed.
					}
				}
				else
				{
					from.SendLocalizedMessage( 502801 ); // You can't tame that!
				}
			}
			public static void AdjustSkillCaps( BaseCreature bc )
			{
				int totalCapIncrease = 0;

				for ( int i = 0; i < bc.Skills.Length; ++i )
				{
					if( bc.Skills[i].Base > bc.Skills[i].Cap )
					{
						totalCapIncrease += (bc.Skills[i].BaseFixedPoint - bc.Skills[i].CapFixedPoint);
						bc.Skills[i].Cap = bc.Skills[i].Base;
					}
				}

				bc.SkillsCap += totalCapIncrease;
			}

			private class InternalTimer : Timer
			{
				private Mobile m_Tamer;
				private BaseCreature m_Creature;
				private int m_MaxCount;
				private int m_Count;
				private bool m_Paralyzed;
				private DateTime m_StartTime;

				public InternalTimer( Mobile tamer, BaseCreature creature, int count ) : base( TimeSpan.FromSeconds( 3.0 ), TimeSpan.FromSeconds( 3.0 ), count )
				{
					m_Tamer = tamer;
					m_Creature = creature;
					m_MaxCount = count;
					m_Paralyzed = creature.Paralyzed;
					m_StartTime = DateTime.Now;
					Priority = TimerPriority.TwoFiftyMS;
				}

				protected override void OnTick()
				{
					m_Count++;

					DamageEntry de = m_Creature.FindMostRecentDamageEntry( false );
					bool alreadyOwned = m_Creature.Owners.Contains( m_Tamer );

					if ( !m_Tamer.InRange( m_Creature, 6 ) )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502795, m_Tamer.NetState ); // You are too far away to continue taming.
						Stop();
					}
					else if ( !m_Tamer.CheckAlive() )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502796, m_Tamer.NetState ); // You are dead, and cannot continue taming.
						Stop();
					}
					else if ( !m_Tamer.CanSee( m_Creature ) || !m_Tamer.InLOS( m_Creature ) )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1049654, m_Tamer.NetState ); // You do not have a clear path to the animal you are taming, and must cease your attempt.
						Stop();
					}
					else if ( !m_Creature.Tamable )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1049655, m_Tamer.NetState ); // That creature cannot be tamed.
						Stop();
					}
					else if ( m_Creature.Controlled )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502804, m_Tamer.NetState ); // That animal looks tame already.
						Stop();
					}
					else if ( m_Creature.Owners.Count >= BaseCreature.MaxOwners && !m_Creature.Owners.Contains( m_Tamer ) )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1005615, m_Tamer.NetState ); // This animal has had too many owners and is too upset for you to tame.
						Stop();
					}
					else if ( MustBeSubdued( m_Creature ) )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1054025, m_Tamer.NetState ); // You must subdue this creature before you can tame it!
						Stop();
					}
					else if ( de != null && de.LastDamage > m_StartTime )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502794, m_Tamer.NetState ); // The animal is too angry to continue taming.
						Stop();
					}
					else if ( m_Count < m_MaxCount )
					{
						m_Tamer.RevealingAction();

						switch ( Utility.Random( 3 ) )
						{
							case 0: m_Tamer.PublicOverheadMessage( MessageType.Regular, 0x3B2, Utility.Random( 502790, 4 ) ); break;
							case 1: m_Tamer.PublicOverheadMessage( MessageType.Regular, 0x3B2, Utility.Random( 1005608, 6 ) ); break;
							case 2: m_Tamer.PublicOverheadMessage( MessageType.Regular, 0x3B2, Utility.Random( 1010593, 4 ) ); break;
						}

						if ( !alreadyOwned ) // Passively check animal lore for gain
							m_Tamer.CheckTargetSkill( SkillName.AnimalLore, m_Creature, 0.0, 120.0 );

						if ( m_Creature.Paralyzed )
							m_Paralyzed = true;
					}
					else
					{
						m_Tamer.RevealingAction();
						m_Tamer.NextSkillTime = DateTime.Now;
						m_BeingTamed.Remove( m_Creature );

						if ( m_Creature.Paralyzed )
							m_Paralyzed = true;

						if ( !alreadyOwned ) // Passively check animal lore for gain
							m_Tamer.CheckTargetSkill( SkillName.AnimalLore, m_Creature, 0.0, 120.0 );

						double minSkill = m_Creature.MinTameSkill + (m_Creature.Owners.Count * 6.0);

						if ( minSkill > -24.9 && CheckMastery( m_Tamer, m_Creature ) )
							minSkill = -24.9; // 50% at 0.0?

						minSkill += 24.9;

						if ( alreadyOwned || m_Tamer.CheckTargetSkill( SkillName.AnimalTaming, m_Creature, minSkill - 25.0, minSkill + 25.0 ) )
						{
							if ( m_Creature.Owners.Count == 0 ) // First tame
							{
								if ( m_Paralyzed )
									ScaleSkills( m_Creature, 0.86 ); // 86% of original skills if they were paralyzed during the taming
								else
									ScaleSkills( m_Creature, 0.90 ); // 90% of original skills

								if ( m_Creature.StatLossAfterTame )
									ScaleStats( m_Creature, 0.50 );
								
								if( Core.SE )
									AdjustSkillCaps( m_Creature );
							}

							if ( alreadyOwned )
							{
								m_Tamer.SendLocalizedMessage( 502797 ); // That wasn't even challenging.
							}
							else
							{
								m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502799, m_Tamer.NetState ); // It seems to accept you as master.
								m_Creature.Owners.Add( m_Tamer );
							}

							m_Creature.SetControlMaster( m_Tamer );
							m_Creature.IsBonded = false;
						}
						else
						{
							m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502798, m_Tamer.NetState ); // You fail to tame the creature.
						}
					}
				}
			}
		}
	}
}