using System;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.MLQuests.Gumps
{
	public class InfoNPCGump : BaseQuestGump
	{
		public InfoNPCGump( TextDefinition title, TextDefinition message )
			: base( 1060668 ) // INFORMATION
		{
			RegisterButton( ButtonPosition.Left, ButtonGraphic.Close, 3 );

			SetPageCount( 1 );

			BuildPage();
			TextDefinition.AddHtmlText( this, 160, 108, 250, 16, title, false, false, 0x2710, 0x4AC684 );
			TextDefinition.AddHtmlText( this, 98, 156, 312, 180, message, false, true, 0x15F90, 0xBDE784 );
		}
	}
}
