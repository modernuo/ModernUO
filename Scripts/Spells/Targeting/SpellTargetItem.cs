using Server.Targeting;

namespace Server.Spells
{
  public interface ISpellTargetingItem : ISpell
  {
    void Target(Item item);
  }

  public class SpellTargetItem : Target
  {
    public ISpellTargetingItem Spell{ get; }

    public SpellTargetItem(ISpellTargetingItem spell, TargetFlags flags, int range = 12) : base(range, false, flags)
    {
      Spell = spell;
    }

    protected override void OnTarget(Mobile from, object o)
    {
      if (o is Item item)
        Spell.Target(item);
    }

    protected override void OnTargetFinish(Mobile from)
    {
      Spell?.FinishSequence();
    }
  }
}
