using System;
using Server;
using Server.Multis;
using Server.Gumps;
using Server.Network;
using Server.Engines.VeteranRewards;

namespace Server.Items
{	
	public class StoneAnkhComponent : AddonComponent
	{
		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } }

		public StoneAnkhComponent( int itemID ) : base( itemID )
		{
			Weight = 1.0;
		}

		public StoneAnkhComponent( Serial serial ) : base( serial )
		{
		}
		
		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );
			
			if ( Addon is StoneAnkh && ((StoneAnkh) Addon).IsRewardItem )
				list.Add( 1076221 ); // 5th Year Veteran Reward
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}
			
		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class StoneAnkh : BaseAddon, IRewardItem
	{
		public override BaseAddonDeed Deed
		{ 
			get
			{ 
				StoneAnkhDeed deed = new StoneAnkhDeed();
				deed.IsRewardItem = m_IsRewardItem;

				return deed; 
			} 
		}

		private bool m_IsRewardItem;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem
		{
			get{ return m_IsRewardItem; }
			set{ m_IsRewardItem = value; InvalidateProperties(); }
		}
		
		[Constructable]
		public StoneAnkh() : this( true )
		{
		}
		
		[Constructable]
		public StoneAnkh( bool east ) : base()
		{			
			if ( east )
			{
				AddComponent( new StoneAnkhComponent( 0x2 ), 0, 0, 0 );
				AddComponent( new StoneAnkhComponent( 0x3 ), 0, -1, 0 );
			}
			else
			{
				AddComponent( new StoneAnkhComponent( 0x5 ), 0, 0, 0 );
				AddComponent( new StoneAnkhComponent( 0x4 ), -1, 0, 0 );
			}
		}

		public StoneAnkh( Serial serial ) : base( serial )
		{
		}
		
		public override void OnChop( Mobile from )
		{
			from.SendLocalizedMessage( 500489 ); // You can't use an axe on that.
			return;
		}

			public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );
			
			if ( Core.ML && m_IsRewardItem )
				list.Add( 1076221 ); // 5th Year Veteran Reward
		}

		public override void OnComponentUsed( AddonComponent c, Mobile from )
			{
				if ( from.InRange( Location, 2 ) )
				{
				BaseHouse house = BaseHouse.FindHouseAt( this );  
				
						if ( house != null && house.IsOwner( from ) )
						{
						from.CloseGump( typeof( RewardDemolitionGump ) );
						from.SendGump( new RewardDemolitionGump( this, 1049783 ) ); // Do you wish to re-deed this decoration?
					}
					else
					from.SendLocalizedMessage( 1049784 ); // You can only re-deed this decoration if you are the house owner or originally placed the decoration.
				}
				else
				from.Say( 1019045 ); // I can't reach that.
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
	}	
	
	public class StoneAnkhDeed : BaseAddonDeed, IRewardItem
	{
		public override int LabelNumber{ get{ return 1049773; } } // deed for a stone ankh
		
		private bool m_East;
		private bool m_IsRewardItem;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem
		{
			get{ return m_IsRewardItem; }
			set{ m_IsRewardItem = value; InvalidateProperties(); }
		}

		public override BaseAddon Addon
		{ 
			get
			{ 
				StoneAnkh addon = new StoneAnkh( m_East );
				addon.IsRewardItem = m_IsRewardItem;

				return addon; 
			} 
		}

		[Constructable]
		public StoneAnkhDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public StoneAnkhDeed( Serial serial ) : base( serial )
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
		
		private void SendTarget( Mobile m )
		{
			base.OnDoubleClick( m );
		}
		
		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );
			
			if ( m_IsRewardItem )
				list.Add( 1076221 ); // 5th Year Veteran Reward
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
			private StoneAnkhDeed m_Deed;
				
			private enum Buttons
			{
				Cancel,
				South,
				East
			}

			public InternalGump( StoneAnkhDeed deed ) : base( 150, 50 )
			{
				m_Deed = deed;				
				
				Closable = true;
				Disposable = true;
				Dragable = true;
				Resizable = false;

				AddPage( 0 );

				AddBackground( 0, 0, 300, 150, 0xA28 );

				AddItem( 90, 30, 0x4 );
				AddItem( 112, 30, 0x5 );
				AddButton( 50, 35, 0x867, 0x869, (int) Buttons.South, GumpButtonType.Reply, 0 ); // South

				AddItem( 170, 30, 0x2 );
				AddItem( 192, 30, 0x3 );
				AddButton( 145, 35, 0x867, 0x869, (int) Buttons.East, GumpButtonType.Reply, 0 ); // East
			}

			public override void OnResponse( NetState sender, RelayInfo info )
			{
				if ( m_Deed == null || m_Deed.Deleted )
					return;
					
				if ( info.ButtonID != (int) Buttons.Cancel )
				{
					m_Deed.m_East = ( info.ButtonID == (int) Buttons.East );
					m_Deed.SendTarget( sender.Mobile );
				}
			}
		}
	}
}
