using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Ethics
{
	public class PowerDefinition
	{
		private int m_Power;

		private TextDefinition m_Name;
		private TextDefinition m_Phrase;
		private TextDefinition m_Description;

		public int Power { get { return m_Power; } }

		public TextDefinition Name { get { return m_Name; } }
		public TextDefinition Phrase { get { return m_Phrase; } }
		public TextDefinition Description { get { return m_Description; } }

		public PowerDefinition( int power, TextDefinition name, TextDefinition phrase, TextDefinition description )
		{
			m_Power = power;

			m_Name = name;
			m_Phrase = phrase;
			m_Description = description;
		}
	}
}
