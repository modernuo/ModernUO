using System;
using Server.Mobiles;
using Server.Items;

namespace Server.Engines.Quests.Ambitious
{
	public class AmbitiousQueenQuest : QuestSystem
	{
		private static Type[] m_TypeReferenceTable = new[]
			{
				typeof( Ambitious.DontOfferConversation ),
				typeof( Ambitious.AcceptConversation ),
				typeof( Ambitious.DuringKillQueensConversation ),
				typeof( Ambitious.GatherFungiConversation ),
				typeof( Ambitious.DuringFungiGatheringConversation ),
				typeof( Ambitious.EndConversation ),
				typeof( Ambitious.FullBackpackConversation ),
				typeof( Ambitious.End2Conversation ),
				typeof( Ambitious.KillQueensObjective ),
				typeof( Ambitious.ReturnAfterKillsObjective ),
				typeof( Ambitious.GatherFungiObjective ),
				typeof( Ambitious.GetRewardObjective )
			};

		public override Type[] TypeReferenceTable => m_TypeReferenceTable;

		public override object Name => 1054146;

		public override object OfferMessage => 1054060;

		public override TimeSpan RestartDelay => TimeSpan.Zero;
		public override bool IsTutorial => false;

		public override int Picture => 0x15C9;

		private bool m_RedSolen;

		public bool RedSolen => m_RedSolen;

		public AmbitiousQueenQuest( PlayerMobile from, bool redSolen ) : base( from )
		{
			m_RedSolen = redSolen;
		}

		// Serialization
		public AmbitiousQueenQuest()
		{
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_RedSolen = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (bool) m_RedSolen );
		}

		public override void Accept()
		{
			base.Accept();

			AddConversation( new AcceptConversation() );
		}

		public static void GiveRewardTo( PlayerMobile player, ref bool bagOfSending, ref bool powderOfTranslocation, ref bool gold )
		{
			if ( bagOfSending )
			{
				Item reward = new BagOfSending();

				if ( player.PlaceInBackpack( reward ) )
				{
					player.SendLocalizedMessage( 1054074, "", 0x59 ); // You have been given a bag of sending.
					bagOfSending = false;
				}
				else
				{
					reward.Delete();
				}
			}

			if ( powderOfTranslocation )
			{
				Item reward = new PowderOfTranslocation( Utility.RandomMinMax( 10, 12 ) );

				if ( player.PlaceInBackpack( reward ) )
				{
					player.SendLocalizedMessage( 1054075, "", 0x59 ); // You have been given some powder of translocation.
					powderOfTranslocation = false;
				}
				else
				{
					reward.Delete();
				}
			}

			if ( gold )
			{
				Item reward = new Gold( Utility.RandomMinMax( 250, 350 ) );

				if ( player.PlaceInBackpack( reward ) )
				{
					player.SendLocalizedMessage( 1054076, "", 0x59 ); // You have been given some gold.
					gold = false;
				}
				else
				{
					reward.Delete();
				}
			}
		}
	}
}
