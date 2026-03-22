using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.First
{
    public class ReactiveArmorSpell : MagerySpell, ITargetingSpell<Mobile>
    {
        private static readonly SpellInfo _info = new(
            "Reactive Armor",
            "Flam Sanct",
            236,
            9011,
            Reagent.Garlic,
            Reagent.SpidersSilk,
            Reagent.SulfurousAsh
        );

        private static Dictionary<Mobile, ResistanceMod[]> _table;
        private static Dictionary<Mobile, TimerExecutionToken> _t2aTable;

        public ReactiveArmorSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.First;

        public static bool HasEffect(Mobile m) => _t2aTable?.ContainsKey(m) == true;

        public static void RemoveEffect(Mobile m)
        {
            if (_t2aTable?.Remove(m, out var token) == true)
            {
                token.Cancel();
            }
        }

        public static void HandleMeleeHit(Mobile attacker, Mobile defender, ref int damage)
        {
            if (!Core.UOR)
            {
                // T2A: percentage-based melee reflection
                if (!HasEffect(defender))
                {
                    return;
                }

                // Only reflect adjacent melee hits; guards are exempt
                if (!defender.InRange(attacker, 1) || attacker is BaseGuard)
                {
                    return;
                }

                var magery = defender.Skills.Magery.Value;
                var reflectPct = (int)(10 + magery / 4); // 10–35%
                var reflected = damage * reflectPct / 100;

                if (reflected > 0)
                {
                    damage -= reflected;

                    // Sourceless damage — does not trigger attacker's own combat events
                    attacker.Damage(reflected);
                }

                attacker.PlaySound(0x1F1);
                attacker.FixedEffect(0x374A, 10, 16);
            }
            else
            {
                // UOR: damage absorption pool
                var absorb = defender.MeleeDamageAbsorb;

                if (absorb <= 0)
                {
                    return;
                }

                if (absorb > damage)
                {
                    var react = Math.Max(damage / 5, 1);

                    defender.MeleeDamageAbsorb -= damage;
                    damage = 0;

                    attacker.Damage(react, defender);

                    attacker.PlaySound(0x1F1);
                    attacker.FixedEffect(0x374A, 10, 16);
                }
                else
                {
                    defender.MeleeDamageAbsorb = 0;
                    defender.SendLocalizedMessage(1005556); // Your reactive armor spell has been nullified.
                    DefensiveSpell.Nullify(defender);
                }
            }
        }

        public override bool CheckCast()
        {
            if (Core.AOS || !Core.UOR)
            {
                return true;
            }

            if (Caster.MeleeDamageAbsorb > 0)
            {
                Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                return false;
            }

            if (!Caster.CanBeginAction<DefensiveSpell>())
            {
                Caster.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
                return false;
            }

            return true;
        }

        public void Target(Mobile m)
        {
            if (!Caster.CanBeBeneficial(m))
            {
                return;
            }

            if (HasEffect(m))
            {
                // This target already has Reactive Armor
                Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
                return;
            }

            if (CheckBSequence(m))
            {
                Caster.DoBeneficial(m);
                SpellHelper.Turn(Caster, m);

                m.FixedParticles(0x376A, 9, 32, 5008, EffectLayer.Waist);
                m.PlaySound(0x1F2);

                var duration = TimeSpan.FromSeconds(25 + Caster.Skills.Magery.Value / 2); // 25–75s

                Timer.StartTimer(duration, () => ExpireT2AEffect(m), out var token);
                _t2aTable ??= [];
                _t2aTable[m] = token;
            }
        }

        private static void ExpireT2AEffect(Mobile m) => _t2aTable?.Remove(m);

        public override void OnCast()
        {
            if (Core.AOS)
            {
                /* The reactive armor spell increases the caster's physical resistance, while lowering the caster's elemental resistances.
                 * 15 + (Inscription/20) Physcial bonus
                 * -5 Elemental
                 * The reactive armor spell has an indefinite duration, becoming active when cast, and deactivated when re-cast.
                 * Reactive Armor, Protection, and Magic Reflection will stay on even after logging out,
                 * even after dying, until you turn them off by casting them again.
                 * (+20 physical -5 elemental at 100 Inscription)
                 */

                if (CheckSequence())
                {
                    if (_table?.Remove(Caster, out var mods) == true)
                    {
                        Caster.PlaySound(0x1ED);
                        Caster.FixedParticles(0x376A, 9, 32, 5008, EffectLayer.Waist);

                        for (var i = 0; i < mods.Length; ++i)
                        {
                            Caster.RemoveResistanceMod(mods[i]);
                        }

                        (Caster as PlayerMobile)?.RemoveBuff(BuffIcon.ReactiveArmor);
                    }
                    else
                    {
                        Caster.PlaySound(0x1E9);
                        Caster.FixedParticles(0x376A, 9, 32, 5008, EffectLayer.Waist);

                        mods =
                        [
                            new ResistanceMod(
                                ResistanceType.Physical,
                                "PhysicalResistReactiveArmorSpell",
                                15 + (int)(Caster.Skills.Inscribe.Value / 20)
                            ),
                            new ResistanceMod(ResistanceType.Fire, "FireResistReactiveArmorSpell", -5),
                            new ResistanceMod(ResistanceType.Cold, "ColdResistReactiveArmorSpell", -5),
                            new ResistanceMod(ResistanceType.Poison, "PoisonResistReactiveArmorSpell", -5),
                            new ResistanceMod(ResistanceType.Energy, "EnergyResistReactiveArmorSpell", -5)
                        ];

                        _table ??= [];
                        _table[Caster] = mods;

                        for (var i = 0; i < mods.Length; ++i)
                        {
                            Caster.AddResistanceMod(mods[i]);
                        }

                        var args = $"{15 + (int)(Caster.Skills.Inscribe.Value / 20)}\t{5}\t{5}\t{5}\t{5}";

                        (Caster as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.ReactiveArmor, 1075812, 1075813, args: args));
                    }
                }

                FinishSequence();
            }
            else if (!Core.UOR)
            {
                Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Beneficial);
            }
            else
            {
                if (Caster.MeleeDamageAbsorb > 0)
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
                        var value = Math.Clamp(
                            (int)(Caster.Skills.Magery.Value + Caster.Skills.Meditation.Value +
                                  Caster.Skills.Inscribe.Value) / 3,
                            1,
                            75
                        );

                        Caster.MeleeDamageAbsorb = value;

                        Caster.FixedParticles(0x376A, 9, 32, 5008, EffectLayer.Waist);
                        Caster.PlaySound(0x1F2);
                    }
                    else
                    {
                        Caster.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
                    }
                }

                FinishSequence();
            }
        }

        [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
        public static void EndArmor(Mobile m)
        {
            RemoveEffect(m);

            if (_table?.Remove(m, out var mods) != true)
            {
                return;
            }

            for (var i = 0; i < mods?.Length; ++i)
            {
                m.RemoveResistanceMod(mods[i]);
            }

            (m as PlayerMobile)?.RemoveBuff(BuffIcon.ReactiveArmor);
        }
    }
}
