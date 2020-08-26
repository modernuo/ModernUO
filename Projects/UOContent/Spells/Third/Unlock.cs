using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Third
{
  public class UnlockSpell : MagerySpell, ISpellTargetingPoint3D
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Unlock Spell", "Ex Por",
      215,
      9001,
      Reagent.Bloodmoss,
      Reagent.SulfurousAsh);

    public UnlockSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Third;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetPoint3D(this, TargetFlags.None, Core.ML ? 10 : 12);
    }

    public void Target(IPoint3D p)
    {
      if (CheckSequence())
      {
        SpellHelper.Turn(Caster, p);

        Effects.SendLocationParticles(EffectItem.Create(new Point3D(p), Caster.Map, EffectItem.DefaultDuration),
          0x376A, 9, 32, 5024);

        Effects.PlaySound(p, Caster.Map, 0x1FF);

        if (p is Mobile)
          Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503101); // That did not need to be unlocked.
        else if (!(p is LockableContainer cont))
          Caster.SendLocalizedMessage(501666); // You can't unlock that!
        else
        {
          if (BaseHouse.CheckSecured(cont))
            Caster.SendLocalizedMessage(503098); // You cannot cast this on a secure item.
          else if (!cont.Locked)
            Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2,
              503101); // That did not need to be unlocked.
          else if (cont.LockLevel == 0)
            Caster.SendLocalizedMessage(501666); // You can't unlock that!
          else
          {
            int level = (int)(Caster.Skills.Magery.Value * 0.8) - 4;

            if (level >= cont.RequiredSkill &&
                !(cont is TreasureMapChest chest && chest.Level > 2))
            {
              cont.Locked = false;

              if (cont.LockLevel == -255)
                cont.LockLevel = cont.RequiredSkill - 10;
            }
            else
              Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2,
                503099); // My spell does not seem to have an effect on that lock.
          }
        }
      }

      FinishSequence();
    }
  }
}
