using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
  public class PoisonStrikeSpell : NecromancerSpell, ISpellTargetingMobile
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Poison Strike", "In Vas Nox",
      203,
      9031,
      Reagent.NoxCrystal);

    public PoisonStrikeSpell(Mobile caster, Item scroll = null)
      : base(caster, scroll, m_Info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(Core.ML ? 1.75 : 1.5);

    public override double RequiredSkill => 50.0;
    public override int RequiredMana => 17;

    public override bool DelayedDamage => false;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
    }

    public void Target(Mobile m)
    {
      if (m == null)
        return;

      if (CheckHSequence(m))
      {
        SpellHelper.Turn(Caster, m);

        /* Creates a blast of poisonous energy centered on the target.
         * The main target is inflicted with a large amount of Poison damage, and all valid targets in a radius of 2 tiles around the main target are inflicted with a lesser effect.
         * One tile from main target receives 50% damage, two tiles from target receives 33% damage.
         */

        // CheckResisted( m );
        // Check magic resist for skill, but do not use return value
        // reports from OSI:  Necro spells don't give Resist gain

        Effects.SendLocationParticles(EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration), 0x36B0, 1,
          14, 63, 7, 9915, 0);
        Effects.PlaySound(m.Location, m.Map, 0x229);

        double damage = Utility.RandomMinMax(Core.ML ? 32 : 36, 40) * ((300 + GetDamageSkill(Caster) * 9) / 1000);

        double sdiBonus = (double)AosAttributes.GetValue(Caster, AosAttribute.SpellDamage) / 100;
        double pvmDamage = damage * (1 + sdiBonus);

        if (Core.ML && sdiBonus > 0.15)
          sdiBonus = 0.15;
        double pvpDamage = damage * (1 + sdiBonus);

        Map map = m.Map;

        if (map != null)
        {
          List<Mobile> targets = new List<Mobile>();

          if (Caster.CanBeHarmful(m, false))
            targets.Add(m);

          targets.AddRange(m.GetMobilesInRange(2)
            .Where(targ => !(Caster is BaseCreature && targ is BaseCreature && targ != Caster && m != targ && SpellHelper.ValidIndirectTarget(Caster, targ) && Caster.CanBeHarmful(targ, false))));

          for (int i = 0; i < targets.Count; ++i)
          {
            Mobile targ = targets[i];
            int num;

            if (targ.InRange(m.Location, 0))
              num = 1;
            else if (targ.InRange(m.Location, 1))
              num = 2;
            else
              num = 3;

            Caster.DoHarmful(targ);
            SpellHelper.Damage(this, targ, (m.Player && Caster.Player ? pvpDamage : pvmDamage) / num, 0, 0, 0,
              100, 0);
          }
        }
      }

      FinishSequence();
    }
  }
}
