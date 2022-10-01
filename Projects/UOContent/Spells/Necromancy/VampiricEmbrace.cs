using System;
using Server.Items;

namespace Server.Spells.Necromancy
{
    public class VampiricEmbraceSpell : TransformationSpell
    {
        private static readonly SpellInfo _info = new(
            "Vampiric Embrace",
            "Rel Xen An Sanct",
            203,
            9031,
            Reagent.BatWing,
            Reagent.NoxCrystal,
            Reagent.PigIron
        );

        public VampiricEmbraceSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);

        public override double RequiredSkill => 99.0;
        public override int RequiredMana => 23;

        public override int Body => Caster.Female ? 745 : 744;
        public override int Hue => 0x847E;

        public override int FireResistOffset => -25;

        public override void GetCastSkills(out double min, out double max)
        {
            if (Caster.Skills[CastSkill].Value >= RequiredSkill)
            {
                min = 80.0;
                max = 120.0;
            }
            else
            {
                base.GetCastSkills(out min, out max);
            }
        }

        public override void DoEffect(Mobile m)
        {
            Effects.SendLocationParticles(
                EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration),
                0x373A,
                1,
                17,
                1108,
                7,
                9914,
                0
            );
            Effects.SendLocationParticles(
                EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration),
                0x376A,
                1,
                22,
                67,
                7,
                9502,
                0
            );
            Effects.PlaySound(m.Location, m.Map, 0x4B1);
        }
    }
}
