using System;
using System.Collections.Generic;

namespace Server.Items
{
    /// <summary>
    ///     Raises your physical resistance for a short time while lowering your ability to inflict damage. Requires Bushido or
    ///     Ninjitsu skill.
    /// </summary>
    public class DefenseMastery : WeaponAbility
    {
        private static readonly Dictionary<Mobile, DefenseMasteryInfo> _table = new();

        public override int BaseMana => 30;
        public override bool RequiresSecondarySkill(Mobile from) => true;

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            attacker.SendLocalizedMessage(1063353); // You perform a masterful defense!

            attacker.FixedParticles(0x375A, 1, 17, 0x7F2, 0x3E8, 0x3, EffectLayer.Waist);

            var modifier =
                (int)(30.0 *
                      ((Math.Max(attacker.Skills.Bushido.Value, attacker.Skills.Ninjitsu.Value) - 50.0) / 70.0));

            if (_table.TryGetValue(attacker, out var info))
            {
                EndDefense(info);
            }

            var mod = new ResistanceMod(ResistanceType.Physical, "PhysicalResistDefenseMastery", 50 + modifier);
            attacker.AddResistanceMod(mod);

            info = new DefenseMasteryInfo(attacker, 80 - modifier, mod);
            Timer.StartTimer(TimeSpan.FromSeconds(3.0), () => EndDefense(info), out info._timerToken);

            _table[attacker] = info;

            attacker.Delta(MobileDelta.WeaponDamage);
        }

        public static bool GetMalus(Mobile targ, ref int damageMalus)
        {
            if (!_table.TryGetValue(targ, out var info))
            {
                return false;
            }

            damageMalus = info.m_DamageMalus;
            return true;
        }

        private static void EndDefense(DefenseMasteryInfo info)
        {
            if (info.m_Mod != null)
            {
                info.m_From.RemoveResistanceMod(info.m_Mod);
            }

            info._timerToken.Cancel();

            // No message is sent to the player.

            _table.Remove(info.m_From);

            info.m_From.Delta(MobileDelta.WeaponDamage);
        }

        private class DefenseMasteryInfo
        {
            public readonly int m_DamageMalus;
            public readonly Mobile m_From;
            public readonly ResistanceMod m_Mod;
            public TimerExecutionToken _timerToken;

            public DefenseMasteryInfo(Mobile from, int damageMalus, ResistanceMod mod)
            {
                m_From = from;
                m_DamageMalus = damageMalus;
                m_Mod = mod;
            }
        }
    }
}
