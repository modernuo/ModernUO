using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Naturalist
{
	public class StudyOfSolenQuest : QuestSystem
	{
		private static Type[] m_TypeReferenceTable = {
				typeof( StudyNestsObjective ),
				typeof( ReturnToNaturalistObjective ),
				typeof( DontOfferConversation ),
				typeof( AcceptConversation ),
				typeof( NaturalistDuringStudyConversation ),
				typeof( EndConversation ),
				typeof( SpecialEndConversation ),
				typeof( FullBackpackConversation )
			};

		public override Type[] TypeReferenceTable => m_TypeReferenceTable;

		public override object Name => 1054041;

		public override object OfferMessage => 1054042;

		public override TimeSpan RestartDelay => TimeSpan.Zero;
		public override bool IsTutorial => false;

		public override int Picture => 0x15C7;

		private Naturalist m_Naturalist;

		public Naturalist Naturalist => m_Naturalist;

		public StudyOfSolenQuest( PlayerMobile from, Naturalist naturalist ) : base( from )
		{
			m_Naturalist = naturalist;
		}

		// Serialization
		public StudyOfSolenQuest()
		{
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_Naturalist = (Naturalist) reader.ReadMobile();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (Mobile) m_Naturalist );
		}

		public override void Accept()
		{
			base.Accept();

			if ( m_Naturalist != null )
				m_Naturalist.PlaySound( 0x431 );

			AddConversation( new AcceptConversation() );
		}
	}
}
