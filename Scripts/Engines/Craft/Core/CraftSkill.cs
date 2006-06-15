using System;

namespace Server.Engines.Craft
{
	public class CraftSkill
	{
		private SkillName m_SkillToMake;
		private double m_MinSkill;
		private double m_MaxSkill;

		public CraftSkill( SkillName skillToMake, double minSkill, double maxSkill )
		{
			m_SkillToMake = skillToMake;
			m_MinSkill = minSkill;
			m_MaxSkill = maxSkill;
		}

		public SkillName SkillToMake
		{
			get { return m_SkillToMake; }
		}

		public double MinSkill
		{
			get { return m_MinSkill; }
		}

		public double MaxSkill
		{
			get { return m_MaxSkill; }
		}
	}
}