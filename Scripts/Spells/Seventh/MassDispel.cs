using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Seventh
{
  public class MassDispelSpell : MagerySpell
  {
    private static SpellInfo m_Info = new SpellInfo(
      "Mass Dispel", "Vas An Ort",
      263,
      9002,
      Reagent.Garlic,
      Reagent.MandrakeRoot,
      Reagent.BlackPearl,
      Reagent.SulfurousAsh
    );

    public MassDispelSpell(Mobile caster, Item scroll) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Seventh;

    public override void OnCast()
    {
      Caster.Target = new InternalTarget(this);
    }

    public void Target(IPoint3D p)
    {
      if (!Caster.CanSee(p))
      {
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      }
      else if (CheckSequence())
      {
        SpellHelper.Turn(Caster, p);

        SpellHelper.GetSurfaceTop(ref p);

        List<Mobile> targets = new List<Mobile>();

        Map map = Caster.Map;

        if (map != null)
        {
          IPooledEnumerable<BaseCreature> eable = map.GetMobilesInRange<BaseCreature>(new Point3D(p), 8);

          foreach (BaseCreature m in eable)
            if (m.IsDispellable && Caster.CanBeHarmful(m, false))
              targets.Add(m);

          eable.Free();
        }

        for (int i = 0; i < targets.Count; ++i)
        {
          Mobile m = targets[i];

          if (!(m is BaseCreature bc))
            continue;

          double dispelChance =
            (50.0 + 100 * (Caster.Skills.Magery.Value - bc.DispelDifficulty) / (bc.DispelFocus * 2)) / 100;

          if (dispelChance > Utility.RandomDouble())
          {
            Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration),
              0x3728, 8, 20, 5042);
            Effects.PlaySound(m, m.Map, 0x201);

            m.Delete();
          }
          else
          {
            Caster.DoHarmful(m);

            m.FixedEffect(0x3779, 10, 20);
          }
        }
      }

      FinishSequence();
    }

    private class InternalTarget : Target
    {
      private MassDispelSpell m_Owner;

      public InternalTarget(MassDispelSpell owner) : base(Core.ML ? 10 : 12, true, TargetFlags.None)
      {
        m_Owner = owner;
      }

      protected override void OnTarget(Mobile from, object o)
      {
        if (o is IPoint3D p)
          m_Owner.Target(p);
      }

      protected override void OnTargetFinish(Mobile from)
      {
        m_Owner.FinishSequence();
      }
    }
  }
}