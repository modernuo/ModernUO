namespace Server.Engines.Craft
{
  public class CraftSkill
  {
    public CraftSkill(SkillName skillToMake, int minSkill, int maxSkill)
    {
      SkillToMake = skillToMake;
      MinSkill = minSkill;
      MaxSkill = maxSkill;
    }

    public SkillName SkillToMake{ get; }

    public int MinSkill{ get; }

    public int MaxSkill{ get; }
  }
}
