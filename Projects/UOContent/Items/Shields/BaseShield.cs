using System;
using ModernUO.Serialization;

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

    public override int GetLuckBonus()
    {
        if (CraftResources.GetType(Resource) != CraftResourceType.Wood)
        {
            return base.GetLuckBonus();
        }
        else
        {
            CraftAttributeInfo attrInfo = GetResourceAttrs(Resource);

            if (attrInfo == null)
                return 0;

            return attrInfo.ShieldLuck;
        }
    }

    protected override void ApplyResourceResistances(CraftResource oldResource)
    {
        if (CraftResources.GetType(Resource) != CraftResourceType.Wood)
        {
            base.ApplyResourceResistances(oldResource);
        }
        else
        {
            CraftAttributeInfo info;

            if (oldResource > CraftResource.None)
            {
                info = GetResourceAttrs(oldResource);
                // Remove old bonus

                PhysicalBonus = Math.Max(0, PhysicalBonus - info.ShieldPhysicalResist);
                FireBonus = Math.Max(0, FireBonus - info.ShieldFireResist);
                ColdBonus = Math.Max(0, ColdBonus - info.ShieldColdResist);
                PoisonBonus = Math.Max(0, PoisonBonus - info.ShieldPoisonResist);
                EnergyBonus = Math.Max(0, EnergyBonus - info.ShieldEnergyResist);

                //PhysNonImbuing = Math.Max(0, PhysNonImbuing - info.ShieldPhysicalResist);
                //FireNonImbuing = Math.Max(0, FireNonImbuing - info.ShieldFireResist);
                //ColdNonImbuing = Math.Max(0, ColdNonImbuing - info.ShieldColdResist);
                //PoisonNonImbuing = Math.Max(0, PoisonNonImbuing - info.ShieldPoisonResist);
                //EnergyNonImbuing = Math.Max(0, EnergyNonImbuing - info.ShieldEnergyResist);
            }

            info = GetResourceAttrs(Resource);

            // add new bonus
            PhysicalBonus += info.ShieldPhysicalResist;
            FireBonus += info.ShieldFireResist;
            ColdBonus += info.ShieldColdResist;
            PoisonBonus += info.ShieldPoisonResist;
            EnergyBonus += info.ShieldEnergyResist;

            //PhysNonImbuing += info.ShieldPhysicalResist;
            //FireNonImbuing += info.ShieldFireResist;
            //ColdNonImbuing += info.ShieldColdResist;
            //PoisonNonImbuing += info.ShieldPoisonResist;
            //EnergyNonImbuing += info.ShieldEnergyResist;
        }
    }

    public override void DistributeMaterialBonus(CraftAttributeInfo attrInfo)
    {
        if (CraftResources.GetType(Resource) != CraftResourceType.Wood)
        {
            base.DistributeMaterialBonus(attrInfo);
        }
        else
        {
            if (Resource != CraftResource.Heartwood)
            {
                Attributes.SpellChanneling += attrInfo.ShieldSpellChanneling;
                ArmorAttributes.LowerStatReq += attrInfo.ShieldLowerRequirements;
                Attributes.RegenHits += attrInfo.ShieldRegenHits;
            }
            else
            {
                switch (Utility.Random(7))
                {
                    case 0: Attributes.BonusDex += attrInfo.ShieldBonusDex; break;
                    case 1: Attributes.BonusStr += attrInfo.ShieldBonusStr; break;
                    case 2: PhysicalBonus += attrInfo.ShieldPhysicalRandom; break;
                    case 3: Attributes.ReflectPhysical += attrInfo.ShieldReflectPhys; break;
                    case 4: ArmorAttributes.SelfRepair += attrInfo.ShieldSelfRepair; break;
                    case 5: ColdBonus += attrInfo.ShieldColdRandom; break;
                    case 6: Attributes.SpellChanneling += attrInfo.ShieldSpellChanneling; break;
                }
            }
        }
    }
}
