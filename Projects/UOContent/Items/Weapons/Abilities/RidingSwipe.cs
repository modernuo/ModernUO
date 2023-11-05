using System;
using Server.Mobiles;

namespace Server.Items
{
    /// <summary>
    ///     If you are on foot, dismounts your opponent and damage the ethereal's rider or the
    ///     living mount(which must be healed before ridden again). If you are mounted, damages
    ///     and stuns the mounted opponent.
    /// </summary>
    public class RidingSwipe : WeaponAbility
    {
        public override int BaseMana => 30;

        public override bool RequiresSE => true;
        public override bool RequiresSecondarySkill(Mobile from) => true;
        public override SkillName GetSecondarySkillName(Mobile from) => SkillName.Bushido;

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (!defender.Mounted)
            {
                attacker.SendLocalizedMessage(1060848); // This attack only works on mounted targets
                ClearCurrentAbility(attacker);
                return;
            }

            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            if (!attacker.Mounted)
            {
                var mount = defender.Mount as Mobile;
                BaseMount.Dismount(defender);

                if (mount != null) // Ethy mounts don't take damage
                {
                    var amount = 10 + (int)(10.0 * (attacker.Skills.Bushido.Value - 50.0) / 70.0 + 5);

                    AOS.Damage(
                        mount,
                        null,
                        amount,
                        100,
                        0,
                        0,
                        0,
                        0
                    ); // The mount just takes damage, there's no flagging as if it was attacking the mount directly

                    // TODO: Mount prevention until mount healed
                }
            }
            else
            {
                var amount = 10 + (int)(10.0 * (attacker.Skills.Bushido.Value - 50.0) / 70.0 + 5);

                AOS.Damage(defender, attacker, amount, 100, 0, 0, 0, 0);

                if (Items.ParalyzingBlow.IsImmune(defender)) // Does it still do damage?
                {
                    attacker.SendLocalizedMessage(1070804); // Your target resists paralysis.
                    defender.SendLocalizedMessage(1070813); // You resist paralysis.
                }
                else
                {
                    defender.Paralyze(TimeSpan.FromSeconds(3.0));
                    Items.ParalyzingBlow.BeginImmunity(defender, Items.ParalyzingBlow.FreezeDelayDuration);
                }
            }
        }
    }
}
