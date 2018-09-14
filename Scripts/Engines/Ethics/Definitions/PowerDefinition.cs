namespace Server.Ethics
{
	public class PowerDefinition
	{
		public int Power { get; }

		public TextDefinition Name { get; }

		public TextDefinition Phrase { get; }

		public TextDefinition Description { get; }

		public PowerDefinition( int power, TextDefinition name, TextDefinition phrase, TextDefinition description )
		{
			Power = power;

			Name = name;
			Phrase = phrase;
			Description = description;
		}
	}
}
