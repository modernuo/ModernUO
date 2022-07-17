using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Items;
using Server.SkillHandlers;

namespace Server.Spells.Ninjitsu
{
    public class DeathStrike : NinjaMove
    {
        private static readonly Dictionary<Mobile, DeathStrikeTimer> _table = new();

        public override int BaseMana => 30;
        public override double RequiredSkill => 85.0;

        public override TextDefinition AbilityMessage =>
            new(1063091); // You prepare to hit your opponent with a Death Strike.

        public override double GetDamageScalar(Mobile attacker, Mobile defender) => 0.5;

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentMove(attacker);

            var ninjitsu = attacker.Skills.Ninjitsu.Value;

            // TODO: should be defined onHit method, what if the player hit and remove the weapon before process? ;)
            var isRanged = attacker.Weapon is BaseRanged;

            var chance = ninjitsu switch
            {
                // This formula is an approximation from OSI data.  TODO: find correct formula
                < 100 => 30 + (ninjitsu - 85) * 2.2,
                _     => 63 + (ninjitsu - 100) * 1.1
            };

            if (chance / 100 < Utility.RandomDouble())
            {
                attacker.SendLocalizedMessage(1070779); // You missed your opponent with a Death Strike.
                return;
            }

            var damageBonus = 0;

            if (_table.Remove(defender, out var timer))
            {
                defender.SendLocalizedMessage(1063092); // Your opponent lands another Death Strike!

                if (timer.Steps > 0)
                {
                    damageBonus = attacker.Skills.Ninjitsu.Fixed / 150;
                }

                timer.Stop();
            }
            else
            {
                defender.SendLocalizedMessage(1063093); // You have been hit by a Death Strike!  Move with caution!
            }

            attacker.SendLocalizedMessage(1063094); // You inflict a Death Strike upon your opponent!

            defender.FixedParticles(0x374A, 1, 17, 0x26BC, EffectLayer.Waist);
            attacker.PlaySound(attacker.Female ? 0x50D : 0x50E);

            var t = new DeathStrikeTimer(defender, attacker, damageBonus, isRanged);

            _table[defender] = t;

            t.Start();

            CheckGain(attacker);
        }

        public static void AddStep(Mobile m)
        {
            if (_table.TryGetValue(m, out var timer) && ++timer.Steps >= 5)
            {
                timer.ProcessDeathStrike();
            }
        }

        private class DeathStrikeTimer : Timer
        {
            private Mobile _attacker;
            private int _damageBonus;
            private bool _isRanged;
            private Mobile _target;
            public int Steps { get; set; }

            internal DeathStrikeTimer(Mobile target, Mobile attacker, int damageBonus, bool isRanged)
                : base(TimeSpan.FromSeconds(5.0))
            {
                _target = target;
                _attacker = attacker;
                _damageBonus = damageBonus;
                _isRanged = isRanged;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void OnTick()
            {
                ProcessDeathStrike();
            }

            public void ProcessDeathStrike()
            {
                int damage;

                var ninjitsu = _attacker.Skills.Ninjitsu.Value;
                var stalkingBonus = Tracking.GetStalkingBonus(_attacker, _target);

                if (Core.ML)
                {
                    var scalar = Math.Min(1, (_attacker.Skills.Hiding.Value + _attacker.Skills.Stealth.Value) / 220);

                    // New formula doesn't apply DamageBonus anymore, caps must be, directly, 60/30.
                    if (Steps >= 5)
                    {
                        damage = (int)Math.Floor(Math.Min(60, ninjitsu / 3 * (0.3 + 0.7 * scalar) + stalkingBonus));
                    }
                    else
                    {
                        damage = (int)Math.Floor(Math.Min(30, ninjitsu / 9 * (0.3 + 0.7 * scalar) + stalkingBonus));
                    }

                    if (_isRanged)
                    {
                        damage /= 2;
                    }

                    _target.Damage(damage, _attacker); // Damage is direct.
                }
                else
                {
                    var divisor = Steps >= 5 ? 30 : 80;
                    var baseDamage = ninjitsu / divisor * 10;

                    var maxDamage = Steps >= 5 ? 62 : 22;
                    damage = Math.Clamp((int)(baseDamage + stalkingBonus), 0, maxDamage) + _damageBonus;

                    // Damage is physical.
                    AOS.Damage(
                        _target,
                        _attacker,
                        damage,
                        true,
                        100,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        false,
                        false,
                        true
                    );
                }

                Stop();
            }
        }
    }
}
