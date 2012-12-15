using System;
using Server;
using Server.Network;

namespace Server.Gumps
{
	/*
	 * A generic version of the EA Clean Up Britannia reward gump.
	 */

	public interface IRewardEntry
	{
		int Price { get; }
		int ItemID { get; }
		int Hue { get; }
		int Tooltip { get; }
		TextDefinition Description { get; }
	}

	public delegate void RewardPickedHandler( Mobile from, int index );

	public class RewardGump : Gump
	{
		private TextDefinition m_Title;
		private IRewardEntry[] m_Rewards;
		private int m_Points;
		private RewardPickedHandler m_OnPicked;

		public TextDefinition Title { get { return m_Title; } }
		public IRewardEntry[] Rewards { get { return m_Rewards; } }
		public int Points { get { return m_Points; } }
		public RewardPickedHandler OnPicked { get { return m_OnPicked; } }

		public RewardGump( TextDefinition title, IRewardEntry[] rewards, int points, RewardPickedHandler onPicked )
			: base( 250, 50 )
		{
			m_Title = title;
			m_Rewards = rewards;
			m_Points = points;
			m_OnPicked = onPicked;

			AddPage( 0 );

			AddImage( 0, 0, 0x1F40 );
			AddImageTiled( 20, 37, 300, 308, 0x1F42 );
			AddImage( 20, 325, 0x1F43 );
			AddImage( 35, 8, 0x39 );
			AddImageTiled( 65, 8, 257, 10, 0x3A );
			AddImage( 290, 8, 0x3B );
			AddImage( 32, 33, 0x2635 );
			AddImageTiled( 70, 55, 230, 2, 0x23C5 );

			if ( m_Title.String != null )
				AddHtml( 70, 35, 270, 20, m_Title.String, false, false );
			else if ( m_Title.Number != 0 )
				AddHtmlLocalized( 70, 35, 270, 20, m_Title.Number, 1, false, false );

			AddHtmlLocalized( 50, 65, 150, 20, 1072843, 1, false, false ); // Your Reward Points:
			AddLabel( 230, 65, 0x64, m_Points.ToString() );
			AddImageTiled( 35, 85, 270, 2, 0x23C5 );
			AddHtmlLocalized( 35, 90, 270, 20, 1072844, 1, false, false ); // Please Choose a Reward:

			AddPage( 1 );

			int offset = 110;
			int page = 1;

			for ( int i = 0; i < m_Rewards.Length; ++i )
			{
				IRewardEntry entry = m_Rewards[i];

				Rectangle2D bounds = ItemBounds.Table[entry.ItemID];
				int height = Math.Max( 36, bounds.Height );

				if ( offset + height > 320 )
				{
					AddHtmlLocalized( 240, 335, 60, 20, 1072854, 1, false, false ); // <div align=right>Next</div>
					AddButton( 300, 335, 0x15E1, 0x15E5, 51, GumpButtonType.Page, page + 1 );

					AddPage( ++page );

					AddButton( 150, 335, 0x15E3, 0x15E7, 52, GumpButtonType.Page, page - 1 );
					AddHtmlLocalized( 170, 335, 60, 20, 1074880, 1, false, false ); // Previous

					offset = 110;
				}

				bool available = ( entry.Price <= m_Points );
				int half = offset + ( height / 2 );

				if ( available )
					AddButton( 35, half - 6, 0x837, 0x838, 100 + i, GumpButtonType.Reply, 0 );

				AddItem( 83 - ( bounds.Width / 2 ) - bounds.X, half - ( bounds.Height / 2 ) - bounds.Y, entry.ItemID, available ? entry.Hue : 995 );

				if ( entry.Tooltip != 0 )
					AddTooltip( entry.Tooltip );

				AddLabel( 133, half - 10, available ? 0x64 : 0x21, entry.Price.ToString() );

				if ( entry.Description != null )
				{
					if ( entry.Description.String != null )
						AddHtml( 190, offset, 114, height, entry.Description.String, false, false );
					else if ( entry.Description.Number != 0 )
						AddHtmlLocalized( 190, offset, 114, height, entry.Description.Number, 1, false, false );
				}

				offset += height + 10;
			}
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			int choice = info.ButtonID;

			if ( choice == 0 )
				return; // Close

			choice -= 100;

			if ( choice >= 0 && choice < m_Rewards.Length )
			{
				IRewardEntry entry = m_Rewards[choice];

				if ( entry.Price <= m_Points )
					sender.Mobile.SendGump( new RewardConfirmGump( this, choice, entry ) );
			}
		}
	}

	public class RewardConfirmGump : Gump
	{
		private RewardGump m_Parent;
		private int m_Index;

		public RewardConfirmGump( RewardGump parent, int index, IRewardEntry entry )
			: base( 120, 50 )
		{
			m_Parent = parent;
			m_Index = index;

			Closable = false;

			AddPage( 0 );

			AddImageTiled( 0, 0, 348, 262, 0xA8E );
			AddAlphaRegion( 0, 0, 348, 262 );
			AddImage( 0, 15, 0x27A8 );
			AddImageTiled( 0, 30, 17, 200, 0x27A7 );
			AddImage( 0, 230, 0x27AA );
			AddImage( 15, 0, 0x280C );
			AddImageTiled( 30, 0, 300, 17, 0x280A );
			AddImage( 315, 0, 0x280E );
			AddImage( 15, 244, 0x280C );
			AddImageTiled( 30, 244, 300, 17, 0x280A );
			AddImage( 315, 244, 0x280E );
			AddImage( 330, 15, 0x27A8 );
			AddImageTiled( 330, 30, 17, 200, 0x27A7 );
			AddImage( 330, 230, 0x27AA );
			AddImage( 333, 2, 0x2716 );
			AddImage( 333, 248, 0x2716 );
			AddImage( 2, 248, 0x2716 );
			AddImage( 2, 2, 0x2716 );

			AddItem( 140, 120, entry.ItemID, entry.Hue );

			if ( entry.Tooltip != 0 )
				AddTooltip( entry.Tooltip );

			AddHtmlLocalized( 25, 22, 200, 20, 1074974, 0x7D00, false, false ); // Confirm Selection
			AddImage( 25, 40, 0xBBF );
			AddHtmlLocalized( 25, 55, 300, 120, 1074975, 0xFFFFFF, false, false ); // Are you sure you wish to select this?
			AddRadio( 25, 175, 0x25F8, 0x25FB, true, 1 );
			AddRadio( 25, 210, 0x25F8, 0x25FB, false, 0 );
			AddHtmlLocalized( 60, 180, 280, 20, 1074976, 0xFFFFFF, false, false ); // Yes
			AddHtmlLocalized( 60, 215, 280, 20, 1074977, 0xFFFFFF, false, false ); // No
			AddButton( 265, 220, 0xF7, 0xF8, 7, GumpButtonType.Reply, 0 );
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			if ( info.ButtonID == 7 && info.IsSwitched( 1 ) )
				m_Parent.OnPicked( sender.Mobile, m_Index );
			else
				sender.Mobile.SendGump( new RewardGump( m_Parent.Title, m_Parent.Rewards, m_Parent.Points, m_Parent.OnPicked ) );
		}
	}
}