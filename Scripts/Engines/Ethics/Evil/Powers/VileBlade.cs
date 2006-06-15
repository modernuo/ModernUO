using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Ethics.Evil
{
	public sealed class VileBlade : Power
	{
		public VileBlade()
		{
			m_Definition = new PowerDefinition(
					10,
					"Vile Blade",
					"Velgo Reyam",
					""
				);
		}

		public override void BeginInvoke( Player from )
		{
		}
	}
}
