using System;
using Server.Items;

namespace Server.ContextMenus
{
	public class OpenBankEntry : ContextMenuEntry
	{
		private Mobile m_Banker;

		public OpenBankEntry( Mobile from, Mobile banker ) : base( 6105, 12 )
		{
			m_Banker = banker;
		}

		public override void OnClick()
		{
			if ( !Owner.From.CheckAlive() )
				return;

			if ( Owner.From.Criminal )
			{
				m_Banker.Say( 500378 ); // Thou art a criminal and cannot access thy bank box.
			}
			else
			{
				this.Owner.From.BankBox.Open();
			}
		}
	}
}