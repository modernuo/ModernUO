using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Ethics.Evil
{
	public sealed class UnholyWord : Power
	{
		public UnholyWord()
		{
			m_Definition = new PowerDefinition(
					100,
					"Unholy Word",
					"Velgo Oostrac",
					""
				);
		}

		public override void BeginInvoke( Player from )
		{
		}
	}
}
