namespace Server.Engines.Quests.Matriarch
{
	public class DontOfferConversation : QuestConversation
	{
		private bool m_Friend;

		public override object Message
		{
			get
			{
				if ( m_Friend )
				{
					/* <I>The Solen Matriarch smiles as you greet her.</I><BR><BR>
					 *
					 * It is good to see you again. I would offer to process some zoogi fungus for you,
					 * but you seem to be busy with another task at the moment. Perhaps you should
					 * finish whatever is occupying your attention at the moment and return to me once
					 * you're done.
					 */
					return 1054081;
				}

				/* <I>The Solen Matriarch smiles as she eats the seed you offered.</I><BR><BR>
					 *
					 * Thank you for that seed. It was quite delicious.  <BR><BR>
					 *
					 * I would offer to make you a friend of my colony, but you seem to be busy with
					 * another task at the moment. Perhaps you should finish whatever is occupying
					 * your attention at the moment and return to me once you're done.
					 */
				return 1054079;
			}
		}

		public override bool Logged => false;

		public DontOfferConversation( bool friend )
		{
			m_Friend = friend;
		}

		public DontOfferConversation()
		{
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_Friend = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (bool) m_Friend );
		}
	}

	public class AcceptConversation : QuestConversation
	{
		public override object Message => 1054084;

		public AcceptConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new KillInfiltratorsObjective() );
		}
	}

	public class DuringKillInfiltratorsConversation : QuestConversation
	{
		public override object Message => 1054089;

		public override bool Logged => false;

		public DuringKillInfiltratorsConversation()
		{
		}
	}

	public class GatherWaterConversation : QuestConversation
	{
		public override object Message => 1054091;

		public GatherWaterConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new GatherWaterObjective() );
		}
	}

	public class DuringWaterGatheringConversation : QuestConversation
	{
		public override object Message => 1054094;

		public override bool Logged => false;

		public DuringWaterGatheringConversation()
		{
		}
	}

	public class ProcessFungiConversation : QuestConversation
	{
		private bool m_Friend;

		public override object Message
		{
			get
			{
				if ( m_Friend )
				{
					/* <I>The Solen Matriarch listens as you report the completion of your
					 * tasks to her.</I><BR><BR>
					 *
					 * I give you my thanks for your help, and I will gladly process some zoogi
					 * fungus into powder of translocation for you. Two of the zoogi fungi are
					 * required for each measure of the powder. I will process up to 200 zoogi fungi
					 * into 100 measures of powder of translocation.<BR><BR>
					 *
					 * I will also give you some gold for assisting me and my colony, but first let's
					 * take care of your zoogi fungus.
					 */
					return 1054097;
				}

				/* <I>The Solen Matriarch listens as you report the completion of your
					 * tasks to her.</I><BR><BR>
					 *
					 * I give you my thanks for your help, and I will gladly make you a friend of my
					 * solen colony. My warriors, workers, and queens will not longer look at you
					 * as an intruder and attack you when you enter our lair.<BR><BR>
					 *
					 * I will also process some zoogi fungus into powder of translocation for you.
					 * Two of the zoogi fungi are required for each measure of the powder. I will
					 * process up to 200 zoogi fungi into 100 measures of powder of translocation.<BR><BR>
					 *
					 * I will also give you some gold for assisting me and my colony, but first let's
					 * take care of your zoogi fungus.
					 */
				return 1054096;
			}
		}

		public ProcessFungiConversation( bool friend )
		{
			m_Friend = friend;
		}

		public override void OnRead()
		{
			System.AddObjective( new ProcessFungiObjective() );
		}

		public ProcessFungiConversation()
		{
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_Friend = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (bool) m_Friend );
		}
	}

	public class DuringFungiProcessConversation : QuestConversation
	{
		public override object Message => 1054099;

		public override bool Logged => false;

		public DuringFungiProcessConversation()
		{
		}
	}

	public class FullBackpackConversation : QuestConversation
	{
		public override object Message => 1054102;

		private bool m_Logged;

		public override bool Logged => m_Logged;

		public FullBackpackConversation( bool logged )
		{
			m_Logged = logged;
		}

		public FullBackpackConversation()
		{
			m_Logged = true;
		}

		public override void OnRead()
		{
			if ( m_Logged )
				System.AddObjective( new GetRewardObjective() );
		}
	}

	public class EndConversation : QuestConversation
	{
		public override object Message => 1054101;

		public EndConversation()
		{
		}

		public override void OnRead()
		{
			System.Complete();
		}
	}
}
