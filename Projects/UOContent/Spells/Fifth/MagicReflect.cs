using System.Collections.Generic;

namespace Server.Spells.Fifth
{
    public class MagicReflectSpell : MagerySpell
    {
        private static readonly SpellInfo _info = new(
            "Magic Reflection",
            "In Jux Sanct",
            242,
            9012,
            Reagent.Garlic,
            Reagent.MandrakeRoot,
            Reagent.SpidersSilk
        );

        private static readonly Dictionary<Mobile, ResistanceMod[]> _table = new();

        public MagicReflectSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fifth;

        public override bool CheckCast()
        {
            if (Core.AOS)
            {
                return true;
            }

            if (Caster.MagicDamageAbsorb > 0)
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

        public override void OnCast()
        {
            if (Core.AOS)
            {
                /* The magic reflection spell decreases the caster's physical resistance, while increasing the caster's elemental resistances.
                 * Physical decrease = 25 - (Inscription/20).
                 * Elemental resistance = +10 (-20 physical, +10 elemental at GM Inscription)
                 * The magic reflection spell has an indefinite duration, becoming active when cast, and deactivated when re-cast.
                 * Reactive Armor, Protection, and Magic Reflection will stay on even after logging out,
                 * even after dying, until you turn them off by casting them again.
                 */

                if (CheckSequence())
                {
                    var targ = Caster;

                    if (_table.Remove(targ, out var mods))
                    {
                        targ.PlaySound(0x1ED);
                        targ.FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);

                        for (var i = 0; i < mods.Length; ++i)
                        {
                            targ.RemoveResistanceMod(mods[i]);
                        }

                        BuffInfo.RemoveBuff(targ, BuffIcon.MagicReflection);
                    }
                    else
                    {
                        targ.PlaySound(0x1E9);
                        targ.FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);

                        var physiMod = -25 + (int)(targ.Skills.Inscribe.Value / 20);
                        const int otherMod = 10;

                        mods = new[]
                        {
                            new ResistanceMod(ResistanceType.Physical, "PhysicalResistMagicResist", physiMod),
                            new ResistanceMod(ResistanceType.Fire, "FireResistMagicResist", otherMod),
                            new ResistanceMod(ResistanceType.Cold, "ColdResistMagicResist", otherMod),
                            new ResistanceMod(ResistanceType.Poison, "PoisonResistMagicResist", otherMod),
                            new ResistanceMod(ResistanceType.Energy, "EnergyResistMagicResist", otherMod)
                        };

                        _table[targ] = mods;

                        for (var i = 0; i < mods.Length; ++i)
                        {
                            targ.AddResistanceMod(mods[i]);
                        }

                        var buffFormat = $"{physiMod}\t+{otherMod}\t+{otherMod}\t+{otherMod}\t+{otherMod}";

                        BuffInfo.AddBuff(targ, new BuffInfo(BuffIcon.MagicReflection, 1075817, buffFormat, true));
                    }
                }

                FinishSequence();
            }
            else
            {
                if (Caster.MagicDamageAbsorb > 0)
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
                        var value = (int)(Caster.Skills.Magery.Value + Caster.Skills.Inscribe.Value);
                        value = (int)(8 + value / 200.0 * 7.0); // absorb from 8 to 15 "circles"

                        Caster.MagicDamageAbsorb = value;

                        Caster.FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);
                        Caster.PlaySound(0x1E9);
                    }
                    else
                    {
                        Caster.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
                    }
                }

                FinishSequence();
            }
        }

        public static void EndReflect(Mobile m)
        {
            if (!_table.Remove(m, out var mods))
            {
                return;
            }

            for (var i = 0; i < mods?.Length; ++i)
            {
                m.RemoveResistanceMod(mods[i]);
            }

            BuffInfo.RemoveBuff(m, BuffIcon.MagicReflection);
        }
    }
}
