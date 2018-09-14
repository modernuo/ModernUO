using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Doom
{
	public class TheSummoningQuest : QuestSystem
	{
		private static Type[] m_TypeReferenceTable = new[]
			{
				typeof( AcceptConversation ),
				typeof( CollectBonesObjective ),
				typeof( VanquishDaemonConversation ),
				typeof( VanquishDaemonObjective )
			};

		public override Type[] TypeReferenceTable => m_TypeReferenceTable;

		private Victoria m_Victoria;
		private bool m_WaitForSummon;

		public Victoria Victoria => m_Victoria;

		public bool WaitForSummon
		{
			get => m_WaitForSummon;
			set => m_WaitForSummon = value;
		}

		public override object Name => 1050025;

		public override object OfferMessage => 1050020;

		public override bool IsTutorial => false;
		public override TimeSpan RestartDelay => TimeSpan.Zero;
		public override int Picture => 0x15B5;

		// NOTE: Quest not entirely OSI-accurate: some changes made to prevent numerous OSI bugs

		public override void Slice()
		{
			if ( m_WaitForSummon && m_Victoria != null )
			{
				SummoningAltar altar = m_Victoria.Altar;

				if ( altar != null && (altar.Daemon == null || !altar.Daemon.Alive) )
				{
					if ( From.Map == m_Victoria.Map && From.InRange( m_Victoria, 8 ) )
					{
						m_WaitForSummon = false;

						AddConversation( new VanquishDaemonConversation() );
					}
				}
			}

			base.Slice();
		}

		public static int GetDaemonBonesFor( BaseCreature creature )
		{
			if ( creature == null || creature.Controlled || creature.Summoned )
				return 0;

			int fame = creature.Fame;

			if ( fame < 1500 )
				return Utility.Dice( 2, 5, -1 );
			else if ( fame < 20000 )
				return Utility.Dice( 2, 4, 8 );
			else
				return 50;
		}

		public TheSummoningQuest( Victoria victoria, PlayerMobile from ) : base( from )
		{
			m_Victoria = victoria;
		}

		public TheSummoningQuest()
		{
		}

		public override void Cancel()
		{
			base.Cancel();

			QuestObjective obj = FindObjective( typeof( CollectBonesObjective ) );

			if ( obj != null && obj.CurProgress > 0 )
			{
				From.BankBox.DropItem( new DaemonBone( obj.CurProgress ) );

				From.SendLocalizedMessage( 1050030 ); // The Daemon bones that you have thus far given to Victoria have been returned to you.
			}
		}

		public override void Accept()
		{
			base.Accept();

			AddConversation( new AcceptConversation() );
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_Victoria = reader.ReadMobile() as Victoria;
			m_WaitForSummon = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (Mobile) m_Victoria );
			writer.Write( (bool) m_WaitForSummon );
		}
	}
}
