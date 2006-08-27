using System;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server.Factions
{
	public abstract class FactionGump : Gump
	{
		public virtual int ButtonTypes{ get{ return 10; } }

		public int ToButtonID( int type, int index )
		{
			return 1 + (index * ButtonTypes) + type;
		}

		public bool FromButtonID( int buttonID, out int type, out int index )
		{
			int offset = buttonID - 1;

			if ( offset >= 0 )
			{
				type = offset % ButtonTypes;
				index = offset / ButtonTypes;
				return true;
			}
			else
			{
				type = index = 0;
				return false;
			}
		}

		public static bool Exists( Mobile mob )
		{
			return ( mob.FindGump( typeof( FactionGump ) ) != null );
		}

		public void AddHtmlText( int x, int y, int width, int height, TextDefinition text, bool back, bool scroll )
		{
			if ( text != null && text.Number > 0 )
				AddHtmlLocalized( x, y, width, height, text.Number, back, scroll );
			else if ( text != null && text.String != null )
				AddHtml( x, y, width, height, text.String, back, scroll );
		}

		public FactionGump( int x, int y ) : base( x, y )
		{
		}
	}
}