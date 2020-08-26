using Server.Targeting;

namespace Server.Spells
{
  public interface ISpellTargetingMobile : ISpell
  {
    void Target(Mobile from);
  }

  public class SpellTargetMobile : Target, ISpellTarget
  {
    private readonly ISpellTargetingMobile m_Spell;
    public ISpell Spell => m_Spell;

    public SpellTargetMobile(ISpellTargetingMobile spell, TargetFlags flags, int range = 12) : base(range, false, flags) => m_Spell = spell;

    protected override void OnTarget(Mobile from, object o)
    {
      m_Spell.Target(o as Mobile);
    }

    protected override void OnTargetFinish(Mobile from)
    {
      m_Spell?.FinishSequence();
    }
  }
}
