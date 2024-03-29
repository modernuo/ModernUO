using Server.Mobiles;
using System;
using Server.Spells.Ninjitsu;

namespace Server.Items;

// The projectile will be infused with energy by the attacker causing it to do more damage and stun or dismount the target.
// The player infuses their throwing projectile with mystical power.
// The infused projectile will dismount the target if possible; otherwise it will temporarily stun the target.
// The target will be hit with chaos damage regardless of whether they were dismounted or paralyzed.
public class InfusedThrow : WeaponAbility
{
    public override int BaseMana => 25;

    public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
    {
        if (!CheckMana(attacker, true))
        {
            return;
        }

        ClearCurrentAbility(attacker);

        attacker.SendLocalizedMessage(1149563); //The infused projectile strikes a target!
        defender.SendLocalizedMessage(1149564); //You are struck by the infused projectile and take damage!

        AOS.Damage(defender, attacker, 15, false, 0, 0, 0, 0, 0, 100);

        IMount mount = defender.Mount;

        if ((defender.Mounted || defender.Flying || AnimalForm.UnderTransformation(defender)) &&
            !attacker.Mounted && !attacker.Flying && defender is not ChaosDragoon && defender is not ChaosDragoonElite)
        {
            defender.PlaySound(0x140);
            defender.FixedParticles(0x3728, 10, 15, 9955, EffectLayer.Waist);

            Items.Dismount.DoDismount(attacker, defender, TimeSpan.FromSeconds(10));
        }
        else
        {
            defender.FixedEffect(0x376A, 9, 32);
            defender.PlaySound(0x204);
            defender.Paralyze(TimeSpan.FromSeconds(defender.Player ? 3.0 : 6.0));
        }
    }
}
