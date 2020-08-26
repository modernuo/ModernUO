using Server.Targeting;

namespace Server.Spells
{
  public interface ISpellTargetingItem : ISpell
  {
    void Target(Item item);
  }

  public class SpellTargetItem : Target, ISpellTarget
  {
    private readonly ISpellTargetingItem m_Spell;
    public ISpell Spell => m_Spell;

    public SpellTargetItem(ISpellTargetingItem spell, TargetFlags flags, int range = 12) : base(range, false, flags) => m_Spell = spell;

    protected override void OnTarget(Mobile from, object o)
    {
      if (o is Item item)
        m_Spell.Target(item);
    }

    protected override void OnTargetFinish(Mobile from)
    {
      m_Spell?.FinishSequence();
    }
  }
}
