using System;
using System.Collections.Generic;
using System.Text;
using Server;
using Server.Engines.MLQuests.Gumps;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.MLQuests.Mobiles
{
	public class SirHelper : Mage
	{
		private static readonly Gump m_Gump = new InfoNPCGump( 1078029, 1078028 );
		private static readonly TimeSpan m_ShoutDelay = TimeSpan.FromSeconds( 20 );
		private static readonly TimeSpan m_ShoutCooldown = TimeSpan.FromDays( 1 ); // TODO: Verify, could be a lot longer... or until a restart even

		private DateTime m_NextShout;

		public override bool IsActiveVendor { get { return false; } }

		[Constructable]
		public SirHelper()
		{
			Name = "Sir Helper";
			Title = "the Profession Guide"; // TODO: Don't display in paperdoll

			Hue = 0x83EA;

			Direction = Direction.South;
			Frozen = true;
		}

		public override void InitSBInfo()
		{
		}

		public override bool GetGender()
		{
			return false; // male
		}

		public override void CheckMorph()
		{
		}

		public override void InitOutfit()
		{
			HairItemID = 0x203C;
			FacialHairItemID = 0x204D;
			HairHue = FacialHairHue = 0x8A7;

			AddItem( new Sandals() );

			Item item;

			item = new Cloak();
			item.ItemID = 0x26AD;
			item.Hue = 0x455;
			AddItem( item );

			item = new Robe();
			item.ItemID = 0x26AE;
			item.Hue = 0x4AB;
			AddItem( item );

			item = new Backpack();
			item.Movable = false;
			AddItem( item );
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.CanBeginAction( this ) )
			{
				from.BeginAction( this );
				Timer.DelayCall<Mobile>( m_ShoutCooldown, EndLock, from );
			}

			MLQuestSystem.TurnToFace( this, from );
			from.SendGump( m_Gump );

			// Paperdoll doesn't open
			//base.OnDoubleClick( from );
		}

		public override void OnThink()
		{
			base.OnThink();

			if ( m_NextShout <= DateTime.UtcNow )
			{
				Packet shoutPacket = null;

				foreach ( NetState state in GetClientsInRange( 12 ) )
				{
					Mobile m = state.Mobile;

					if ( m.CanSee( this ) && m.InLOS( this ) && m.CanBeginAction( this ) )
					{
						if ( shoutPacket == null )
							shoutPacket = Packet.Acquire( new MessageLocalized( Serial, Body, MessageType.Regular, 946, 3, 1078099, Name, "" ) ); // Double Click On Me For Help!

						state.Send( shoutPacket );
					}
				}

				Packet.Release( shoutPacket );

				m_NextShout = DateTime.UtcNow + m_ShoutDelay;
			}
		}

		private void EndLock( Mobile m )
		{
			m.EndAction( this );
		}

		public SirHelper( Serial serial )
			: base( serial )
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

			Frozen = true;
		}
	}
}
