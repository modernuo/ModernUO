using System;

namespace Server.Engines.Harvest
{
	public class HarvestResource
	{
		private Type[] m_Types;
		private double m_ReqSkill, m_MinSkill, m_MaxSkill;
		private object m_SuccessMessage;

		public Type[] Types{ get => m_Types;
			set => m_Types = value;
		}
		public double ReqSkill{ get => m_ReqSkill;
			set => m_ReqSkill = value;
		}
		public double MinSkill{ get => m_MinSkill;
			set => m_MinSkill = value;
		}
		public double MaxSkill{ get => m_MaxSkill;
			set => m_MaxSkill = value;
		}
		public object SuccessMessage => m_SuccessMessage;

		public void SendSuccessTo( Mobile m )
		{
			if ( m_SuccessMessage is int messageInt )
				m.SendLocalizedMessage( messageInt );
			else
				m.SendMessage( m_SuccessMessage.ToString() );
		}

		public HarvestResource( double reqSkill, double minSkill, double maxSkill, object message, params Type[] types )
		{
			m_ReqSkill = reqSkill;
			m_MinSkill = minSkill;
			m_MaxSkill = maxSkill;
			m_Types = types;
			m_SuccessMessage = message;
		}
	}
}
