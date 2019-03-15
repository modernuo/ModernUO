using Server.Targeting;

namespace Server.Spells
{
  public interface ISpellTargetingMobile : ISpell
  {
    void Target(Mobile from);
  }

  public class SpellTargetMobile : Target
  {
    public ISpellTargetingMobile Spell{ get; }

    public SpellTargetMobile(ISpellTargetingMobile spell, TargetFlags flags, int range = 12) : base(range, false, flags)
    {
      Spell = spell;
    }

    protected override void OnTarget(Mobile from, object o)
    {
      Spell.Target(o as Mobile);
    }

    protected override void OnTargetFinish(Mobile from)
    {
      Spell?.FinishSequence();
    }
  }
}
