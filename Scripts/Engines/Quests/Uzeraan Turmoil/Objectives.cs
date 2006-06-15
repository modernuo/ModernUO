using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Network;

namespace Server.Engines.Quests.Haven
{
	public class FindUzeraanBeginObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Find Uzeraan.  Uzeraan will explain what you need to do next.
				return 1046039;
			}
		}

		public FindUzeraanBeginObjective()
		{
		}

		public override void OnComplete()
		{
			if ( System.From.Profession == 5 ) // paladin
				System.AddConversation( new UzeraanTitheConversation() );
			else
				System.AddConversation( new UzeraanFirstTaskConversation() );
		}
	}

	public class TitheGoldObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Go to the shrine inside of Uzeraan's Mansion, near the front doors and
				 * <a href = "?ForceTopic109">tithe</a> at least 500 gold.<BR><BR>
				 * 
				 * Return to Uzeraan when you are done.
				 */
				return 1060386;
			}
		}

		private int m_OldTithingPoints;

		public TitheGoldObjective()
		{
			m_OldTithingPoints = -1;
		}

		public override void CheckProgress()
		{
			PlayerMobile pm = System.From;
			int curTithingPoints = pm.TithingPoints;

			if ( curTithingPoints >= 500 )
				Complete();
			else if ( curTithingPoints > m_OldTithingPoints && m_OldTithingPoints >= 0 )
				pm.SendLocalizedMessage( 1060240, "", 0x41 ); // You must have at least 500 tithing points before you can continue in your quest.

			m_OldTithingPoints = curTithingPoints;
		}

		public override void OnComplete()
		{
			System.AddObjective( new FindUzeraanFirstTaskObjective() );
		}
	}

	public class FindUzeraanFirstTaskObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Return to Uzeraan, now that you have enough tithing points to continue your quest.
				return 1060387;
			}
		}

		public FindUzeraanFirstTaskObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new UzeraanFirstTaskConversation() );
		}
	}

	public enum KillHordeMinionsStep
	{
		First,
		LearnKarma,
		Others
	}

	public class KillHordeMinionsObjective : QuestObjective
	{
		private KillHordeMinionsStep m_Step;

		public KillHordeMinionsStep Step { get { return m_Step; } }

		public override object Message
		{
			get
			{
				switch ( m_Step )
				{
					case KillHordeMinionsStep.First:
						/* Find the mountain pass beyond the house which lies at the
						 * end of the runic road.<BR><BR>
						 * 
						 * Assist the city Militia by slaying <I>Horde Minions</I>
						 */
						return 1049089;

					case KillHordeMinionsStep.LearnKarma:
						/* You have just gained some <a href="?ForceTopic45">Karma</a>
						 * for killing the horde minion. <a href="?ForceTopic134">Learn</a>
						 * how this affects your Paladin abilities.
						 */
						return 1060389;

					default:
						// Continue driving back the Horde Minions, as Uzeraan instructed you to do.
						return 1060507;
				}
			}
		}

		public override int MaxProgress
		{
			get
			{
				if ( System.From.Profession == 5 ) // paladin
				{
					switch ( m_Step )
					{
						case KillHordeMinionsStep.First: return 1;
						case KillHordeMinionsStep.LearnKarma: return 2;
						default: return 5;
					}
				}
				else
				{
					return 5;
				}
			}
		}

		public override bool Completed
		{
			get
			{
				if ( m_Step == KillHordeMinionsStep.LearnKarma && HasBeenRead )
					return true;
				else
					return base.Completed;
			}
		}

		public KillHordeMinionsObjective()
		{
		}

		public KillHordeMinionsObjective( KillHordeMinionsStep step )
		{
			m_Step = step;
		}

		public override void RenderProgress( BaseQuestGump gump )
		{
			if ( !Completed )
			{
				gump.AddHtmlObject( 70, 260, 270, 100, 1049090, BaseQuestGump.Blue, false, false ); // Horde Minions killed:
				gump.AddLabel( 70, 280, 0x64, CurProgress.ToString() );
			}
			else
			{
				base.RenderProgress( gump );
			}
		}

		public override void OnRead()
		{
			CheckCompletionStatus();
		}

		public override bool IgnoreYoungProtection( Mobile from )
		{
			// This restriction continues until the quest is ended
			if ( from is HordeMinion && from.Map == Map.Trammel && from.X >= 3314 && from.X <= 3814 && from.Y >= 2345 && from.Y <= 3095 ) // Haven island
				return true;

			return false;
		}

		public override void OnKill( BaseCreature creature, Container corpse )
		{
			if ( creature is HordeMinion && corpse.Map == Map.Trammel && corpse.X >= 3314 && corpse.X <= 3814 && corpse.Y >= 2345 && corpse.Y <= 3095 ) // Haven island
			{
				if ( CurProgress == 0 )
					System.From.Send( new DisplayHelpTopic( 29, false ) ); // HEALING

				CurProgress++;
			}
		}

		public override void OnComplete()
		{
			if ( System.From.Profession == 5 )
			{
				switch ( m_Step )
				{
					case KillHordeMinionsStep.First:
					{
						QuestObjective obj = new KillHordeMinionsObjective( KillHordeMinionsStep.LearnKarma );
						System.AddObjective( obj );
						obj.CurProgress = CurProgress;
						break;
					}
					case KillHordeMinionsStep.LearnKarma:
					{
						QuestObjective obj = new KillHordeMinionsObjective( KillHordeMinionsStep.Others );
						System.AddObjective( obj );
						obj.CurProgress = CurProgress;
						break;
					}
					default:
					{
						System.AddObjective( new FindUzeraanAboutReportObjective() );
						break;
					}
				}
			}
			else
			{
				System.AddObjective( new FindUzeraanAboutReportObjective() );
			}
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_Step = (KillHordeMinionsStep) reader.ReadEncodedInt();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.WriteEncodedInt( (int) m_Step );
		}
	}

	public class FindUzeraanAboutReportObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* It's no use... The <I>Horde Minions</I> are too many.
				 * They are appearing out of nowhere.<BR><BR>
				 * 
				 * Return to Uzeraan and report your findings.
				 */
				return 1049091;
			}
		}

		public FindUzeraanAboutReportObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new UzeraanReportConversation() );
		}
	}

	public class FindSchmendrickObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Prepare for battle and step onto the teleporter,
				 * located against the wall in the main hall of Uzeraan's mansion.<BR><BR>
				 * 
				 * Find Schmendrick within the mines.
				 */
				return 1049120;
			}
		}

		public FindSchmendrickObjective()
		{
		}

		public override bool IgnoreYoungProtection( Mobile from )
		{
			// This restriction begins when this objective is completed, and continues until the quest is ended
			if ( Completed && from is RestlessSoul && from.Map == Map.Trammel && from.X >= 5199 && from.X <= 5271 && from.Y >= 1812 && from.Y <= 1865 ) // Schmendrick's cave
				return true;

			return false;
		}

		public override void OnComplete()
		{
			System.AddConversation( new SchmendrickConversation() );
		}
	}

	public class FindApprenticeObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Find Schmendrick's apprentice who is somewhere in the mining cave.
				 * The apprentice has the scroll of power needed by Uzeraan.
				 */
				return 1049323;
			}
		}

		public FindApprenticeObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddObjective( new ReturnScrollOfPowerObjective() );
		}
	}

	public class ReturnScrollOfPowerObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* You have obtained the scroll of power!  Find your way out of the cave.<BR><BR>
				 * 
				 * Hand the scroll to Uzeraan (drag and drop) once you arrive in his mansion.
				 */
				return 1049324;
			}
		}

		public ReturnScrollOfPowerObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new UzeraanScrollOfPowerConversation() );
		}
	}

	public class FindDryadObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Find the Dryad in the woods of Haven and get a patch
				 * of fertile dirt from her.<BR><BR>
				 * 
				 * Use Uzeraan's teleporter to get there if necessary.
				 */
				return 1049358;
			}
		}

		public FindDryadObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new DryadConversation() );
		}
	}

	public class ReturnFertileDirtObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* You have acquired the <I>Fertile Dirt</I>!<BR><BR>
				 * 
				 * Return to the mansion (<a href = "?ForceTopic13">North-East</a>
				 * of the Dryad's Grove) and hand it to Uzeraan.
				 */
				return 1049327;
			}
		}

		public ReturnFertileDirtObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new UzeraanFertileDirtConversation() );
		}
	}

	public class GetDaemonBloodObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Bring back a vial of blood.<BR><BR>
				 * 
				 * Follow the road leading north from the mansion and walk into the hut
				 * to find the chest that contains the vial
				 */
				return 1049361;
			}
		}

		private bool m_Ambushed;

		public GetDaemonBloodObjective()
		{
		}

		public override void CheckProgress()
		{
			PlayerMobile player = System.From;

			if ( !m_Ambushed && player.Map == Map.Trammel && player.InRange( new Point3D( 3456, 2558, 50 ), 30 ) )
			{
				int x = player.X - 1;
				int y = player.Y - 2;
				int z = Map.Trammel.GetAverageZ( x, y );

				if ( Map.Trammel.CanSpawnMobile( x, y, z ) )
				{
					m_Ambushed = true;

					player.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1049330 ); // You have been ambushed! Fight for your honor!!!

					BaseCreature creature = new HordeMinion();
					creature.MoveToWorld( new Point3D( x, y, z ), Map.Trammel );
					creature.Combatant = player;
				}
			}
		}

		public override void OnComplete()
		{
			System.AddObjective( new ReturnDaemonBloodObjective() );
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_Ambushed = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (bool) m_Ambushed );
		}
	}

	public class ReturnDaemonBloodObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* You have the vial of blood!<BR><BR>
				 * 
				 * Return to Uzeraan's mansion and hand him the vial.
				 */
				return 1049332;
			}
		}

		public ReturnDaemonBloodObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new UzeraanDaemonBloodConversation() );
		}
	}

	public class GetDaemonBoneObjective : QuestObjective
	{
		private Container m_CorpseWithBone;

		public Container CorpseWithBone
		{
			get { return m_CorpseWithBone; }
			set { m_CorpseWithBone = value; }
		}

		public override object Message
		{
			get
			{
				if ( System.From.Profession == 5 )
				{
					/* Use your <a href="?ForceTopic108">Sacred Journey</a>
					 * ability on the rune to the <a href="?ForceTopic13">North</a>
					 * of Uzeraan to travel to the graveyard.
					 */
					return 1060755;
				}
				else
				{
					/* Use Uzeraan's teleporter to get to the Haunted graveyard.<BR><BR>
					 * 
					 * Slay the undead until you find a <I>Daemon Bone</I>.
					 */
					return 1049362;
				}
			}
		}

		public GetDaemonBoneObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddObjective( new ReturnDaemonBoneObjective() );
		}

		public override bool IgnoreYoungProtection( Mobile from )
		{
			// This restriction continues until the end of the quest
			if ( ( from is Zombie || from is Skeleton ) && from.Map == Map.Trammel && from.X >= 3391 && from.X <= 3424 && from.Y >= 2639 && from.Y <= 2664 ) // Haven graveyard
				return true;

			return false;
		}

		public override bool GetKillEvent( BaseCreature creature, Container corpse )
		{
			if ( base.GetKillEvent( creature, corpse ) )
				return true;

			return UzeraanTurmoilQuest.HasLostDaemonBone( System.From );
		}

		public override void OnKill( BaseCreature creature, Container corpse )
		{
			if ( ( creature is Zombie || creature is Skeleton ) && corpse.Map == Map.Trammel && corpse.X >= 3391 && corpse.X <= 3424 && corpse.Y >= 2639 && corpse.Y <= 2664 ) // Haven graveyard
			{
				if ( Utility.RandomDouble() < 0.25 )
					m_CorpseWithBone = corpse;
			}
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_CorpseWithBone = (Container) reader.ReadItem();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			if ( m_CorpseWithBone != null && m_CorpseWithBone.Deleted )
				m_CorpseWithBone = null;

			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (Item) m_CorpseWithBone );
		}
	}

	public class ReturnDaemonBoneObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Head <a href = "?ForceTopic13">East</a> of here (or use the Horn of Retreat)
				 * to return to Uzeraan's Mansion and deliver the bone to Uzeraan.
				 */
				return 1049334;
			}
		}

		public ReturnDaemonBoneObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new UzeraanDaemonBoneConversation() );
		}
	}

	public class CashBankCheckObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Locate the Haven Bank (use the teleporter in Uzeraan's Mansion
				 * if necessary), which lies <a href = "?ForceTopic13">South-East</a>
				 * of Uzeraan's Mansion.  Once there, <a href="?ForceTopic86">cash your check</a>.
				 */
				return 1049336;
			}
		}

		public CashBankCheckObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new BankerConversation() );
		}
	}
}