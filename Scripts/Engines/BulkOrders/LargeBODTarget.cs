using System;
using Server;
using Server.Targeting;
using Server.Network;

namespace Server.Engines.BulkOrders
{
	public class LargeBODTarget : Target
	{
		private LargeBOD m_Deed;

		public LargeBODTarget( LargeBOD deed ) : base( 18, false, TargetFlags.None )
		{
			m_Deed = deed;
		}

		protected override void OnTarget( Mobile from, object targeted )
		{
			if ( m_Deed.Deleted || !m_Deed.IsChildOf( from.Backpack ) )
				return;

			m_Deed.EndCombine( from, targeted );
		}
	}
}