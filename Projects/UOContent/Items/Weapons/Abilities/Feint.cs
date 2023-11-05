using System;
using System.Collections.Generic;

namespace Server.Items
{
    /// <summary>
    ///     Gain a defensive advantage over your primary opponent for a short time.
    /// </summary>
    public class Feint : WeaponAbility
    {
        public static Dictionary<Mobile, FeintTimer> Registry { get; } = new();

        public override int BaseMana => 30;
        public override bool RequiresSecondarySkill(Mobile from) => true;

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            if (Registry.TryGetValue(defender, out var timer))
            {
                timer.Stop();
                Registry.Remove(defender);
            }

            ClearCurrentAbility(attacker);

            attacker.SendLocalizedMessage(1063360); // You baffle your target with a feint!
            defender.SendLocalizedMessage(1063361); // You were deceived by an attacker's feint!

            attacker.FixedParticles(0x3728, 1, 13, 0x7F3, 0x962, 0, EffectLayer.Waist);

            timer = new FeintTimer(
                defender,
                (int)(20.0 + 3.0 *
                    (Math.Max(attacker.Skills.Ninjitsu.Value, attacker.Skills.Bushido.Value) - 50.0) / 7.0)
            ); // 20-50 % decrease

            timer.Start();
            Registry.Add(defender, timer);
        }

        public class FeintTimer : Timer
        {
            private readonly Mobile m_Defender;

            public FeintTimer(Mobile defender, int swingSpeedReduction)
                : base(TimeSpan.FromSeconds(6.0))
            {
                m_Defender = defender;
                SwingSpeedReduction = swingSpeedReduction;
            }

            public int SwingSpeedReduction { get; }

            protected override void OnTick()
            {
                Registry.Remove(m_Defender);
            }
        }
    }
}
