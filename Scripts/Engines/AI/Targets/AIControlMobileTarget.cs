using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Targets
{
	public class AIControlMobileTarget : Target
	{
		private List<BaseAI> m_List;
		private OrderType m_Order;

		public OrderType Order {
			get {
				return m_Order;
			}
		}

		public AIControlMobileTarget( BaseAI ai, OrderType order ) : base( -1, false, ( order == OrderType.Attack ? TargetFlags.Harmful : TargetFlags.None ) )
		{
			m_List = new List<BaseAI>();
			m_Order = order;

			AddAI( ai );
		}

		public void AddAI( BaseAI ai )
		{
			if ( !m_List.Contains( ai ) )
				m_List.Add( ai );
		}

		protected override void OnTarget( Mobile from, object o )
		{
			if ( o is Mobile ) {
				Mobile m = (Mobile)o;
				for ( int i = 0; i < m_List.Count; ++i )
					m_List[i].EndPickTarget( from, m, m_Order );
			}
		}
	}
}