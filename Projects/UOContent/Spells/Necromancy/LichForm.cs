using System;

namespace Server.Spells.Necromancy
{
    public class LichFormSpell : TransformationSpell
    {
        private static readonly SpellInfo m_Info = new(
            "Lich Form",
            "Rel Xen Corp Ort",
            203,
            9031,
            Reagent.GraveDust,
            Reagent.DaemonBlood,
            Reagent.NoxCrystal
        );

        public LichFormSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);

        public override double RequiredSkill => 70.0;
        public override int RequiredMana => 23;

        public override int Body => 749;

        public override int FireResistOffset => -10;
        public override int ColdResistOffset => +10;
        public override int PoisResistOffset => +10;

        public override double TickRate => 2.5;

        public override void DoEffect(Mobile m)
        {
            m.PlaySound(0x19C);
            m.FixedParticles(0x3709, 1, 30, 9904, 1108, 6, EffectLayer.RightFoot);
        }

        public override void OnTick(Mobile m)
        {
            --m.Hits;
        }
    }
}
