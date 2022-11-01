using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Spells.Ninjitsu
{
    public class KiAttack : NinjaMove
    {
        private static readonly Dictionary<Mobile, KiAttackTimer> _table = new();

        public override int BaseMana => 25;
        public override double RequiredSkill => 80.0;

        // Your Ki Attack must be complete within 2 seconds for the damage bonus!
        public override TextDefinition AbilityMessage { get; } = 1063099;

        public override void OnUse(Mobile from)
        {
            if (!Validate(from))
            {
                return;
            }

            var t = new KiAttackTimer(from);
            _table[from] = t;
            t.Start();
        }

        public override bool Validate(Mobile from)
        {
            if (from.Hidden && from.AllowedStealthSteps > 0)
            {
                from.SendLocalizedMessage(1063127); // You cannot use this ability while in stealth mode.
                return false;
            }

            if (Core.ML && from.Weapon is BaseRanged)
            {
                from.SendLocalizedMessage(1075858); // You can only use this with melee attacks.
                return false;
            }

            return base.Validate(from);
        }

        public override double GetDamageScalar(Mobile attacker, Mobile defender)
        {
            if (attacker.Hidden)
            {
                return 1.0;
            }

            /*
             * Pub40 changed pvp damage max to 55%
             */
            return 1.0 + GetBonus(attacker) / (Core.ML && attacker.Player && defender.Player ? 40 : 10);
        }

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentMove(attacker);

            if (GetBonus(attacker) == 0.0)
            {
                attacker.SendLocalizedMessage(1063101); // You were too close to your target to cause any additional damage.
            }
            else
            {
                attacker.FixedParticles(0x37BE, 1, 5, 0x26BD, 0x0, 0x1, EffectLayer.Waist);
                attacker.PlaySound(0x510);

                // Your quick flight to your target causes extra damage as you strike!
                attacker.SendLocalizedMessage(1063100);
                defender.FixedParticles(0x37BE, 1, 5, 0x26BD, 0, 0x1, EffectLayer.Waist);

                CheckGain(attacker);
            }
        }

        public override void OnClearMove(Mobile from)
        {
            if (_table.Remove(from, out var t))
            {
                t.Stop();
            }
        }

        public static double GetBonus(Mobile from)
        {
            if (!_table.TryGetValue(from, out var t))
            {
                return 0;
            }

            var xDelta = t._location.X - from.X;
            var yDelta = t._location.Y - from.Y;

            return Math.Min(Math.Sqrt(xDelta * xDelta + yDelta * yDelta), 20.0);
        }

        private class KiAttackTimer : Timer
        {
            public Mobile _mobile;
            public Point3D _location;

            public KiAttackTimer(Mobile m) : base(TimeSpan.FromSeconds(2.0))
            {
                _mobile = m;
                _location = m.Location;
            }

            protected override void OnTick()
            {
                ClearCurrentMove(_mobile);
                _mobile.SendLocalizedMessage(1063102); // You failed to complete your Ki Attack in time.

                _table.Remove(_mobile);
            }
        }
    }
}
