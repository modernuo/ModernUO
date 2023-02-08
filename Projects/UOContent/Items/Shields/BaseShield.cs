using System;
using ModernUO.Serialization;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BaseShield : BaseArmor
{
    public BaseShield(int itemID) : base(itemID)
    {
    }

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Plate;

    public override double ArmorRating
    {
        get
        {
            var m = Parent as Mobile;
            var ar = base.ArmorRating;

            if (m != null)
            {
                return m.Skills.Parry.Value * ar / 200.0 + 1.0;
            }

            return ar;
        }
    }

    public override int OnHit(BaseWeapon weapon, int damage)
    {
        if (Core.AOS)
        {
            if (ArmorAttributes.SelfRepair > Utility.Random(10))
            {
                HitPoints += 2;
            }
            else
            {
                var halfArmor = ArmorRating / 2.0;
                var absorbed = Math.Max(2, (int)(halfArmor + halfArmor * Utility.RandomDouble()));

                TryLowerDurability(weapon.Type == WeaponType.Bashing ? absorbed / 2 : Utility.Random(2));
            }

            return 0;
        }

        if (Parent is not Mobile owner)
        {
            return damage;
        }

        var ar = ArmorRating;
        var chance = (owner.Skills.Parry.Value - ar * 2.0) / 100.0;

        if (chance < 0.01)
        {
            chance = 0.01;
        }

        if (owner.CheckSkill(SkillName.Parry, chance))
        {
            damage -= Math.Min(damage, weapon.Skill == SkillName.Archery ? (int)ar : (int)(ar / 2.0));

            owner.FixedEffect(0x37B9, 10, 16);

            if (Utility.Random(100) < 25) // 25% chance to lower durability
            {
                TryLowerDurability(Utility.Random(2));
            }
        }

        return damage;
    }

    private void TryLowerDurability(int wear)
    {
        if (wear <= 0 || MaxHitPoints <= 0)
        {
            return;
        }

        if (HitPoints >= wear)
        {
            HitPoints -= wear;
            wear = 0;
        }
        else
        {
            wear -= HitPoints;
            HitPoints = 0;
        }

        if (wear <= 0)
        {
            return;
        }

        if (MaxHitPoints > wear)
        {
            MaxHitPoints -= wear;

            // Your equipment is severely damaged.
            (Parent as Mobile)?.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1061121);
        }
        else
        {
            Delete();
        }
    }
}
