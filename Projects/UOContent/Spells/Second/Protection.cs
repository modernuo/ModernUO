using System;
using System.Collections.Generic;

namespace Server.Spells.Second
{
    public class ProtectionSpell : MagerySpell
    {
        private static readonly SpellInfo _info = new(
            "Protection",
            "Uus Sanct",
            236,
            9011,
            Reagent.Garlic,
            Reagent.Ginseng,
            Reagent.SulfurousAsh
        );

        private static readonly Dictionary<Mobile, Tuple<ResistanceMod, DefaultSkillMod>> _table =
            new();

        public ProtectionSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public static Dictionary<Mobile, int> Registry { get; } = new();

        public override SpellCircle Circle => SpellCircle.Second;

        public override bool CheckCast()
        {
            if (Core.AOS)
            {
                return true;
            }

            if (Registry.ContainsKey(Caster))
            {
                Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                return false;
            }

            if (Caster.CanBeginAction<DefensiveSpell>())
            {
                return true;
            }

            Caster.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
            return false;
        }

        // AOS+ only
        public static void Toggle(Mobile caster, Mobile target)
        {
            /* Players under the protection spell effect can no longer have their spells "disrupted" when hit.
             * Players under the protection spell have decreased physical resistance stat value (-15 + (Inscription/20),
             * a decreased "resisting spells" skill value by -35 + (Inscription/20),
             * and a slower casting speed modifier (technically, a negative "faster cast speed") of 2 points.
             * The protection spell has an indefinite duration, becoming active when cast, and deactivated when re-cast.
             * Reactive Armor, Protection, and Magic Reflection will stay on even after logging out,
             * even after dying, until you turn them off by casting them again.
             */

            if (_table.Remove(target, out var mods))
            {
                target.PlaySound(0x1ED);
                target.FixedParticles(0x375A, 9, 20, 5016, EffectLayer.Waist);

                Registry.Remove(target);

                target.RemoveResistanceMod(mods.Item1);
                target.RemoveSkillMod(mods.Item2);

                BuffInfo.RemoveBuff(target, BuffIcon.Protection);
            }
            else
            {
                target.PlaySound(0x1E9);
                target.FixedParticles(0x375A, 9, 20, 5016, EffectLayer.Waist);

                var physLoss = -15 + (int)(caster.Skills.Inscribe.Value / 20);
                var resistLoss = -35 + (int)(caster.Skills.Inscribe.Value / 20);
                var physMod = new ResistanceMod(ResistanceType.Physical, "PhysicalResistProtectionSpell", physLoss);
                var resistMod = new DefaultSkillMod(SkillName.MagicResist, "MagicResistProtectionSpell", true, resistLoss);

                _table[target] = Tuple.Create(physMod, resistMod);
                Registry[target] = 1000; // 100.0% protection from disruption

                target.AddResistanceMod(physMod);
                target.AddSkillMod(resistMod);

                var args = $"{physLoss}\t{resistLoss}";
                BuffInfo.AddBuff(target, new BuffInfo(BuffIcon.Protection, 1075814, 1075815, args));
            }
        }

        public static void EndProtection(Mobile m)
        {
            if (!_table.Remove(m, out var mods))
            {
                return;
            }

            Registry.Remove(m);

            m.RemoveResistanceMod(mods.Item1);
            m.RemoveSkillMod(mods.Item2);

            BuffInfo.RemoveBuff(m, BuffIcon.Protection);
        }

        public override void OnCast()
        {
            if (Core.AOS)
            {
                if (CheckSequence())
                {
                    Toggle(Caster, Caster);
                }

                FinishSequence();
            }
            else
            {
                if (Registry.ContainsKey(Caster))
                {
                    Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                }
                else if (!Caster.CanBeginAction<DefensiveSpell>())
                {
                    Caster.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
                }
                else if (CheckSequence())
                {
                    if (Caster.BeginAction<DefensiveSpell>())
                    {
                        double value = (Caster.Skills.EvalInt.Value +
                                        Caster.Skills.Meditation.Value +
                                        Caster.Skills.Inscribe.Value) * 10 / 4;

                        Registry.Add(Caster, Math.Clamp((int)value, 0, 750)); // 75.0% protection from disruption
                        new InternalTimer(Caster).Start();

                        Caster.FixedParticles(0x375A, 9, 20, 5016, EffectLayer.Waist);
                        Caster.PlaySound(0x1ED);
                    }
                    else
                    {
                        Caster.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
                    }
                }

                FinishSequence();
            }
        }

        private class InternalTimer : Timer
        {
            private readonly Mobile m_Caster;

            public InternalTimer(Mobile caster) : base(TimeSpan.FromSeconds(0))
            {
                var val = Math.Clamp(caster.Skills.Magery.Value * 2.0, 15, 240);

                m_Caster = caster;
                Delay = TimeSpan.FromSeconds(val);
            }

            protected override void OnTick()
            {
                Registry.Remove(m_Caster);
                DefensiveSpell.Nullify(m_Caster);
            }
        }
    }
}
