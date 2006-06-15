using System;
using Server;
using Server.Targeting;
using Server.Mobiles;
using System.Collections;

namespace Server.Targets
{
	public class AIControlMobileTarget : Target
	{
		private ArrayList m_List;
		private OrderType m_Order;

		public OrderType Order
		{
			get
			{
				return m_Order;
			}
		}

		public AIControlMobileTarget( BaseAI ai, OrderType order ) : base( -1, false, ( order == OrderType.Attack ? TargetFlags.Harmful : TargetFlags.None ) )
		{
			m_List = new ArrayList();
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
			if ( o is Mobile )
			{
				for ( int i = 0; i < m_List.Count; ++i )
					((BaseAI)m_List[i]).EndPickTarget( from, (Mobile)o, m_Order );
			}
		}
	}
}