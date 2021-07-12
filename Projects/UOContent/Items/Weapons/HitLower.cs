using System;
using System.Collections.Generic;

namespace Server.Items
{
    public static class HitLower
    {
        public static readonly TimeSpan AttackEffectDuration = TimeSpan.FromSeconds(10.0);
        public static readonly TimeSpan DefenseEffectDuration = TimeSpan.FromSeconds(8.0);

        private static readonly HashSet<Mobile> m_AttackTable = new();
        private static readonly HashSet<Mobile> m_DefenseTable = new();

        public static bool IsUnderAttackEffect(Mobile m) => m_AttackTable.Contains(m);

        public static bool ApplyAttack(Mobile m)
        {
            if (IsUnderAttackEffect(m))
            {
                return false;
            }

            m_AttackTable.Add(m);
            var timer = new AttackTimer(m);
            timer.Start();
            m.SendLocalizedMessage(1062319); // Your attack chance has been reduced!
            return true;
        }

        private static void RemoveAttack(Mobile m)
        {
            m_AttackTable.Remove(m);
            m.SendLocalizedMessage(1062320); // Your attack chance has returned to normal.
        }

        public static bool IsUnderDefenseEffect(Mobile m) => m_DefenseTable.Contains(m);

        public static bool ApplyDefense(Mobile m)
        {
            if (IsUnderDefenseEffect(m))
            {
                return false;
            }

            m_DefenseTable.Add(m);
            var timer = new DefenseTimer(m);
            timer.Start();
            m.SendLocalizedMessage(1062318); // Your defense chance has been reduced!
            return true;
        }

        private static void RemoveDefense(Mobile m)
        {
            m_DefenseTable.Remove(m);
            m.SendLocalizedMessage(1062321); // Your defense chance has returned to normal.
        }

        private class AttackTimer : Timer
        {
            private readonly Mobile m_Player;

            public AttackTimer(Mobile player) : base(AttackEffectDuration)
            {
                m_Player = player;
            }

            protected override void OnTick()
            {
                RemoveAttack(m_Player);
            }
        }

        private class DefenseTimer : Timer
        {
            private readonly Mobile m_Player;

            public DefenseTimer(Mobile player) : base(DefenseEffectDuration)
            {
                m_Player = player;
            }

            protected override void OnTick()
            {
                RemoveDefense(m_Player);
            }
        }
    }
}
