using System;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Multis;
using Server.Engines.VeteranRewards;

namespace Server.Items
{	
	public class TreeStump : BaseAddon, IRewardItem
	{
		public override BaseAddonDeed Deed
		{
			get
			{
				TreeStumpDeed deed = new TreeStumpDeed();
				deed.IsRewardItem = m_IsRewardItem;
				deed.Logs = m_Logs;

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

		private int m_Logs;

		[CommandProperty( AccessLevel.GameMaster )]
		public int Logs
		{
			get{ return m_Logs; }
			set{ m_Logs = value; InvalidateProperties(); }
		}


		private Timer m_Timer;
		
		[Constructable]
		public TreeStump( int itemID ) : base()
		{	
			AddComponent( new AddonComponent( itemID ), 0, 0, 0 );

			m_Timer = Timer.DelayCall( TimeSpan.FromDays( 1 ), TimeSpan.FromDays( 1 ), new TimerCallback( GiveLogs ) );
		}

		public TreeStump( Serial serial ) : base( serial )
		{
		}

		private void GiveLogs()
		{
			m_Logs = Math.Min( 100, m_Logs + 10 );
		}

		public override void OnComponentUsed( AddonComponent c, Mobile from )
		{
			BaseHouse house = BaseHouse.FindHouseAt( this );

			if ( !from.InRange( GetWorldLocation(), 2 ) )
			{
				from.LocalOverheadMessage( Network.MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
			}
			else if ( house != null && house.HasSecureAccess( from, SecureLevel.Friends ) )
			{
				if ( m_Logs > 0 )
				{
					Item logs = null;

					switch ( Utility.Random( 7 ) )
					{
						case 0: logs = new Log(); break;
						case 1: logs = new AshLog(); break;
						case 2: logs = new OakLog(); break;
						case 3: logs = new YewLog(); break;
						case 4: logs = new HeartwoodLog(); break;
						case 5: logs = new BloodwoodLog(); break;
						case 6: logs = new FrostwoodLog(); break;
					}
					
					int amount = Math.Min( 10, m_Logs );
					logs.Amount = amount;

					if ( !from.PlaceInBackpack( logs ) )
					{
						logs.Delete();
						from.SendLocalizedMessage( 1078837 ); // Your backpack is full! Please make room and try again.
					}
					else
					{
						m_Logs -= amount;
						PublicOverheadMessage( MessageType.Regular, 0, 1094719, m_Logs.ToString() ); // Logs: ~1_COUNT~
					}
				}
				else 
					from.SendLocalizedMessage( 1094720 ); // There are no more logs available.
			}
			else 
				from.SendLocalizedMessage( 1061637 ); // You are not allowed to access this.
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
			
			writer.Write( (bool) m_IsRewardItem );
			writer.Write( (int) m_Logs );

			if ( m_Timer != null )
				writer.Write( (DateTime) m_Timer.Next );
			else
				writer.Write( (DateTime) DateTime.Now + TimeSpan.FromDays( 1 ) );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
			
			m_IsRewardItem = reader.ReadBool();
			m_Logs = reader.ReadInt();

			DateTime next = reader.ReadDateTime();

			if ( next < DateTime.Now )
				next = DateTime.Now;	

			m_Timer = Timer.DelayCall( next - DateTime.Now, TimeSpan.FromDays( 1 ), new TimerCallback( GiveLogs ) );
		}
	}	
	
	public class TreeStumpDeed : BaseAddonDeed, IRewardItem, IRewardOption
	{
		public override int LabelNumber{ get{ return 1080406; } } // a deed for a tree stump decoration

		public override BaseAddon Addon
		{
			get
			{
				TreeStump addon = new TreeStump( m_ItemID );
				addon.IsRewardItem = m_IsRewardItem;
				addon.Logs = m_Logs;

				return addon;
			}
		}
		
		private int m_ItemID;
		private bool m_IsRewardItem;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsRewardItem
		{
			get{ return m_IsRewardItem; }
			set{ m_IsRewardItem = value; InvalidateProperties(); }
		}

		private int m_Logs;		

		[CommandProperty( AccessLevel.GameMaster )]
		public int Logs
		{
			get{ return m_Logs; }
			set{ m_Logs = value; InvalidateProperties(); }
		}

		[Constructable]
		public TreeStumpDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public TreeStumpDeed( Serial serial ) : base( serial )
		{
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );
			
			if ( m_IsRewardItem )
				list.Add( 1076223 ); // 7th Year Veteran Reward
		}
		
		public override void OnDoubleClick( Mobile from )
		{        	
			if ( m_IsRewardItem && !RewardSystem.CheckIsUsableBy( from, this, null ) )
				return;
		
			if ( IsChildOf( from.Backpack ) )
			{
				from.CloseGump( typeof( RewardOptionGump ) );
				from.SendGump( new RewardOptionGump( this ) );
			}
			else
				from.SendLocalizedMessage( 1062334 ); // This item must be in your backpack to be used.          	
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version

			writer.Write( (bool) m_IsRewardItem );
			writer.Write( (int) m_Logs );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
			
			m_IsRewardItem = reader.ReadBool();
			m_Logs = reader.ReadInt();
		}		
		
		public void GetOptions( RewardOptionList list )
		{
			list.Add( 1, 1080403 ); // Tree Stump with Axe West
			list.Add( 2, 1080404 ); // Tree Stump with Axe North
			list.Add( 3, 1080401 ); // Tree Stump East
			list.Add( 4, 1080402 ); // Tree Stump South
		}

		public void OnOptionSelected( Mobile from, int option )
		{
			switch ( option )
			{		
				case 1: m_ItemID = 0xE56; break;
				case 2: m_ItemID = 0xE58; break;
				case 3: m_ItemID = 0xE57; break;
				case 4: m_ItemID = 0xE59; break;
			}

			if ( !Deleted )
				base.OnDoubleClick( from );
		}
	}
}
