using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Ethics
{
	public class EthicDefinition
	{
		private int m_PrimaryHue;

		private TextDefinition m_Title;
		private TextDefinition m_Adjunct;

		private TextDefinition m_JoinPhrase;

		private Power[] m_Powers;

		public int PrimaryHue { get { return m_PrimaryHue; } }

		public TextDefinition Title { get { return m_Title; } }
		public TextDefinition Adjunct { get { return m_Adjunct; } }

		public TextDefinition JoinPhrase { get { return m_JoinPhrase; } }

		public Power[] Powers { get { return m_Powers; } }

		public EthicDefinition( int primaryHue, TextDefinition title, TextDefinition adjunct, TextDefinition joinPhrase, Power[] powers )
		{
			m_PrimaryHue = primaryHue;

			m_Title = title;
			m_Adjunct = adjunct;

			m_JoinPhrase = joinPhrase;

			m_Powers = powers;
		}
	}
}
