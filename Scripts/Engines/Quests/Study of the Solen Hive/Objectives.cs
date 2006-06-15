using System;
using System.Collections;
using Server;
using Server.Mobiles;

namespace Server.Engines.Quests.Naturalist
{
	public class StudyNestsObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Find an entrance to the Solen Hive, and search within for the Solen
				 * Egg Nests. Each Nest must be studied for some time without a break in
				 * concentration in order to gather useful information.<BR><BR>
				 * 
				 * Once you have completed your study of the Nests, return to the Naturalist
				 * who gave you this task.
				 */
				return 1054044;
			}
		}

		public override int MaxProgress{ get{ return NestArea.NonSpecialCount; } }

		private NestArea m_CurrentNest;
		private DateTime m_StudyBegin;

		private enum StudyState { Inactive, FirstStep, SecondStep }
		private StudyState m_StudyState;

		private ArrayList m_StudiedNests;
		private bool m_StudiedSpecialNest;

		public bool StudiedSpecialNest{ get{ return m_StudiedSpecialNest; } }

		public StudyNestsObjective()
		{
			m_StudiedNests = new ArrayList();
		}

		public override bool GetTimerEvent()
		{
			return true;
		}

		public override void CheckProgress()
		{
			PlayerMobile from = System.From;

			if ( m_CurrentNest != null )
			{
				NestArea nest = m_CurrentNest;

				if ( ( from.Map == Map.Trammel || from.Map == Map.Felucca ) && nest.Contains( from ) )
				{
					if ( m_StudyState != StudyState.Inactive )
					{
						TimeSpan time = DateTime.Now - m_StudyBegin;

						if ( time > TimeSpan.FromSeconds( 30.0 ) )
						{
							m_StudiedNests.Add( nest );
							m_StudyState = StudyState.Inactive;

							if ( m_CurrentNest.Special )
							{
								from.SendLocalizedMessage( 1054057 ); // You complete your examination of this bizarre Egg Nest. The Naturalist will undoubtedly be quite interested in these notes!
								m_StudiedSpecialNest = true;
							}
							else
							{
								from.SendLocalizedMessage( 1054054 ); // You have completed your study of this Solen Egg Nest. You put your notes away.
								CurProgress++;
							}
						}
						else if ( m_StudyState == StudyState.FirstStep && time > TimeSpan.FromSeconds( 15.0 ) )
						{
							if ( !nest.Special )
								from.SendLocalizedMessage( 1054058 ); // You begin recording your completed notes on a bit of parchment.

							m_StudyState = StudyState.SecondStep;
						}
					}
				}
				else
				{
					if ( m_StudyState != StudyState.Inactive )
						from.SendLocalizedMessage( 1054046 ); // You abandon your study of the Solen Egg Nest without gathering the needed information.

					m_CurrentNest = null;
				}
			}
			else if ( from.Map == Map.Trammel || from.Map == Map.Felucca )
			{
				NestArea nest = NestArea.Find( from );

				if ( nest != null )
				{
					m_CurrentNest = nest;
					m_StudyBegin = DateTime.Now;

					if ( m_StudiedNests.Contains( nest ) )
					{
						m_StudyState = StudyState.Inactive;

						from.SendLocalizedMessage( 1054047 ); // You glance at the Egg Nest, realizing you've already studied this one.
					}
					else
					{
						m_StudyState = StudyState.FirstStep;

						if ( nest.Special )
							from.SendLocalizedMessage( 1054056 ); // You notice something very odd about this Solen Egg Nest. You begin taking notes.
						else
							from.SendLocalizedMessage( 1054045 ); // You begin studying the Solen Egg Nest to gather information.

						if ( from.Female )
							from.PlaySound( 0x30B );
						else
							from.PlaySound( 0x419 );
					}
				}
			}
		}

		public override void RenderProgress( BaseQuestGump gump )
		{
			if ( !Completed )
			{
				gump.AddHtmlLocalized( 70, 260, 270, 100, 1054055, BaseQuestGump.Blue, false, false ); // Solen Nests Studied :
				gump.AddLabel( 70, 280, 0x64, CurProgress.ToString() );
				gump.AddLabel( 100, 280, 0x64, "/" );
				gump.AddLabel( 130, 280, 0x64, MaxProgress.ToString() );
			}
			else
			{
				base.RenderProgress( gump );
			}
		}

		public override void OnComplete()
		{
			System.AddObjective( new ReturnToNaturalistObjective() );
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			int count = reader.ReadEncodedInt();
			for ( int i = 0; i < count; i++ )
			{
				NestArea nest = NestArea.GetByID( reader.ReadEncodedInt() );
				m_StudiedNests.Add( nest );
			}

			m_StudiedSpecialNest = reader.ReadBool();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.WriteEncodedInt( (int) m_StudiedNests.Count );
			foreach ( NestArea nest in m_StudiedNests )
			{
				writer.WriteEncodedInt( (int) nest.ID );
			}

			writer.Write( (bool) m_StudiedSpecialNest );
		}
	}

	public class ReturnToNaturalistObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* You have studied enough Solen Egg Nests to gather a fair amount of
				 * useful information. Return to the Naturalist who gave you this task.
				 */
				return 1054048;
			}
		}

		public ReturnToNaturalistObjective()
		{
		}

		public override void RenderProgress( BaseQuestGump gump )
		{
			string count = NestArea.NonSpecialCount.ToString();

			gump.AddHtmlLocalized( 70, 260, 270, 100, 1054055, BaseQuestGump.Blue, false, false ); // Solen Nests Studied :
			gump.AddLabel( 70, 280, 0x64, count );
			gump.AddLabel( 100, 280, 0x64, "/" );
			gump.AddLabel( 130, 280, 0x64, count );
		}
	}
}