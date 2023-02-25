using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;

namespace Server.Items
{
    /// <summary>
    ///     Make your opponent bleed profusely with this wicked use of your weapon.
    ///     When successful, the target will bleed for several seconds, taking damage as time passes for up to ten seconds.
    ///     The rate of damage slows down as time passes, and the blood loss can be completely staunched with the use of bandages.
    /// </summary>
    public class BleedAttack : WeaponAbility
    {
        private static readonly Dictionary<Mobile, Timer> _table = new();

        public override int BaseMana => 30;

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            // Necromancers under Lich or Wraith Form are immune to Bleed Attacks.
            var context = TransformationSpellHelper.GetContext(defender);

            if (context != null && (context.Type == typeof(LichFormSpell) || context.Type == typeof(WraithFormSpell)) ||
                defender is BaseCreature creature && creature.BleedImmune)
            {
                attacker.SendLocalizedMessage(1062052); // Your target is not affected by the bleed attack!
                return;
            }

            attacker.SendLocalizedMessage(1060159); // Your target is bleeding!
            defender.SendLocalizedMessage(1060160); // You are bleeding!

            if (defender is PlayerMobile)
            {
                defender.LocalOverheadMessage(MessageType.Regular, 0x21, 1060757); // You are bleeding profusely
                defender.NonlocalOverheadMessage(
                    MessageType.Regular,
                    0x21,
                    1060758, // ~1_NAME~ is bleeding profusely
                    defender.Name
                );
            }

            defender.PlaySound(0x133);
            defender.FixedParticles(0x377A, 244, 25, 9950, 31, 0, EffectLayer.Waist);

            BeginBleed(defender, attacker);
        }

        public static bool IsBleeding(Mobile m) => _table.ContainsKey(m);

        public static void BeginBleed(Mobile m, Mobile from)
        {
            _table.TryGetValue(m, out var timer);
            timer?.Stop();

            _table[m] = timer = new InternalTimer(from, m);
            timer.Start();
        }

        public static void DoBleed(Mobile m, Mobile from, int level)
        {
            if (m.Alive)
            {
                var damage = Utility.RandomMinMax(level, level * 2);

                if (!m.Player)
                {
                    damage *= 2;
                }

                m.PlaySound(0x133);
                m.Damage(damage, from);

                var blood = new Blood { ItemID = Utility.Random(0x122A, 5) };
                blood.MoveToWorld(m.Location, m.Map);
            }
            else
            {
                EndBleed(m, false);
            }
        }

        public static void EndBleed(Mobile m, bool message)
        {
            if (!_table.Remove(m, out var t))
            {
                return;
            }

            t.Stop();

            if (message)
            {
                m.SendLocalizedMessage(1060167); // The bleeding wounds have healed, you are no longer bleeding!
            }
        }

        private class InternalTimer : Timer
        {
            private readonly Mobile m_From;
            private readonly Mobile m_Mobile;

            public InternalTimer(Mobile from, Mobile m) : base(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.0), 5)
            {
                m_From = from;
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                DoBleed(m_Mobile, m_From, 5 - Index);

                if (Index == 4)
                {
                    EndBleed(m_Mobile, true);
                }
            }
        }
    }
}
