using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class Bladeweave : WeaponAbility
    {
        private class BladeWeaveRedirect
        {
            public readonly WeaponAbility NewAbility;
            public readonly int ClilocEntry;

            public BladeWeaveRedirect(WeaponAbility ability, int cliloc)
            {
                NewAbility = ability;
                ClilocEntry = cliloc;
            }
        }

        private static readonly Dictionary<Mobile, BladeWeaveRedirect> _newAttack = new();

        public static bool BladeWeaving(Mobile attacker, out WeaponAbility a)
        {
            if (_newAttack.TryGetValue(attacker, out var bwr))
            {
                a = bwr.NewAbility;
                return true;
            }

            a = null;
            return false;
        }

        public override int BaseMana => 30;

        public override bool OnBeforeSwing(Mobile attacker, Mobile defender)
        {
            if (!Validate(attacker) || !CheckMana(attacker, false))
            {
                return false;
            }

            // Bladeweave is only from 90 fighting and tactics skill, so you need only to check bushido/ninjitsu value over 50 to perform block and feint.
            var requiredBushido = Math.Max(attacker.Skills.Bushido.Value, attacker.Skills.Ninjitsu.Value);

            var ran = Utility.Random(requiredBushido >= 50 ? 9 : 7);

            var newAttack = _newAttack[attacker] = GetBladeWeaveRedirect(ran);

            return newAttack.NewAbility.OnBeforeSwing(attacker, defender);
        }

        private static BladeWeaveRedirect GetBladeWeaveRedirect(int number) =>
            number switch
            {
                0 => new BladeWeaveRedirect(ArmorIgnore, 1028838),
                1 => new BladeWeaveRedirect(BleedAttack, 1028839),
                2 => new BladeWeaveRedirect(ConcussionBlow, 1028840),
                3 => new BladeWeaveRedirect(CrushingBlow, 1028841),
                4 => new BladeWeaveRedirect(DoubleStrike, 1028844),
                5 => new BladeWeaveRedirect(MortalStrike, 1028846),
                6 => new BladeWeaveRedirect(ParalyzingBlow, 1028848),
                7 => new BladeWeaveRedirect(Block, 1028853),
                _ => new BladeWeaveRedirect(Feint, 1028857) //8
            };

        public override bool OnBeforeDamage(Mobile attacker, Mobile defender) =>
            _newAttack.TryGetValue(attacker, out var bwr)
                ? bwr.NewAbility.OnBeforeDamage(attacker, defender)
                : base.OnBeforeDamage(attacker, defender);

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (CheckMana(attacker, false))
            {
                if (_newAttack.TryGetValue(attacker, out var bwr))
                {
                    attacker.SendLocalizedMessage(1072841, $"#{bwr.ClilocEntry}");
                    bwr.NewAbility.OnHit(attacker, defender, damage, worldLocation);
                }
                else
                {
                    base.OnHit(attacker, defender, damage, worldLocation);
                }

                _newAttack.Remove(attacker);
                ClearCurrentAbility(attacker);
            }
        }

        public override void OnMiss(Mobile attacker, Mobile defender)
        {
            if (_newAttack.TryGetValue(attacker, out var bwr))
            {
                bwr.NewAbility.OnMiss(attacker, defender);
            }
            else
            {
                base.OnMiss(attacker, defender);
            }

            _newAttack.Remove(attacker);
        }
    }
}
