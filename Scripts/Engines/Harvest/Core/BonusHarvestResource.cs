using System;

namespace Server.Engines.Harvest
{
	public class BonusHarvestResource
	{
		private Type m_Type;
		private double m_ReqSkill, m_Chance;
		private TextDefinition m_SuccessMessage;

		public Type Type { get { return m_Type; } set { m_Type = value; } }
		public double ReqSkill { get { return m_ReqSkill; } set { m_ReqSkill = value; } }
		public double Chance { get { return m_Chance; } set { m_Chance = value; } }

		public TextDefinition SuccessMessage { get { return m_SuccessMessage; } }

		public void SendSuccessTo( Mobile m )
		{
			TextDefinition.SendMessageTo( m, m_SuccessMessage );
		}

		public BonusHarvestResource( double reqSkill, double chance, TextDefinition message, Type type )
		{
			m_ReqSkill = reqSkill;

			m_Chance = chance;
			m_Type = type;
			m_SuccessMessage = message;
		}
	}
}