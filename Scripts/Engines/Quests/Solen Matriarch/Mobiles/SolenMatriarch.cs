using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.ContextMenus;
using Server.Engines.Plants;
using Server.Engines.Quests;

namespace Server.Engines.Quests.Matriarch
{
	public abstract class BaseSolenMatriarch : BaseQuester
	{
		public abstract bool RedSolen{ get; }

		public override bool DisallowAllMoves{ get{ return false; } }

		public BaseSolenMatriarch()
		{
			Name = "the solen matriarch";

			Body = 0x328;

			if ( !RedSolen )
				Hue = 0x44E;

			SpeechHue = 0;
		}

		public override int GetIdleSound()
		{
			return 0x10D;
		}

		public override bool CanTalkTo( PlayerMobile to )
		{
			if ( SolenMatriarchQuest.IsFriend( to, this.RedSolen ) )
				return true;

			SolenMatriarchQuest qs = to.Quest as SolenMatriarchQuest;

			return qs != null && qs.RedSolen == this.RedSolen;
		}

		public override void OnTalk( PlayerMobile player, bool contextMenu )
		{
			this.Direction = GetDirectionTo( player );

			SolenMatriarchQuest qs = player.Quest as SolenMatriarchQuest;

			if ( qs != null && qs.RedSolen == this.RedSolen )
			{
				if ( qs.IsObjectiveInProgress( typeof( KillInfiltratorsObjective ) ) )
				{
					qs.AddConversation( new DuringKillInfiltratorsConversation() );
				}
				else
				{
					QuestObjective obj = qs.FindObjective( typeof( ReturnAfterKillsObjective ) );

					if ( obj != null && !obj.Completed )
					{
						obj.Complete();
					}
					else if ( qs.IsObjectiveInProgress( typeof( GatherWaterObjective ) ) )
					{
						qs.AddConversation( new DuringWaterGatheringConversation() );
					}
					else
					{
						obj = qs.FindObjective( typeof( ReturnAfterWaterObjective ) );

						if ( obj != null && !obj.Completed )
						{
							obj.Complete();
						}
						else if ( qs.IsObjectiveInProgress( typeof( ProcessFungiObjective ) ) )
						{
							qs.AddConversation( new DuringFungiProcessConversation() );
						}
						else
						{
							obj = qs.FindObjective( typeof( GetRewardObjective ) );

							if ( obj != null && !obj.Completed )
							{
								if ( SolenMatriarchQuest.GiveRewardTo( player ) )
								{
									obj.Complete();
								}
								else
								{
									qs.AddConversation( new FullBackpackConversation( false ) );
								}
							}
						}
					}
				}
			}
			else if ( SolenMatriarchQuest.IsFriend( player, this.RedSolen ) )
			{
				QuestSystem newQuest = new SolenMatriarchQuest( player, this.RedSolen );

				if ( player.Quest == null && QuestSystem.CanOfferQuest( player, typeof( SolenMatriarchQuest ) ) )
				{
					newQuest.SendOffer();
				}
				else
				{
					newQuest.AddConversation( new DontOfferConversation( true ) );
				}
			}
		}

		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			PlayerMobile player = from as PlayerMobile;

			if ( player != null )
			{
				if ( dropped is Seed )
				{
					SolenMatriarchQuest qs = player.Quest as SolenMatriarchQuest;

					if ( qs != null && qs.RedSolen == this.RedSolen )
					{
						SayTo( player, 1054080 ); // Thank you for that plant seed. Those have such wonderful flavor.
					}
					else
					{
						QuestSystem newQuest = new SolenMatriarchQuest( player, this.RedSolen );

						if ( player.Quest == null && QuestSystem.CanOfferQuest( player, typeof( SolenMatriarchQuest ) ) )
						{
							newQuest.SendOffer();
						}
						else
						{
							newQuest.AddConversation( new DontOfferConversation( SolenMatriarchQuest.IsFriend( player, this.RedSolen ) ) );
						}
					}

					dropped.Delete();
					return true;
				}
				else if ( dropped is ZoogiFungus )
				{
					OnGivenFungi( player, (ZoogiFungus)dropped );

					return dropped.Deleted;
				}
			}

			return base.OnDragDrop( from, dropped );
		}

		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );

			if ( from.Alive )
			{
				PlayerMobile pm = from as PlayerMobile;

				if ( pm != null )
				{
					SolenMatriarchQuest qs = pm.Quest as SolenMatriarchQuest;

					if ( qs != null && qs.RedSolen == this.RedSolen )
					{
						if ( qs.IsObjectiveInProgress( typeof( ProcessFungiObjective ) ) )
						{
							list.Add( new ProcessZoogiFungusEntry( this, pm ) );
						}
					}
				}
			}
		}

		private class ProcessZoogiFungusEntry : ContextMenuEntry
		{
			private BaseSolenMatriarch m_Matriarch;
			private PlayerMobile m_From;

			public ProcessZoogiFungusEntry( BaseSolenMatriarch matriarch, PlayerMobile from ) : base( 6184 )
			{
				m_Matriarch = matriarch;
				m_From = from;
			}

			public override void OnClick()
			{
				if ( m_From.Alive )
					m_From.Target = new ProcessFungiTarget( m_Matriarch, m_From );
			}
		}

		private class ProcessFungiTarget : Target
		{
			private BaseSolenMatriarch m_Matriarch;
			private PlayerMobile m_From;

			public ProcessFungiTarget( BaseSolenMatriarch matriarch, PlayerMobile from ) : base( -1, false, TargetFlags.None )
			{
				m_Matriarch = matriarch;
				m_From = from;
			}

			protected override void OnTargetCancel( Mobile from, TargetCancelType cancelType )
			{
				from.SendLocalizedMessage( 1042021, "", 0x59 ); // Cancelled.
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( targeted is ZoogiFungus )
				{
					ZoogiFungus fungus = (ZoogiFungus)targeted;

					if ( fungus.IsChildOf( m_From.Backpack ) )
						m_Matriarch.OnGivenFungi( m_From, (ZoogiFungus)targeted );
					else
						m_From.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
				}
			}
		}

		public void OnGivenFungi( PlayerMobile player, ZoogiFungus fungi )
		{
			this.Direction = GetDirectionTo( player );

			SolenMatriarchQuest qs = player.Quest as SolenMatriarchQuest;

			if ( qs != null && qs.RedSolen == this.RedSolen )
			{
				QuestObjective obj = qs.FindObjective( typeof( ProcessFungiObjective ) );

				if ( obj != null && !obj.Completed )
				{
					int amount = fungi.Amount / 2;

					if ( amount > 100 )
						amount = 100;

					if ( amount > 0 )
					{
						if ( amount * 2 >= fungi.Amount )
							fungi.Delete();
						else
							fungi.Amount -= amount * 2;

						PowderOfTranslocation powder = new PowderOfTranslocation( amount );
						player.AddToBackpack( powder );

						player.SendLocalizedMessage( 1054100 ); // You receive some powder of translocation.

						obj.Complete();
					}
				}
			}
		}

		public BaseSolenMatriarch( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class RedSolenMatriarch : BaseSolenMatriarch
	{
		public override bool RedSolen{ get{ return true; } }

		[Constructable]
		public RedSolenMatriarch()
		{
		}

		public RedSolenMatriarch( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class BlackSolenMatriarch : BaseSolenMatriarch
	{
		public override bool RedSolen{ get{ return false; } }

		[Constructable]
		public BlackSolenMatriarch()
		{
		}

		public BlackSolenMatriarch( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}