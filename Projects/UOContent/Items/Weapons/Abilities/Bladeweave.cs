using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class Bladeweave : WeaponAbility
    {
        private class BladeWeaveRedirect
        {
            public WeaponAbility NewAbility;
            public int ClilocEntry;

            public BladeWeaveRedirect(WeaponAbility ability, int cliloc)
            {
                NewAbility = ability;
                ClilocEntry = cliloc;
            }
        }

        private static readonly Dictionary<Mobile, BladeWeaveRedirect> m_NewAttack = new Dictionary<Mobile, BladeWeaveRedirect>();

        public static bool BladeWeaving(Mobile attacker, out WeaponAbility a)
        {
            BladeWeaveRedirect bwr;
            bool success = m_NewAttack.TryGetValue(attacker, out bwr);
            if (success)
                a = bwr.NewAbility;
            else
                a = null;

            return success;
        }

        public override int BaseMana => 30;

        public override bool OnBeforeSwing(Mobile attacker, Mobile defender)
        {
            if (!Validate(attacker) || !CheckMana(attacker, false))
                return false;

            int ran = -1;

            var requiredSecondarySkill = Math.Max(attacker.Skills.Bushido.Value, attacker.Skills.Ninjitsu.Value);
            // pokud ninjitsu nebo bushido je ober getrequired skill tak je to ok
            var canfeint = requiredSecondarySkill >= Feint.GetRequiredSecondarySkill(attacker);
            var canblock = requiredSecondarySkill >= Block.GetRequiredSecondarySkill(attacker);

            if (canfeint && canblock)
            {
                ran = Utility.Random(9);
            }
            else if (canblock)
            {
                ran = Utility.Random(8);
            }
            else
            {
                ran = Utility.RandomList(0, 1, 2, 3, 4, 5, 6, 8);
            }

            return GetBladeWeaveRedirect(ran).NewAbility.OnBeforeSwing(attacker, defender);
        }

        private static BladeWeaveRedirect GetBladeWeaveRedirect(int number)
            =>
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
                //8 => new BladeWeaveRedirect(Feint, 1028857),
                _ => new BladeWeaveRedirect(Feint, 1028857)//8
            };

        public override bool OnBeforeDamage(Mobile attacker, Mobile defender)
        {
            BladeWeaveRedirect bwr;
            if (m_NewAttack.TryGetValue(attacker, out bwr))
                return bwr.NewAbility.OnBeforeDamage(attacker, defender);
            else
                return base.OnBeforeDamage(attacker, defender);
        }

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (CheckMana(attacker, false))
            {
                BladeWeaveRedirect bwr;
                if (m_NewAttack.TryGetValue(attacker, out bwr))
                {
                    attacker.SendLocalizedMessage(1072841, "#" + bwr.ClilocEntry.ToString());
                    bwr.NewAbility.OnHit(attacker, defender, damage);
                }
                else
                    base.OnHit(attacker, defender, damage);

                m_NewAttack.Remove(attacker);
                ClearCurrentAbility(attacker);
            }
        }

        public override void OnMiss(Mobile attacker, Mobile defender)
        {
            BladeWeaveRedirect bwr;
            if (m_NewAttack.TryGetValue(attacker, out bwr))
                bwr.NewAbility.OnMiss(attacker, defender);
            else
                base.OnMiss(attacker, defender);

            m_NewAttack.Remove(attacker);
        }
    }
}
