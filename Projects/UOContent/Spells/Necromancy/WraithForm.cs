using System;
using Server.Mobiles;

namespace Server.Spells.Necromancy
{
    public class WraithFormSpell : TransformationSpell
    {
        private static readonly SpellInfo _info = new(
            "Wraith Form",
            "Rel Xen Um",
            203,
            9031,
            Reagent.NoxCrystal,
            Reagent.PigIron
        );

        public WraithFormSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);

        public override double RequiredSkill => 20.0;
        public override int RequiredMana => 17;

        public override int Body => Caster.Female ? 747 : 748;
        public override int Hue => Caster.Female ? 0 : 0x4001;

        public override int PhysResistOffset => +15;
        public override int FireResistOffset => -5;
        public override int ColdResistOffset => 0;
        public override int PoisResistOffset => 0;
        public override int NrgyResistOffset => -5;

        public override void DoEffect(Mobile m)
        {
            if (m is PlayerMobile mobile)
            {
                mobile.IgnoreMobiles = true;
            }

            m.PlaySound(0x17F);
            m.FixedParticles(0x374A, 1, 15, 9902, 1108, 4, EffectLayer.Waist);
        }

        public override void RemoveEffect(Mobile m)
        {
            if (m is PlayerMobile { AccessLevel: AccessLevel.Player } mobile)
            {
                mobile.IgnoreMobiles = false;
            }
        }
    }
}
