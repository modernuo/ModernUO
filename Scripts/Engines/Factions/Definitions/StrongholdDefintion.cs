namespace Server.Factions
{
	public class StrongholdDefinition
	{
		private Rectangle2D[] m_Area;
		private Point3D m_JoinStone;
		private Point3D m_FactionStone;
		private Point3D[] m_Monoliths;

		public Rectangle2D[] Area => m_Area;

		public Point3D JoinStone => m_JoinStone;
		public Point3D FactionStone => m_FactionStone;

		public Point3D[] Monoliths => m_Monoliths;

		public StrongholdDefinition( Rectangle2D[] area, Point3D joinStone, Point3D factionStone, Point3D[] monoliths )
		{
			m_Area = area;
			m_JoinStone = joinStone;
			m_FactionStone = factionStone;
			m_Monoliths = monoliths;
		}
	}
}