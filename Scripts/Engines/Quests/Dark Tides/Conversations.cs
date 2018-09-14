using Server.Items;

namespace Server.Engines.Quests.Necro
{
	public class AcceptConversation : QuestConversation
	{
		public override object Message => 1049092;

		public AcceptConversation()
		{
		}

		public override void OnRead()
		{
			Container bag = BaseQuester.GetNewContainer();

			bag.DropItem( new DarkTidesHorn() );

			System.From.AddToBackpack( bag );

			System.AddConversation( new ReanimateMaabusConversation() );
		}
	}

	public class ReanimateMaabusConversation : QuestConversation
	{
		public override object Message => 1060099;

		private static QuestItemInfo[] m_Info = {
				new QuestItemInfo( 1026153, 6178 ), // teleporter
				new QuestItemInfo( 1049117, 4036 ), // Horn of Retreat
				new QuestItemInfo( 1048032, 3702 )  // a bag
			};

		public override QuestItemInfo[] Info => m_Info;

		public ReanimateMaabusConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindMaabusTombObjective() );
		}
	}

	public class MaabasConversation : QuestConversation
	{
		public override object Message => 1060103;

		private static QuestItemInfo[] m_Info = {
				new QuestItemInfo( 1026153, 6178 ) // teleporter
			};

		public override QuestItemInfo[] Info => m_Info;

		public MaabasConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindCrystalCaveObjective() );
		}
	}

	public class HorusConversation : QuestConversation
	{
		public override object Message => 1060105;

		public HorusConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindMardothAboutVaultObjective() );
		}
	}

	public class MardothVaultConversation : QuestConversation
	{
		public override object Message => 1060107;

		public MardothVaultConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindCityOfLightObjective() );
		}
	}

	public class VaultOfSecretsConversation : QuestConversation
	{
		public override object Message => 1060110;

		private static QuestItemInfo[] m_Info = {
				new QuestItemInfo( 1023643, 8787 ) // spellbook
			};

		public override QuestItemInfo[] Info => m_Info;

		public VaultOfSecretsConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FetchAbraxusScrollObjective() );
		}
	}

	public class ReadAbraxusScrollConversation : QuestConversation
	{
		public override object Message => 1060114;

		public ReadAbraxusScrollConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new ReadAbraxusScrollObjective() );
		}
	}

	public class SecondHorusConversation : QuestConversation
	{
		public override object Message => 1060118;

		public SecondHorusConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindCallingScrollObjective() );
		}
	}

	public class HealConversation : QuestConversation
	{
		public override object Message => 1061610;

		public HealConversation()
		{
		}
	}

	public class HorusRewardConversation : QuestConversation
	{
		public override object Message => 1060717;

		public override bool Logged => false;

		public HorusRewardConversation()
		{
		}
	}

	public class LostCallingScrollConversation : QuestConversation
	{
		private bool m_FromMardoth;

		public override object Message
		{
			get
			{
				if ( m_FromMardoth )
				{
					/* You return without the scroll of Calling?  I'm afraid that
					 * won't do.  You must return to the Crystal Cave and fetch
					 * another scroll.  Use the teleporter to the West of me to
					 * get there.  Return here when you have the scroll.  Do not
					 * fail me this time, young apprentice of evil.
					 */
					return 1062058;
				}

				/* You have arrived at the well, but no longer have the scroll
					 * of calling.  Use Mardoth's teleporter to return to the
					 * Crystal Cave and fetch another scroll from the box.
					 */
				return 1060129;
			}
		}

		public override bool Logged => false;

		public LostCallingScrollConversation( bool fromMardoth )
		{
			m_FromMardoth = fromMardoth;
		}

		// Serialization
		public LostCallingScrollConversation()
		{
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_FromMardoth = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (bool) m_FromMardoth );
		}
	}

	public class MardothKronusConversation : QuestConversation
	{
		public override object Message => 1060121;

		public MardothKronusConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindWellOfTearsObjective() );
		}
	}

	public class MardothEndConversation : QuestConversation
	{
		public override object Message => 1060133;

		public MardothEndConversation()
		{
		}

		public override void OnRead()
		{
			System.AddObjective( new FindBankObjective() );
		}
	}

	public class BankerConversation : QuestConversation
	{
		public override object Message => 1060137;

		public BankerConversation()
		{
		}

		public override void OnRead()
		{
			System.Complete();
		}
	}

	public class RadarConversation : QuestConversation
	{
		public override object Message => 1061692;

		public override bool Logged => false;

		public RadarConversation()
		{
		}
	}
}
