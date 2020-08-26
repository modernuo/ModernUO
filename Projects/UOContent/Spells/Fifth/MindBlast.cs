using System;
using Server.Targeting;

namespace Server.Spells.Fifth
{
  public class MindBlastSpell : MagerySpell, ISpellTargetingMobile
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Mind Blast", "Por Corp Wis",
      218,
      Core.AOS ? 9002 : 9032,
      Reagent.BlackPearl,
      Reagent.MandrakeRoot,
      Reagent.Nightshade,
      Reagent.SulfurousAsh);

    public MindBlastSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
      if (Core.AOS)
        m_Info.LeftHandEffect = m_Info.RightHandEffect = 9002;
    }

    public override SpellCircle Circle => SpellCircle.Fifth;

    public override bool DelayedDamage => !Core.AOS;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
    }

    private void AosDelay_Callback(Mobile caster, Mobile target, Mobile defender, int damage)
    {
      if (caster.HarmfulCheck(defender))
      {
        SpellHelper.Damage(this, target, Utility.RandomMinMax(damage, damage + 4), 0, 0, 100, 0, 0);

        target.FixedParticles(0x374A, 10, 15, 5038, 1181, 2, EffectLayer.Head);
        target.PlaySound(0x213);
      }
    }

    public void Target(Mobile m)
    {
      if (m == null)
        return;

      if (!Caster.CanSee(m))
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      else if (Core.AOS)
      {
        if (Caster.CanBeHarmful(m) && CheckSequence())
        {
          Mobile from = Caster, target = m;

          SpellHelper.Turn(from, target);

          SpellHelper.CheckReflect((int)Circle, ref from, ref target);

          int damage = Math.Min((int)((Caster.Skills.Magery.Value + Caster.Int) / 5), 60);

          Timer.DelayCall(TimeSpan.FromSeconds(1.0),
            AosDelay_Callback, Caster, target, m, damage);
        }
      }
      else if (CheckHSequence(m))
      {
        Mobile from = Caster, target = m;

        SpellHelper.Turn(from, target);

        SpellHelper.CheckReflect((int)Circle, ref from, ref target);

        // Algorithm: (highestStat - lowestStat) / 2 [- 50% if resisted]

        int highestStat = target.Str, lowestStat = target.Str;

        if (target.Dex > highestStat)
          highestStat = target.Dex;

        if (target.Dex < lowestStat)
          lowestStat = target.Dex;

        if (target.Int > highestStat)
          highestStat = target.Int;

        if (target.Int < lowestStat)
          lowestStat = target.Int;

        if (highestStat > 150)
          highestStat = 150;

        if (lowestStat > 150)
          lowestStat = 150;

        double damage = Math.Min(GetDamageScalar(m) * (highestStat - lowestStat) / 2, 45); // Many users prefer 3 or 4

        if (CheckResisted(target))
        {
          damage /= 2;
          target.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
        }

        from.FixedParticles(0x374A, 10, 15, 2038, EffectLayer.Head);

        target.FixedParticles(0x374A, 10, 15, 5038, EffectLayer.Head);
        target.PlaySound(0x213);

        SpellHelper.Damage(this, target, damage, 0, 0, 100, 0, 0);
      }

      FinishSequence();
    }

    public override double GetSlayerDamageScalar(Mobile target) => 1.0;
  }
}
