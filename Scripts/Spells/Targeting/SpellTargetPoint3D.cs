using System;
using Server.Targeting;

namespace Server.Spells
{
  public interface ISpellTargetingPoint3D : ISpell
  {
    void Target(IPoint3D p);
  }

  public class SpellTargetPoint3D : Target
  {
    public ISpellTargetingPoint3D Spell{ get; private set; }
    private bool m_CheckLOS;

    public SpellTargetPoint3D(ISpellTargetingPoint3D spell, TargetFlags flags = TargetFlags.None, int range = 12, bool checkLOS = true) : base(range, true, flags)
    {
      Spell = spell;
      m_CheckLOS = checkLOS;
    }

    protected override void OnTarget(Mobile from, object o)
    {
      if (o is IPoint3D p)
        Spell.Target(p);
    }

    protected override void OnTargetOutOfLOS(Mobile from, object o)
    {
      if (!m_CheckLOS)
        return;

      from.SendLocalizedMessage(501943); // Target cannot be seen. Try again.
      from.Target = new SpellTargetPoint3D(Spell);
      from.Target.BeginTimeout(from, TimeoutTime - DateTime.UtcNow);
      Spell = null;
    }

    protected override void OnTargetFinish(Mobile from)
    {
      Spell?.FinishSequence();
    }
  }
}
