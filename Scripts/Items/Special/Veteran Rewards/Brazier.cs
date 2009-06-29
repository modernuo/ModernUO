using System;
using Server;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Engines.VeteranRewards;

namespace Server.Items
{	
	public class RewardBrazier : Item, IRewardItem
	{
		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } }

		private bool m_IsRewardItem;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem
		{
			get{ return m_IsRewardItem; }
			set{ m_IsRewardItem = value; InvalidateProperties(); }
		}

		private Item m_Fire;

		private static int[] m_Art = new int[]
		{
			0x19AA, 0x19BB
		};
		
		[Constructable]
		public RewardBrazier() : this( Utility.RandomList( m_Art ) )
		{	
		}

		[Constructable]
		public RewardBrazier( int itemID ) : base( itemID )
		{
			LootType = LootType.Blessed;
			Weight = 10.0;
		}

		public RewardBrazier( Serial serial ) : base( serial )
		{
		}

		public void TurnOff()
		{
			if ( m_Fire != null )
			{
				m_Fire.Delete();
				m_Fire = null;
			}
		}

		public void TurnOn()
		{
			if ( m_Fire == null )
				m_Fire = new Item();
 
			m_Fire.ItemID = 0x19AB;
			m_Fire.Movable = false;
			m_Fire.MoveToWorld( new Point3D( X, Y, Z + ItemData.Height + 2 ), Map );
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !from.InRange( this.GetWorldLocation(), 2 ) )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
			}
			else if ( IsLockedDown )
			{
				BaseHouse house = BaseHouse.FindHouseAt( from );

				if ( house != null && house.IsCoOwner( from ) )
				{
					if ( m_Fire != null )
						TurnOff();
					else
						TurnOn();
				}
				else
					from.SendLocalizedMessage( 502436 ); // That is not accessible.
			}
			else
				from.SendLocalizedMessage( 502692 ); // This must be in a house and be locked down to work.
		}

		public override void OnLocationChange( Point3D old )
		{
			if ( m_Fire != null )
				m_Fire.MoveToWorld( new Point3D( X, Y, Z + ItemData.Height ), Map );
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );
			
			if ( m_IsRewardItem )
				list.Add( 1076222 ); // 6th Year Veteran Reward
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
			
			writer.Write( (bool) m_IsRewardItem );
			writer.Write( (Item) m_Fire );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
			
			m_IsRewardItem = reader.ReadBool();
			m_Fire = reader.ReadItem();
		}
	}	
	
	public class RewardBrazierDeed : Item, IRewardItem
	{
		public override int LabelNumber{ get{ return 1080527; } } // Brazier Deed

		private bool m_IsRewardItem;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem
		{
			get{ return m_IsRewardItem; }
			set{ m_IsRewardItem = value; InvalidateProperties(); }
		}

		[Constructable]
		public RewardBrazierDeed() : base( 0x14F0 )
		{
			LootType = LootType.Blessed;
			Weight = 1.0;
		}

		public RewardBrazierDeed( Serial serial ) : base( serial )
		{
		}
		
		public override void OnDoubleClick( Mobile from )
		{
			if ( m_IsRewardItem && !RewardSystem.CheckIsUsableBy( from, this, null ) )
				return;

			if ( IsChildOf( from.Backpack ) )
			{
				from.CloseGump( typeof( InternalGump ) );
				from.SendGump( new InternalGump( this ) );
			}
			else
				from.SendLocalizedMessage( 1042038 ); // You must have the object in your backpack to use it.
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( m_IsRewardItem )
				list.Add( 1076222 ); // 6th Year Veteran Reward
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version

			writer.Write( (bool) m_IsRewardItem );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
			
			m_IsRewardItem = reader.ReadBool();
		}

		private class InternalGump : Gump
		{
			private RewardBrazierDeed m_Brazier;

			public InternalGump( RewardBrazierDeed brazier ) : base( 100, 200 )
			{
				m_Brazier = brazier;

				Closable = true;
				Disposable = true;
				Dragable = true;
				Resizable = false;

				AddPage( 0 );
				AddBackground( 0, 0, 200, 200, 2600 );

				AddPage( 1 );
				AddLabel( 45, 15, 0, "Choose a Brazier:" );

				AddItem( 40, 75, 0x19AA );
				AddButton( 55, 50, 0x845, 0x846, 0x19AA, GumpButtonType.Reply, 0 );

				AddItem( 100, 75, 0x19BB );
				AddButton( 115, 50, 0x845, 0x846, 0x19BB, GumpButtonType.Reply, 0 );
			}

			public override void OnResponse( NetState sender, RelayInfo info )
			{
				if ( m_Brazier == null | m_Brazier.Deleted )
					return;

				Mobile m = sender.Mobile;

				if ( info.ButtonID == 0x19AA || info.ButtonID == 0x19BB )
				{
					RewardBrazier brazier = new RewardBrazier( info.ButtonID );
					brazier.IsRewardItem = m_Brazier.IsRewardItem;

					if ( !m.PlaceInBackpack( brazier ) )
					{
						brazier.Delete();
						m.SendLocalizedMessage( 1078837 ); // Your backpack is full! Please make room and try again.
					}
					else
						m_Brazier.Delete();
				}
			}
		}
	}
}
