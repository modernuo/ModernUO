namespace Server.Spells
{
  public interface IRecallSpell
  {
    Mobile Caster { get; }
    void Effect(Point3D loc, Map map, bool checkMulti);
    void FinishSequence();
  }
}
