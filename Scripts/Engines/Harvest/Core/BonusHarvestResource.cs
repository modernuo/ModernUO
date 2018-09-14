using System;

namespace Server.Engines.Harvest
{
	public class BonusHarvestResource
	{
		public Type Type { get; set; }

		public double ReqSkill { get; set; }

		public double Chance { get; set; }

		public TextDefinition SuccessMessage { get; }

		public void SendSuccessTo( Mobile m )
		{
			TextDefinition.SendMessageTo( m, SuccessMessage );
		}

		public BonusHarvestResource( double reqSkill, double chance, TextDefinition message, Type type )
		{
			ReqSkill = reqSkill;

			Chance = chance;
			Type = type;
			SuccessMessage = message;
		}
	}
}
