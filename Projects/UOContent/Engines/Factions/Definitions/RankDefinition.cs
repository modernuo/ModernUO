namespace Server.Factions
{
    public class RankDefinition
    {
        public RankDefinition(int rank, int required, int maxWearables, TextDefinition title)
        {
            Rank = rank;
            Required = required;
            Title = title;
            MaxWearables = maxWearables;
        }

        public int Rank { get; }

        public int Required { get; }

        public int MaxWearables { get; }

        public TextDefinition Title { get; }
    }
}
