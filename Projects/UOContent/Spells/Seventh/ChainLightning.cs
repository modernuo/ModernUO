using System.Collections.Generic;
using System.Linq;
using Server.Targeting;

namespace Server.Spells.Seventh
{
  public class ChainLightningSpell : MagerySpell, ISpellTargetingPoint3D
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Chain Lightning", "Vas Ort Grav",
      209,
      9022,
      false,
      Reagent.BlackPearl,
      Reagent.Bloodmoss,
      Reagent.MandrakeRoot,
      Reagent.SulfurousAsh);

    public ChainLightningSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Seventh;

    public override bool DelayedDamage => true;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetPoint3D(this, TargetFlags.None, Core.ML ? 10 : 12);
    }

    public void Target(IPoint3D p)
    {
      if (!Caster.CanSee(p))
      {
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      }
      else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
      {
        SpellHelper.Turn(Caster, p);

        if (p is Item item)
          p = item.GetWorldLocation();

        List<Mobile> targets = new List<Mobile>();

        Map map = Caster.Map;

        bool playerVsPlayer = false;

        if (map != null)
        {
          IPooledEnumerable<Mobile> eable = map.GetMobilesInRange(new Point3D(p), 2);

          targets.AddRange(eable.Where(m =>
          {
            if ((Core.AOS && (m == Caster || !Caster.InLOS(m))) || !SpellHelper.ValidIndirectTarget(Caster, m) ||
                !Caster.CanBeHarmful(m, false))
              return false;

            if (m.Player)
              playerVsPlayer = true;

            return true;
          }).ToList());

          eable.Free();
        }

        double damage;

        damage = Core.AOS ? GetNewAosDamage(51, 1, 5, playerVsPlayer)
          : Utility.Random(27, 22);

        if (targets.Count > 0)
        {
          if (Core.AOS && targets.Count > 2)
            damage = damage * 2 / targets.Count;
          else if (!Core.AOS)
            damage /= targets.Count;

          for (int i = 0; i < targets.Count; ++i)
          {
            double toDeal = damage;
            Mobile m = targets[i];

            if (!Core.AOS && CheckResisted(m))
            {
              toDeal *= 0.5;

              m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
            }

            toDeal *= GetDamageScalar(m);
            Caster.DoHarmful(m);
            SpellHelper.Damage(this, m, toDeal, 0, 0, 0, 0, 100);

            m.BoltEffect(0);
          }
        }
        else
        {
          Caster.PlaySound(0x29);
        }
      }

      FinishSequence();
    }
  }
}
