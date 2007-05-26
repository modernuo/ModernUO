using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;
using Server.Targeting;
using Server.Network;

namespace Server.Items
{
	public enum BagOfSendingHue
	{
		Yellow,
		Blue,
		Red
	}

	public class BagOfSending : Item, TranslocationItem
	{
		public static BagOfSendingHue RandomHue()
		{
			switch ( Utility.Random( 3 ) )
			{
				case 0: return BagOfSendingHue.Yellow;
				case 1: return BagOfSendingHue.Blue;
				default: return BagOfSendingHue.Red;
			}
		}

		private int m_Charges;
		private int m_Recharges;
		private BagOfSendingHue m_BagOfSendingHue;

		[CommandProperty( AccessLevel.GameMaster )]
		public int Charges
		{
			get{ return m_Charges; }
			set
			{
				if ( value > this.MaxCharges )
					m_Charges = this.MaxCharges;
				else if ( value < 0 )
					m_Charges = 0;
				else
					m_Charges = value;

				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Recharges
		{
			get{ return m_Recharges; }
			set
			{
				if ( value > this.MaxRecharges )
					m_Recharges = this.MaxRecharges;
				else if ( value < 0 )
					m_Recharges = 0;
				else
					m_Recharges = value;

				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int MaxCharges{ get{ return 30; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int MaxRecharges{ get{ return 255; } }

		public string TranslocationItemName{ get{ return "bag of sending"; } }

		public override int LabelNumber{ get{ return 1054104; } } // a bag of sending

		[CommandProperty( AccessLevel.GameMaster )]
		public BagOfSendingHue BagOfSendingHue
		{
			get{ return m_BagOfSendingHue; }
			set
			{
				m_BagOfSendingHue = value;

				switch ( value )
				{
					case BagOfSendingHue.Yellow: this.Hue = 0x8A5; break;
					case BagOfSendingHue.Blue: this.Hue = 0x8AD; break;
					case BagOfSendingHue.Red: this.Hue = 0x89B; break;
				}
			}
		}

		[Constructable]
		public BagOfSending() : this( RandomHue() )
		{
		}

		[Constructable]
		public BagOfSending( BagOfSendingHue hue ) : base( 0xE76 )
		{
			Weight = 2.0;

			this.BagOfSendingHue = hue;

			m_Charges = Utility.RandomMinMax( 3, 9 );
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( 1060741, m_Charges.ToString() ); // charges: ~1_val~
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );

			LabelTo( from, 1060741, m_Charges.ToString() ); // charges: ~1_val~
		}

		public override void GetContextMenuEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.GetContextMenuEntries( from, list );

			if ( from.Alive )
				list.Add( new UseBagEntry( this, Charges > 0 && IsChildOf( from.Backpack ) ) );
		}

		private class UseBagEntry : ContextMenuEntry
		{
			private BagOfSending m_Bag;

			public UseBagEntry( BagOfSending bag, bool enabled ) : base( 6189 )
			{
				m_Bag = bag;

				if ( !enabled )
					Flags |= CMEFlags.Disabled;
			}

			public override void OnClick()
			{
				if ( m_Bag.Deleted )
					return;

				Mobile from = Owner.From;

				if ( from.CheckAlive() )
					m_Bag.OnDoubleClick( from );
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			if( from.Region.IsPartOf( typeof( Regions.Jail ) ) )
			{
				from.SendMessage( "You may not do that in jail." );
			}
			else if( !this.IsChildOf( from.Backpack ) )
			{
				MessageHelper.SendLocalizedMessageTo( this, from, 1054107, 0x59 ); // The bag of sending must be in your backpack.
			}
			else if( this.Charges == 0 )
			{
				MessageHelper.SendLocalizedMessageTo( this, from, 1042544, 0x59 ); // This item is out of charges.
			}
			else
			{
				from.Target = new SendTarget( this );
			}
		}

		private class SendTarget : Target
		{
			private BagOfSending m_Bag;

			public SendTarget( BagOfSending bag ) : base( -1, false, TargetFlags.None )
			{
				m_Bag = bag;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( m_Bag.Deleted )
					return;

				if( from.Region.IsPartOf( typeof( Regions.Jail ) ) )
				{
					from.SendMessage( "You may not do that in jail." );
				}
				else if( !m_Bag.IsChildOf( from.Backpack ) )
				{
					MessageHelper.SendLocalizedMessageTo( m_Bag, from, 1054107, 0x59 ); // The bag of sending must be in your backpack.
				}
				else if ( m_Bag.Charges == 0 )
				{
					MessageHelper.SendLocalizedMessageTo( m_Bag, from, 1042544, 0x59 ); // This item is out of charges.
				}
				else if ( targeted is Item )
				{
					Item item = (Item)targeted;

					if ( !item.IsChildOf( from.Backpack ) )
					{
						MessageHelper.SendLocalizedMessageTo( m_Bag, from, 1054152, 0x59 ); // You may only send items from your backpack to your bank box.
					}
					else if ( item is BagOfSending || item is Container )
					{
						from.Send( new AsciiMessage( m_Bag.Serial, m_Bag.ItemID, MessageType.Regular, 0x3B2, 3, "", "You cannot send a container through the bag of sending." ) );
					}
					else if ( item.LootType == LootType.Cursed )
					{
						MessageHelper.SendLocalizedMessageTo( m_Bag, from, 1054108, 0x59 ); // The bag of sending rejects the cursed item.
					}
					else if ( !item.VerifyMove( from ) || item is Server.Engines.Quests.QuestItem )
					{
						MessageHelper.SendLocalizedMessageTo( m_Bag, from, 1054109, 0x59 ); // The bag of sending rejects that item.
					}
					else if ( Spells.SpellHelper.IsDoomGauntlet( from.Map, from.Location ) )
					{
						from.SendLocalizedMessage( 1062089 ); // You cannot use that here.
					}
					else if ( !from.BankBox.TryDropItem( from, item, false ) )
					{
						MessageHelper.SendLocalizedMessageTo( m_Bag, from, 1054110, 0x59 ); // Your bank box is full.
					}
					else
					{
						m_Bag.Charges--;

						MessageHelper.SendLocalizedMessageTo( m_Bag, from, 1054150, 0x59 ); // The item was placed in your bank box.
					}
				}
			}
		}

		public BagOfSending( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 1 ); // version

			writer.WriteEncodedInt( (int) m_Recharges );

			writer.WriteEncodedInt( (int) m_Charges );
			writer.WriteEncodedInt( (int) m_BagOfSendingHue );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();

			switch ( version )
			{
				case 1:
				{
					m_Recharges = reader.ReadEncodedInt();
					goto case 0;
				}
				case 0:
				{
					m_Charges = Math.Min( reader.ReadEncodedInt(), MaxCharges );
					m_BagOfSendingHue = (BagOfSendingHue) reader.ReadEncodedInt();
					break;
				}
			}
		}
	}
}