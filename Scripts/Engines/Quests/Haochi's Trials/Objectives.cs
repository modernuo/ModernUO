using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai
{
	public class FindHaochiObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Speak to Daimyo Haochi.
				return 1063026;
			}
		}

		public FindHaochiObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new FirstTrialIntroConversation() );
		}
	}

	public class FirstTrialIntroObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Follow the green path. The guards will now let you through.
				return 1063030;
			}
		}

		public FirstTrialIntroObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new FirstTrialKillConversation() );
		}
	}

	public class FirstTrialKillObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Kill 3 Young Ronin or 3 Cursed Souls. Return to Daimyo Haochi when you have finished.
				return 1063032;
			}
		}

		public FirstTrialKillObjective()
		{
		}

		private int m_CursedSoulsKilled;
		private int m_YoungRoninKilled;

		public override int MaxProgress{ get{ return 3; } }

		public override void OnKill( BaseCreature creature, Container corpse )
		{
			if ( creature is CursedSoul )
			{
				if ( m_CursedSoulsKilled == 0 )
					System.AddConversation( new GainKarmaConversation( true ) );

				m_CursedSoulsKilled++;

				// Cursed Souls killed:  ~1_COUNT~
				System.From.SendLocalizedMessage( 1063038, m_CursedSoulsKilled.ToString() );
			}
			else if ( creature is YoungRonin )
			{
				if ( m_YoungRoninKilled == 0 )
					System.AddConversation( new GainKarmaConversation( false ) );

				m_YoungRoninKilled++;

				// Young Ronin killed:  ~1_COUNT~
				System.From.SendLocalizedMessage( 1063039, m_YoungRoninKilled.ToString() );
			}

			CurProgress = Math.Max( m_CursedSoulsKilled, m_YoungRoninKilled );
		}

		public override void OnComplete()
		{
			System.AddObjective( new FirstTrialReturnObjective( m_CursedSoulsKilled > m_YoungRoninKilled ) );
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_CursedSoulsKilled = reader.ReadEncodedInt();
			m_YoungRoninKilled = reader.ReadEncodedInt();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.WriteEncodedInt( m_CursedSoulsKilled );
			writer.WriteEncodedInt( m_YoungRoninKilled );
		}
	}

	public class FirstTrialReturnObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// The first trial is complete. Return to Daimyo Haochi.
				return 1063044;
			}
		}

		bool m_CursedSoul;

		public FirstTrialReturnObjective( bool cursedSoul )
		{
			m_CursedSoul = cursedSoul;
		}

		public FirstTrialReturnObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new SecondTrialIntroConversation( m_CursedSoul ) );
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_CursedSoul = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (bool) m_CursedSoul );
		}
	}

	public class SecondTrialIntroObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Follow the yellow path. The guards will now let you through.
				return 1063047;
			}
		}

		public SecondTrialIntroObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new SecondTrialAttackConversation() );
		}
	}

	public class SecondTrialAttackObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Choose your opponent and attack one with all your skill.
				return 1063058;
			}
		}

		public SecondTrialAttackObjective()
		{
		}
	}

	public class SecondTrialReturnObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// The second trial is complete.  Return to Daimyo Haochi.
				return 1063229;
			}
		}

		private bool m_Dragon;

		public bool Dragon{ get{ return m_Dragon; } }

		public SecondTrialReturnObjective( bool dragon )
		{
			m_Dragon = dragon;
		}

		public SecondTrialReturnObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new ThirdTrialIntroConversation( m_Dragon ) );
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_Dragon = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (bool) m_Dragon );
		}
	}

	public class ThirdTrialIntroObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* The next trial will test your benevolence. Follow the blue path.
				 * The guards will now let you through.
				 */
				return 1063061;
			}
		}

		public ThirdTrialIntroObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new ThirdTrialKillConversation() );
		}
	}

	public class ThirdTrialKillObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Use your Honorable Execution skill to finish off the wounded wolf.
				 * Double click the icon in your Book of Bushido to activate the skill.
				 * When you are done, return to Daimyo Haochi.
				 */
				return 1063063;
			}
		}

		public ThirdTrialKillObjective()
		{
		}

		public override void OnKill( BaseCreature creature, Container corpse )
		{
			if ( creature is InjuredWolf )
				Complete();
		}

		public override void OnComplete()
		{
			System.AddObjective( new ThirdTrialReturnObjective() );
		}
	}

	public class ThirdTrialReturnObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Return to Daimyo Haochi.
				return 1063064;
			}
		}

		public ThirdTrialReturnObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new FourthTrialIntroConversation() );
		}
	}

	public class FourthTrialIntroObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Follow the red path and pass through the guards to the entrance of the fourth trial.
				return 1063066;
			}
		}

		public FourthTrialIntroObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new FourthTrialCatsConversation() );
		}
	}

	public class FourthTrialCatsObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Give the gypsy gold or hunt one of the cats to eliminate the undue
				 * need it has placed on the gypsy.
				 */
				return 1063068;
			}
		}

		public FourthTrialCatsObjective()
		{
		}

		public override void OnKill( BaseCreature creature, Container corpse )
		{
			if ( creature is DiseasedCat )
			{
				Complete();
				System.AddObjective( new FourthTrialReturnObjective( true ) );
			}
		}
	}

	public class FourthTrialReturnObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// You have made your choice.  Return now to Daimyo Haochi.
				return 1063242;
			}
		}

		private bool m_KilledCat;

		public bool KilledCat{ get{ return m_KilledCat; } }

		public FourthTrialReturnObjective( bool killedCat )
		{
			m_KilledCat = killedCat;
		}

		public FourthTrialReturnObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new FifthTrialIntroConversation( m_KilledCat ) );
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_KilledCat = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (bool) m_KilledCat );
		}
	}

	public class FifthTrialIntroObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Retrieve Daimyo Haochi’s katana from the treasure room.
				return 1063072;
			}
		}

		private bool m_StolenTreasure;

		public bool StolenTreasure{ get{ return m_StolenTreasure; } set{ m_StolenTreasure = value; } }

		public FifthTrialIntroObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new FifthTrialReturnConversation() );
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_StolenTreasure = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (bool) m_StolenTreasure );
		}
	}

	public class FifthTrialReturnObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Give the sword to Daimyo Haochi.
				return 1063073;
			}
		}

		public FifthTrialReturnObjective()
		{
		}
	}

	public class SixthTrialIntroObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Light one of the candles near the altar and return to Daimyo Haochi.
				return 1063078;
			}
		}

		public SixthTrialIntroObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddObjective( new SixthTrialReturnObjective() );
		}
	}

	public class SixthTrialReturnObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// You have done as requested.  Return to Daimyo Haochi.
				return 1063252;
			}
		}

		public SixthTrialReturnObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new SeventhTrialIntroConversation() );
		}
	}

	public class SeventhTrialIntroObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Three young Ninja must be dealt with. Your job is to kill them.
				 * When you have done so, return to Daimyo Haochi.
				 */
				return 1063080;
			}
		}

		public override int MaxProgress{ get{ return 3; } }

		public SeventhTrialIntroObjective()
		{
		}

		public override void OnKill( BaseCreature creature, Container corpse )
		{
			if ( creature is YoungNinja )
				CurProgress++;
		}

		public override void OnComplete()
		{
			System.AddObjective( new SeventhTrialReturnObjective() );
		}
	}

	public class SeventhTrialReturnObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// The executions are complete.  Return to the Daimyo.
				return 1063253;
			}
		}

		public SeventhTrialReturnObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new EndConversation() );
		}
	}
}