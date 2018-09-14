namespace Server.Factions
{
	public class RankDefinition
	{
		private int m_Rank;
		private int m_Required;
		private int m_MaxWearables;
		private TextDefinition m_Title;

		public int Rank => m_Rank;
		public int Required => m_Required;
		public int MaxWearables => m_MaxWearables;
		public TextDefinition Title => m_Title;

		public RankDefinition( int rank, int required, int maxWearables, TextDefinition title )
		{
			m_Rank = rank;
			m_Required = required;
			m_Title = title;
			m_MaxWearables = maxWearables;
		}
	}
}