using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.ContextMenus;
using Server.Engines.Quests;
using Server.Engines.Quests.Necro;

namespace Server.Engines.Quests.Doom
{
	public class Victoria : BaseQuester
	{
		public override int TalkNumber{ get{ return 6159; } } // Ask about Chyloth
		public override bool ClickTitle{ get{ return true; } }
		public override bool IsActiveVendor{ get{ return true; } }
		public override bool DisallowAllMoves{ get{ return false; } }

		public override void InitSBInfo()
		{
			m_SBInfos.Add( new SBMage() );
		}

		[Constructable]
		public Victoria() : base( "the Sorceress" )
		{
		}

		public Victoria( Serial serial ) : base( serial )
		{
		}

		public override void InitBody()
		{
			InitStats( 100, 100, 25 );

			Female = true;
			Hue = 0x8835;
			Body = 0x191;

			Name = "Victoria";
		}

		private SummoningAltar m_Altar;

		private const int AltarRange = 24;

		public SummoningAltar Altar
		{
			get
			{
				if ( m_Altar == null || m_Altar.Deleted || m_Altar.Map != this.Map || !Utility.InRange( m_Altar.Location, this.Location, AltarRange ) )
				{
					foreach ( Item item in GetItemsInRange( AltarRange ) )
					{
						if ( item is SummoningAltar )
						{
							m_Altar = (SummoningAltar)item;
							break;
						}
					}
				}

				return m_Altar;
			}
		}

		public override void InitOutfit()
		{
			EquipItem( new GrandGrimoire() );

			EquipItem( SetHue( new Sandals(), 0x455 ) );
			EquipItem( SetHue( new SkullCap(), 0x455 ) );
			EquipItem( SetHue( new PlainDress(), 0x455 ) );

			HairItemID = 0x203C;
			HairHue = 0x482;
		}

		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			PlayerMobile player = from as PlayerMobile;

			if ( player != null )
			{
				QuestSystem qs = player.Quest;

				if ( qs is TheSummoningQuest )
				{
					if ( dropped is DaemonBone )
					{
						DaemonBone bones = (DaemonBone)dropped;

						QuestObjective obj = qs.FindObjective( typeof( CollectBonesObjective ) );

						if ( obj != null && !obj.Completed )
						{
							int need = obj.MaxProgress - obj.CurProgress;

							if ( bones.Amount < need )
							{
								obj.CurProgress += bones.Amount;
								bones.Delete();

								qs.ShowQuestLogUpdated();
							}
							else
							{
								obj.Complete();
								bones.Consume( need );

								if ( !bones.Deleted )
								{
									// TODO: Accurate?
									SayTo( from, 1050038 ); // You have already given me all the Daemon bones necessary to weave the spell.  Keep these for a later time.
								}
							}
						}
						else
						{
							// TODO: Accurate?
							SayTo( from, 1050038 ); // You have already given me all the Daemon bones necessary to weave the spell.  Keep these for a later time.
						}

						return false;
					}
				}
			}

			return base.OnDragDrop( from, dropped );
		}

		public override bool CanTalkTo( PlayerMobile to )
		{
			return ( to.Quest == null && QuestSystem.CanOfferQuest( to, typeof( TheSummoningQuest ) ) );
		}

		public override void OnTalk( PlayerMobile player, bool contextMenu )
		{
			QuestSystem qs = player.Quest;

			if ( qs == null && QuestSystem.CanOfferQuest( player, typeof( TheSummoningQuest ) ) )
			{
				Direction = GetDirectionTo( player );
				new TheSummoningQuest( this, player ).SendOffer();
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}