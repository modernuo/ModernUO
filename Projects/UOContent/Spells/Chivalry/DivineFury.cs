using System;
using System.Collections.Generic;

namespace Server.Spells.Chivalry
{
    public class DivineFurySpell : PaladinSpell
    {
        private static readonly SpellInfo m_Info = new(
            "Divine Fury",
            "Divinum Furis",
            -1,
            9002
        );

        private static readonly Dictionary<Mobile, Timer> m_Table = new();

        public DivineFurySpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

        public override double RequiredSkill => 25.0;
        public override int RequiredMana => 15;
        public override int RequiredTithing => 10;
        public override int MantraNumber => 1060722; // Divinum Furis
        public override bool BlocksMovement => false;

        public override void OnCast()
        {
            if (CheckSequence())
            {
                Caster.PlaySound(0x20F);
                Caster.PlaySound(Caster.Female ? 0x338 : 0x44A);
                Caster.FixedParticles(0x376A, 1, 31, 9961, 1160, 0, EffectLayer.Waist);
                Caster.FixedParticles(0x37C4, 1, 31, 9502, 43, 2, EffectLayer.Waist);

                Caster.Stam = Caster.StamMax;

                m_Table.TryGetValue(Caster, out var timer);
                timer?.Stop();

                var delay = Math.Clamp(ComputePowerValue(10), 7, 24);

                m_Table[Caster] = Timer.DelayCall(TimeSpan.FromSeconds(delay), Expire_Callback, Caster);
                Caster.Delta(MobileDelta.WeaponDamage);

                BuffInfo.AddBuff(
                    Caster,
                    new BuffInfo(BuffIcon.DivineFury, 1060589, 1075634, TimeSpan.FromSeconds(delay), Caster)
                );
            }

            FinishSequence();
        }

        public static bool UnderEffect(Mobile m) => m_Table.ContainsKey(m);

        private static void Expire_Callback(Mobile m)
        {
            m_Table.Remove(m);

            m.Delta(MobileDelta.WeaponDamage);
            m.PlaySound(0xF8);
        }
    }
}
