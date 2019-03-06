using System.Collections.Generic;
using System.Linq;
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

        Map map = Caster.Map;

        if (map != null)
        {
          IEnumerable<BaseCreature> eable = map.GetMobilesInRange<BaseCreature>(new Point3D(p), 8)
            .Where(m => m.IsDispellable && Caster.CanBeHarmful(m, false));

          // eable.Free(); Needed?

          foreach (BaseCreature bc in eable)
          {
            double dispelChance =
              (50.0 + 100 * (Caster.Skills.Magery.Value - bc.DispelDifficulty) / (bc.DispelFocus * 2)) / 100;

            if (dispelChance > Utility.RandomDouble())
            {
              Effects.SendLocationParticles(EffectItem.Create(bc.Location, bc.Map, EffectItem.DefaultDuration),
                0x3728, 8, 20, 5042);
              Effects.PlaySound(bc, bc.Map, 0x201);

              bc.Delete();
            }
            else
            {
              Caster.DoHarmful(bc);

              bc.FixedEffect(0x3779, 10, 20);
            }
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
