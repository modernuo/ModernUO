using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Fourth
{
    public class CurseSpell : MagerySpell, ITargetingSpell<Mobile>
    {
        private static readonly SpellInfo _info = new(
            "Curse",
            "Des Sanct",
            227,
            9031,
            Reagent.Nightshade,
            Reagent.Garlic,
            Reagent.SulfurousAsh
        );

        private static readonly Dictionary<Mobile, Timer> _table = new();

        public CurseSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fourth;

        public static bool DoCurse(Mobile caster, Mobile m)
        {
            var duration = SpellHelper.GetDuration(caster, m);

            if (duration == TimeSpan.Zero)
            {
                return false;
            }

            // Allow curse duration to refresh if the new curse is longer
            if (_table.TryGetValue(m, out var existingTimer) && existingTimer.Next - Core.Now > duration && Core.AOS)
            {
                return false;
            }

            var newStrOffset = SpellHelper.GetOffset(caster, m, StatType.Str, true);
            var newDexOffset = SpellHelper.GetOffset(caster, m, StatType.Dex, true);
            var newIntOffset = SpellHelper.GetOffset(caster, m, StatType.Int, true);

            if (newStrOffset == 0 && newDexOffset == 0 && newIntOffset == 0)
            {
                return false;
            }

            var oldStrOffset = SpellHelper.GetCurse(caster, m, StatType.Str);
            var oldDexOffset = SpellHelper.GetCurse(caster, m, StatType.Dex);
            var oldIntOffset = SpellHelper.GetCurse(caster, m, StatType.Int);

            if (oldStrOffset > newStrOffset && oldDexOffset > newDexOffset && oldIntOffset > newIntOffset)
            {
                return false;
            }

            var strCurse = SpellHelper.AddStatCurse(caster, m, StatType.Str, newStrOffset, duration);
            var dexCurse = SpellHelper.AddStatCurse(caster, m, StatType.Dex, newDexOffset, duration);
            var intCurse = SpellHelper.AddStatCurse(caster, m, StatType.Int, newIntOffset, duration, true);

            if (!strCurse && !dexCurse && !intCurse)
            {
                return false;
            }

            existingTimer?.Stop();
            m.UpdateResistances();
            _table[m] = Timer.DelayCall(duration, mob => RemoveEffect(mob), m);

            m.Spell?.OnCasterHurt();

            m.Paralyzed = false;

            m.FixedParticles(0x374A, 10, 15, 5028, EffectLayer.Waist);
            m.PlaySound(0x1E1);

            var percentage = (int)(SpellHelper.GetOffsetScalar(caster, m, true) * 100);
            var args = $"{percentage}\t{percentage}\t{percentage}\t{10}\t{10}\t{10}\t{10}";

            (m as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.Curse, 1075835, 1075836, duration, args));
            return true;
        }

        public void Target(Mobile m)
        {
            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)Circle, Caster, ref m);

                if (DoCurse(Caster, m))
                {
                    HarmfulSpell(m);
                }
                else
                {
                    DoHurtFizzle();
                }
            }
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
        }

        public static bool RemoveEffect(Mobile m)
        {
            if (_table.Remove(m, out var timer))
            {
                timer.Stop();
                m.UpdateResistances();
                return true;
            }

            return false;
        }

        public static bool UnderEffect(Mobile m) => _table.ContainsKey(m);
    }
}
