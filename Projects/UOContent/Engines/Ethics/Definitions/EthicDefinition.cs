namespace Server.Ethics
{
    public class EthicDefinition
    {
        public EthicDefinition(
            int primaryHue, TextDefinition title, TextDefinition adjunct, TextDefinition joinPhrase,
            Power[] powers
        )
        {
            PrimaryHue = primaryHue;

            Title = title;
            Adjunct = adjunct;

            JoinPhrase = joinPhrase;

            Powers = powers;
        }

        public int PrimaryHue { get; }

        public TextDefinition Title { get; }

        public TextDefinition Adjunct { get; }

        public TextDefinition JoinPhrase { get; }

        public Power[] Powers { get; }
    }
}
