using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class ForceOfNature : WeaponAbility
    {
        public override int BaseMana => 35;

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            attacker.SendLocalizedMessage(1074374); // You attack your enemy with the force of nature!
            defender.SendLocalizedMessage(1074375); // You are assaulted with great force!

            defender.PlaySound(0x22F);
            defender.FixedParticles(0x36CB, 1, 9, 9911, 67, 5, EffectLayer.Head);
            defender.FixedParticles(0x374A, 1, 17, 9502, 1108, 4, (EffectLayer)255);

            Remove(attacker);

            ForceOfNatureTimer t = new ForceOfNatureTimer(attacker, defender);
            t.Start();

            _table[attacker] = t;
        }

        private static readonly Dictionary<Mobile, ForceOfNatureTimer> _table = new Dictionary<Mobile, ForceOfNatureTimer>();

        public static void Remove(Mobile m)
        {
            if (_table.Remove(m, out var timer))
            {
                timer.Stop();
            }
        }

        public static void OnHit(Mobile from, Mobile target)
        {
            if (_table.TryGetValue(from,out var t))
            {
                t.Hits++;
                t.LastHit = Core.Now;

                if (t.Hits % 12 == 0)
                {
                    int duration = target.Skills[SkillName.MagicResist].Value >= 90.0 ? 1 : 2;
                    target.Paralyze(TimeSpan.FromSeconds(duration));

                    target.FixedEffect(0x376A, 9, 32);
                    target.PlaySound(0x204);

                    t.Hits = 0;

                    from.SendLocalizedMessage(1004013); // You successfully stun your opponent!
                    target.SendLocalizedMessage(1004014); // You have been stunned!
                }
            }
        }

        public static double GetDamageScalar(Mobile from, Mobile target)
        {
            if (_table.TryGetValue(from, out var t) && t.Target == target)
            {
                var bonus = Math.Min(100, Math.Max(50, from.Str - 50));
                return (100.0 + bonus) / 100.0;
            }

            return 1.0;
        }

        private class ForceOfNatureTimer : Timer
        {
            public Mobile Target { get; }
            public Mobile From { get; }
            public int Hits { get; set; }
            public DateTime LastHit { get; set; }

            public ForceOfNatureTimer(Mobile from, Mobile target)
                : base(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
            {
                Target = target;
                From = from;
                Hits = 1;
                LastHit = Core.Now;
            }

            protected override void OnTick()
            {
                if (!From.Alive || !Target.Alive || Target.Map != From.Map || Target.GetDistanceToSqrt(From.Location) > 10 || LastHit + TimeSpan.FromSeconds(20) < Core.Now || Index > 36)
                {
                    Remove(From);
                    return;
                }

                if (Index == 1)
                {
                    int damage = Utility.RandomMinMax(15, 35);

                    AOS.Damage(Target, From, damage, false, 0, 0, 0, 0, 0, 0, 100);
                }
            }
        }
    }
}
