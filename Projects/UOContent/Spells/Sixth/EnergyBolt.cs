using Server.Targeting;

namespace Server.Spells.Sixth
{
  public class EnergyBoltSpell : MagerySpell, ISpellTargetingMobile
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Energy Bolt", "Corp Por",
      230,
      9022,
      Reagent.BlackPearl,
      Reagent.Nightshade);

    public EnergyBoltSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Sixth;

    public override bool DelayedDamage => true;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
    }

    public void Target(Mobile m)
    {
      if (m == null)
        return;

      if (!Caster.CanSee(m))
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      else if (CheckHSequence(m))
      {
        Mobile source = Caster;

        SpellHelper.Turn(Caster, m);

        SpellHelper.CheckReflect((int)Circle, ref source, ref m);

        double damage;

        if (Core.AOS)
        {
          damage = GetNewAosDamage(40, 1, 5, m);
        }
        else
        {
          damage = Utility.Random(24, 18);

          if (CheckResisted(m))
          {
            damage *= 0.75;

            m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
          }

          // Scale damage based on evalint and resist
          damage *= GetDamageScalar(m);
        }

        // Do the effects
        source.MovingParticles(m, 0x379F, 7, 0, false, true, 3043, 4043, 0x211);
        source.PlaySound(0x20A);

        // Deal the damage
        SpellHelper.Damage(this, m, damage, 0, 0, 0, 0, 100);
      }

      FinishSequence();
    }
  }
}
