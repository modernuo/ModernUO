using System;
using System.Collections.Generic;

namespace Server.Items
{
    /// <summary>
    ///     Raises your defenses for a short time. Requires Bushido or Ninjitsu skill.
    /// </summary>
    public class Block : WeaponAbility
    {
        private static readonly Dictionary<Mobile, InternalTimer> _table = new();

        public override int BaseMana => 30;
        public override bool RequiresSecondarySkill(Mobile from) => true;

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            attacker.SendLocalizedMessage(1063345); // You block an attack!
            defender.SendLocalizedMessage(1063346); // Your attack was blocked!

            attacker.FixedParticles(0x37C4, 1, 16, 0x251D, 0x39D, 0x3, EffectLayer.RightHand);

            var bonus = (int)(10.0 * ((Math.Max(
                attacker.Skills.Bushido.Value,
                attacker.Skills.Ninjitsu.Value
            ) - 50.0) / 70.0 + 5));

            BeginBlock(attacker, bonus);
        }

        public static bool GetBonus(Mobile targ, ref int bonus)
        {
            if (!_table.TryGetValue(targ, out var info))
            {
                return false;
            }

            bonus = info.m_Bonus;
            return true;
        }

        public static void BeginBlock(Mobile m, int bonus)
        {
            EndBlock(m);
            _table[m] = new InternalTimer(m, bonus);
        }

        public static void EndBlock(Mobile m)
        {
            if (_table.Remove(m, out var timer))
            {
                timer?.Stop();
            }
        }

        private class InternalTimer : Timer
        {
            private readonly Mobile m_Mobile;
            public readonly int m_Bonus;

            public InternalTimer(Mobile m, int bonus) : base(TimeSpan.FromSeconds(6.0))
            {
                m_Bonus = bonus;
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                EndBlock(m_Mobile);
            }
        }
    }
}
