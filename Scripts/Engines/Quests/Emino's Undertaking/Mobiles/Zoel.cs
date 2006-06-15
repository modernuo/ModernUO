using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Gumps;
using Server.Engines.Quests;

namespace Server.Engines.Quests.Ninja
{
	public class Zoel : BaseQuester
	{
		[Constructable]
		public Zoel() : base( "the Masterful Tactician" )
		{
		}

		public override void InitBody()
		{
			InitStats( 100, 100, 25 );

			Hue = 0x83FE;

			Female = false;
			Body = 0x190;
			Name = "Elite Ninja Zoel";
		}

		public override void InitOutfit()
		{
			HairItemID = 0x203B;
			HairHue = 0x901;

			AddItem( new HakamaShita( 0x1 ) );
			AddItem( new NinjaTabi() );
			AddItem( new TattsukeHakama() );
			AddItem( new Bandana() );

			AddItem( new LeatherNinjaBelt() );

			Tekagi tekagi = new Tekagi();
			tekagi.Movable = false;
			AddItem( tekagi );
		}

		public override int TalkNumber{ get	{ return -1; } }

		public override int GetAutoTalkRange( PlayerMobile pm )
		{
			return 2;
		}

		public override bool CanTalkTo( PlayerMobile to )
		{
			return to.Quest is EminosUndertakingQuest;
		}

		public override void OnTalk( PlayerMobile player, bool contextMenu )
		{
			QuestSystem qs = player.Quest;

			if ( qs is EminosUndertakingQuest )
			{
				QuestObjective obj = qs.FindObjective( typeof( FindZoelObjective ) );

				if ( obj != null && !obj.Completed )
					obj.Complete();
			}
		}

		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			PlayerMobile player = from as PlayerMobile;

			if ( player != null )
			{
				QuestSystem qs = player.Quest;

				if ( qs is EminosUndertakingQuest )
				{
					if ( dropped is NoteForZoel )
					{
						QuestObjective obj = qs.FindObjective( typeof( GiveZoelNoteObjective ) );

						if ( obj != null && !obj.Completed )
						{
							dropped.Delete();
							obj.Complete();
							return true;
						}
					}
				}
			}

			return base.OnDragDrop( from, dropped );
		}

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			base.OnMovement( m, oldLocation );

			if ( !m.Frozen && !m.Alive && InRange( m, 4 ) && !InRange( oldLocation, 4 ) && InLOS( m ) )
			{
				if ( m.Map == null || !m.Map.CanFit( m.Location, 16, false, false ) )
				{
					m.SendLocalizedMessage( 502391 ); // Thou can not be resurrected there!
				}
				else
				{
					Direction = GetDirectionTo( m );

					m.PlaySound( 0x214 );
					m.FixedEffect( 0x376A, 10, 16 );

					m.CloseGump( typeof( ResurrectGump ) );
					m.SendGump( new ResurrectGump( m, ResurrectMessage.Healer ) );
				}
			}
		}

		public Zoel( Serial serial ) : base( serial )
		{
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