using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven
{
	public class UzeraanTurmoilQuest : QuestSystem
	{
		private static Type[] m_TypeReferenceTable = new Type[]
			{
				typeof( Haven.AcceptConversation ),
				typeof( Haven.UzeraanTitheConversation ),
				typeof( Haven.UzeraanFirstTaskConversation ),
				typeof( Haven.UzeraanReportConversation ),
				typeof( Haven.SchmendrickConversation ),
				typeof( Haven.UzeraanScrollOfPowerConversation ),
				typeof( Haven.DryadConversation ),
				typeof( Haven.UzeraanFertileDirtConversation ),
				typeof( Haven.UzeraanDaemonBloodConversation ),
				typeof( Haven.UzeraanDaemonBoneConversation ),
				typeof( Haven.BankerConversation ),
				typeof( Haven.RadarConversation ),
				typeof( Haven.LostScrollOfPowerConversation ),
				typeof( Haven.LostFertileDirtConversation ),
				typeof( Haven.DryadAppleConversation ),
				typeof( Haven.LostDaemonBloodConversation ),
				typeof( Haven.LostDaemonBoneConversation ),
				typeof( Haven.FindUzeraanBeginObjective ),
				typeof( Haven.TitheGoldObjective ),
				typeof( Haven.FindUzeraanFirstTaskObjective ),
				typeof( Haven.KillHordeMinionsObjective ),
				typeof( Haven.FindUzeraanAboutReportObjective ),
				typeof( Haven.FindSchmendrickObjective ),
				typeof( Haven.FindApprenticeObjective ),
				typeof( Haven.ReturnScrollOfPowerObjective ),
				typeof( Haven.FindDryadObjective ),
				typeof( Haven.ReturnFertileDirtObjective ),
				typeof( Haven.GetDaemonBloodObjective ),
				typeof( Haven.ReturnDaemonBloodObjective ),
				typeof( Haven.GetDaemonBoneObjective ),
				typeof( Haven.ReturnDaemonBoneObjective ),
				typeof( Haven.CashBankCheckObjective ),
				typeof( Haven.FewReagentsConversation )
			};

		public override Type[] TypeReferenceTable => m_TypeReferenceTable;

		public override object Name => 1049007;

		public override object OfferMessage => 1049008;

		public override TimeSpan RestartDelay => TimeSpan.MaxValue;
		public override bool IsTutorial => true;

		public override int Picture
		{
			get
			{
				switch ( From.Profession )
				{
					case 1: return 0x15C9; // warrior
					case 2: return 0x15C1; // magician
					default: return 0x15D3; // paladin
				}
			}
		}

		private bool m_HasLeftTheMansion;

		public override void Slice()
		{
			if ( !m_HasLeftTheMansion && ( From.Map != Map.Trammel || From.X < 3573 || From.X > 3611 || From.Y < 2568 || From.Y > 2606 ) )
			{
				m_HasLeftTheMansion = true;
				AddConversation( new RadarConversation() );
			}

			base.Slice();
		}

		public UzeraanTurmoilQuest( PlayerMobile from ) : base( from )
		{
		}

		// Serialization
		public UzeraanTurmoilQuest()
		{
		}

		public override void Accept()
		{
			base.Accept();

			AddConversation( new AcceptConversation() );
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_HasLeftTheMansion = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (bool) m_HasLeftTheMansion );
		}

		public static bool HasLostScrollOfPower( Mobile from )
		{
			if ( !(from is PlayerMobile pm) )
				return false;

			QuestSystem qs = pm.Quest;

			if ( qs is UzeraanTurmoilQuest )
			{
				if ( qs.IsObjectiveInProgress( typeof( ReturnScrollOfPowerObjective ) ) )
				{
					return ( from.Backpack?.FindItemByType( typeof( SchmendrickScrollOfPower ) ) == null );
				}
			}

			return false;
		}

		public static bool HasLostFertileDirt( Mobile from )
		{
			if ( !(from is PlayerMobile pm) )
				return false;

			QuestSystem qs = pm.Quest;

			if ( qs is UzeraanTurmoilQuest )
			{
				if ( qs.IsObjectiveInProgress( typeof( ReturnFertileDirtObjective ) ) )
				{
					return ( from.Backpack?.FindItemByType( typeof( QuestFertileDirt ) ) == null );
				}
			}

			return false;
		}

		public static bool HasLostDaemonBlood( Mobile from )
		{
			if ( !(from is PlayerMobile pm) )
				return false;

			QuestSystem qs = pm.Quest;

			if ( qs is UzeraanTurmoilQuest )
			{
				if ( qs.IsObjectiveInProgress( typeof( ReturnDaemonBloodObjective ) ) )
				{
					return ( from.Backpack?.FindItemByType( typeof( QuestDaemonBlood ) ) == null );
				}
			}

			return false;
		}

		public static bool HasLostDaemonBone( Mobile from )
		{
			if ( !(from is PlayerMobile pm) )
				return false;

			QuestSystem qs = pm.Quest;

			if ( qs is UzeraanTurmoilQuest )
			{
				if ( qs.IsObjectiveInProgress( typeof( ReturnDaemonBoneObjective ) ) )
				{
					return ( from.Backpack?.FindItemByType( typeof( QuestDaemonBone ) ) == null );
				}
			}

			return false;
		}
	}
}
