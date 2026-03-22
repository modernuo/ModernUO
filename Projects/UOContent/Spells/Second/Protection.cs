using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Second
{
    public class ProtectionSpell : MagerySpell, ITargetingSpell<Mobile>
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

        private static Dictionary<Mobile, Tuple<ResistanceMod, DefaultSkillMod>> _table;

        // T2A: stores the AR bonus per mobile (shared with ArchProtection)
        private static Dictionary<Mobile, int> _t2aTable;

        public ProtectionSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public static Dictionary<Mobile, int> Registry { get; } = new();

        public override SpellCircle Circle => SpellCircle.Second;

        public static bool HasT2AProtection(Mobile m) => _t2aTable?.ContainsKey(m) ?? false;

        public static void RemoveT2AProtection(Mobile m)
        {
            if (_t2aTable?.Remove(m, out var bonus) == true)
            {
                m.VirtualArmorMod -= Math.Min(bonus, m.VirtualArmorMod);
            }
        }

        public static void ApplyT2AProtection(Mobile caster, Mobile target)
        {
            if (HasT2AProtection(target))
            {
                return;
            }

            var magery = caster.Skills.Magery.Value;
            var bonus = (int)(magery / 10);
            var duration = TimeSpan.FromSeconds(6 * magery / 5);

            _t2aTable ??= [];
            _t2aTable[target] = bonus;
            target.VirtualArmorMod += bonus;

            target.FixedParticles(0x375A, 9, 20, 5016, EffectLayer.Waist);

            new InternalTimer(target, duration).Start();
        }

        public override bool CheckCast()
        {
            if (Core.AOS || !Core.UOR)
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

        public void Target(Mobile m)
        {
            if (!Caster.CanBeBeneficial(m))
            {
                return;
            }

            if (HasT2AProtection(m))
            {
                Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                return;
            }

            if (CheckBSequence(m))
            {
                Caster.DoBeneficial(m);
                SpellHelper.Turn(Caster, m);
                ApplyT2AProtection(Caster, m);
                m.PlaySound(0x1ED);
            }
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

            if (_table?.Remove(target, out var mods) == true)
            {
                target.PlaySound(0x1ED);
                target.FixedParticles(0x375A, 9, 20, 5016, EffectLayer.Waist);

                Registry.Remove(target);

                target.RemoveResistanceMod(mods.Item1);
                target.RemoveSkillMod(mods.Item2);

                (target as PlayerMobile)?.RemoveBuff(BuffIcon.Protection);
            }
            else
            {
                target.PlaySound(0x1E9);
                target.FixedParticles(0x375A, 9, 20, 5016, EffectLayer.Waist);

                var physLoss = -15 + (int)(caster.Skills.Inscribe.Value / 20);
                var resistLoss = -35 + (int)(caster.Skills.Inscribe.Value / 20);
                var physMod = new ResistanceMod(ResistanceType.Physical, "PhysicalResistProtectionSpell", physLoss);
                var resistMod = new DefaultSkillMod(SkillName.MagicResist, "MagicResistProtectionSpell", true, resistLoss);

                _table ??= [];
                _table[target] = Tuple.Create(physMod, resistMod);
                Registry[target] = 1000; // 100.0% protection from disruption

                target.AddResistanceMod(physMod);
                target.AddSkillMod(resistMod);

                var args = $"{physLoss}\t{resistLoss}";
                (target as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.Protection, 1075814, 1075815, args: args));
            }
        }

        [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
        public static void EndProtection(Mobile m)
        {
            RemoveT2AProtection(m);

            if (_table?.Remove(m, out var mods) != true)
            {
                return;
            }

            Registry.Remove(m);

            m.RemoveResistanceMod(mods.Item1);
            m.RemoveSkillMod(mods.Item2);

            (m as PlayerMobile)?.RemoveBuff(BuffIcon.Protection);
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
            else if (!Core.UOR)
            {
                Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Beneficial);
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
                        var value = (Caster.Skills.EvalInt.Value +
                                     Caster.Skills.Meditation.Value +
                                     Caster.Skills.Inscribe.Value) * 10 / 4;

                        Registry.Add(Caster, Math.Clamp((int)value, 0, 750)); // 75.0% protection from disruption
                        var duration = TimeSpan.FromSeconds(Math.Clamp(Caster.Skills.Magery.Value * 2.0, 15, 240));
                        new InternalTimer(Caster, duration).Start();

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
            private readonly Mobile _mobile;

            public InternalTimer(Mobile mobile, TimeSpan duration) : base(duration) => _mobile = mobile;

            protected override void OnTick()
            {
                if (!Core.UOR)
                {
                    RemoveT2AProtection(_mobile);
                }
                else
                {
                    Registry.Remove(_mobile);
                    DefensiveSpell.Nullify(_mobile);
                }
            }
        }
    }
}
