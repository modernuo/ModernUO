using Server.Targeting;

namespace Server.Spells.Third
{
  public class FireballSpell : MagerySpell, ISpellTargetingMobile
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Fireball", "Vas Flam",
      203,
      9041,
      Reagent.BlackPearl);

    public FireballSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Third;

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

        SpellHelper.Turn(source, m);

        SpellHelper.CheckReflect((int)Circle, ref source, ref m);

        double damage;

        if (Core.AOS)
        {
          damage = GetNewAosDamage(19, 1, 5, m);
        }
        else
        {
          damage = Utility.Random(10, 7);

          if (CheckResisted(m))
          {
            damage *= 0.75;

            m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
          }

          damage *= GetDamageScalar(m);
        }

        source.MovingParticles(m, 0x36D4, 7, 0, false, true, 9502, 4019, 0x160);
        source.PlaySound(Core.AOS ? 0x15E : 0x44B);

        SpellHelper.Damage(this, m, damage, 0, 100, 0, 0, 0);
      }

      FinishSequence();
    }
  }
}
