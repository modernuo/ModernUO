using System;
using Server;
using Server.Mobiles;
using Server.Gumps;
using System.Collections.Generic;

namespace Server.Engines.MLQuests.Rewards
{
	public abstract class BaseReward
	{
		private TextDefinition m_Name;

		public TextDefinition Name
		{
			get { return m_Name; }
			set { m_Name = value; }
		}

		public BaseReward( TextDefinition name )
		{
			m_Name = name;
		}

		protected virtual int LabelHeight { get { return 16; } }

		public void WriteToGump( Gump g, int x, ref int y )
		{
			TextDefinition.AddHtmlText( g, x, y, 280, LabelHeight, m_Name, false, false, 0x15F90, 0xBDE784 );
		}

		public abstract void AddRewardItems( PlayerMobile pm, List<Item> rewards );
	}
}
