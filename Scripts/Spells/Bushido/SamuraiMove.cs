namespace Server.Spells
{
  public class SamuraiMove : SpecialMove
  {
    public override SkillName MoveSkill => SkillName.Bushido;

    public override void CheckGain(Mobile m)
    {
      m.CheckSkill(MoveSkill, RequiredSkill - 125, RequiredSkill + 375); //Per five on friday 02/16/07
    }
  }
}
