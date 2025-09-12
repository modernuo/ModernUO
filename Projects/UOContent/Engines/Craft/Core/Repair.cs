using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Craft
{
    public static class Repair
    {
        public static void Do(Mobile from, CraftSystem craftSystem, BaseTool tool)
        {
            from.Target = new InternalTarget(craftSystem, tool);
            from.SendLocalizedMessage(1044276); // Target an item to repair.
        }

        public static void Do(Mobile from, CraftSystem craftSystem, RepairDeed deed)
        {
            from.Target = new InternalTarget(craftSystem, deed);
            from.SendLocalizedMessage(1044276); // Target an item to repair.
        }

        private class InternalTarget : Target
        {
            private readonly CraftSystem m_CraftSystem;
            private readonly RepairDeed m_Deed;
            private readonly BaseTool m_Tool;

            public InternalTarget(CraftSystem craftSystem, BaseTool tool) : base(2, false, TargetFlags.None)
            {
                m_CraftSystem = craftSystem;
                m_Tool = tool;
            }

            public InternalTarget(CraftSystem craftSystem, RepairDeed deed) : base(2, false, TargetFlags.None)
            {
                m_CraftSystem = craftSystem;
                m_Deed = deed;
            }

            private int GetWeakenChance(Mobile mob, SkillName skill, int curHits, int maxHits) => 40 + (maxHits - curHits) -
                (int)((m_Deed?.SkillLevel ?? mob.Skills[skill].Value) / 10);

            private bool CheckWeaken(Mobile mob, SkillName skill, int curHits, int maxHits) =>
                GetWeakenChance(mob, skill, curHits, maxHits) > Utility.Random(100);

            private int GetRepairDifficulty(int curHits, int maxHits) =>
                (maxHits - curHits) * 1250 / Math.Max(maxHits, 1) - 250;

            private bool CheckRepairDifficulty(Mobile mob, SkillName skill, int curHits, int maxHits)
            {
                var difficulty = GetRepairDifficulty(curHits, maxHits) * 0.1;

                if (m_Deed != null)
                {
                    var value = m_Deed.SkillLevel;
                    var minSkill = difficulty - 25.0;
                    var maxSkill = difficulty + 25;

                    if (value < minSkill)
                    {
                        return false; // Too difficult
                    }

                    if (value >= maxSkill)
                    {
                        return true; // No challenge
                    }

                    var chance = (value - minSkill) / (maxSkill - minSkill);

                    return chance >= Utility.RandomDouble();
                }

                return mob.CheckSkill(skill, difficulty - 25.0, difficulty + 25.0);
            }

            private bool CheckDeed(Mobile from)
            {
                if (m_Deed != null)
                {
                    return m_Deed.Check(from);
                }

                return true;
            }

            private bool IsSpecialClothing(BaseClothing clothing)
            {
                // Clothing repairable but not craftable

                if (m_CraftSystem is DefTailoring)
                {
                    return clothing is BearMask or DeerMask or TheMostKnowledgePerson or TheRobeOfBritanniaAri or EmbroideredOakLeafCloak;
                }

                return false;
            }

            private bool IsSpecialWeapon(BaseWeapon weapon)
            {
                // Weapons repairable but not craftable

                if (m_CraftSystem is DefTinkering)
                {
                    return weapon is Cleaver or Hatchet or Pickaxe or ButcherKnife or SkinningKnife;
                }

                if (m_CraftSystem is DefCarpentry)
                {
                    return weapon is Club or BlackStaff or MagicWand or WildStaff;
                }

                if (m_CraftSystem is DefBlacksmithy)
                {
                    return weapon is Pitchfork or RadiantScimitar or WarCleaver or ElvenSpellblade or AssassinSpike or Leafblade or RuneBlade or ElvenMachete or OrnateAxe or DiamondMace;
                }

                // TODO: Make these items craftable
                if (m_CraftSystem is DefBowFletching)
                {
                    return weapon is ElvenCompositeLongbow or MagicalShortbow;
                }

                return false;
            }

            private bool IsSpecialArmor(BaseArmor armor)
            {
                // Armor repairable but not craftable

                // TODO: Make these items craftable
                if (m_CraftSystem is DefTailoring)
                {
                    return armor is LeafTonlet or LeafArms or LeafChest or LeafGloves or LeafGorget or LeafLegs or HideChest or HideGloves or HideGorget or HidePants or HidePauldrons;
                }

                if (m_CraftSystem is DefCarpentry)
                {
                    return armor is WingedHelm or RavenHelm or VultureHelm or WoodlandArms or WoodlandChest or WoodlandGloves or WoodlandGorget or WoodlandLegs;
                }

                if (m_CraftSystem is DefBlacksmithy)
                {
                    return armor is Circlet or RoyalCirclet or GemmedCirclet;
                }

                return false;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                int number;

                if (!CheckDeed(from))
                {
                    return;
                }

                var usingDeed = m_Deed != null;
                var toDelete = false;

                // TODO: Make an IRepairable

                if (m_CraftSystem.CanCraft(from, m_Tool, targeted.GetType()) == 1044267)
                {
                    number = 1044282; // You must be near a forge and and anvil to repair items. * Yes, there are two and's *
                }
                else if (m_CraftSystem is DefTinkering && targeted is Golem g)
                {
                    var damage = g.HitsMax - g.Hits;

                    if (g.IsDeadBondedPet)
                    {
                        number = 500426; // You can't repair that.
                    }
                    else if (damage <= 0)
                    {
                        number = 500423; // That is already in full repair.
                    }
                    else
                    {
                        var skillValue = usingDeed ? m_Deed.SkillLevel : from.Skills.Tinkering.Value;

                        if (skillValue < 60.0)
                        {
                            number =
                                1044153; // You don't have the required skills to attempt this item.	//TODO: How does OSI handle this with deeds with golems?
                        }
                        else if (!from.CanBeginAction<Golem>())
                        {
                            number = 501789; // You must wait before trying again.
                        }
                        else
                        {
                            if (damage > (int)(skillValue * 0.3))
                            {
                                damage = (int)(skillValue * 0.3);
                            }

                            damage += 30;

                            if (!from.CheckSkill(SkillName.Tinkering, 0.0, 100.0))
                            {
                                damage /= 2;
                            }

                            var pack = from.Backpack;

                            if (pack != null)
                            {
                                var v = pack.ConsumeUpTo(typeof(IronIngot), (damage + 4) / 5);

                                if (v > 0)
                                {
                                    g.Hits += v * 5;

                                    number = 1044279; // You repair the item.
                                    toDelete = true;

                                    from.BeginAction<Golem>();
                                    Timer.StartTimer(TimeSpan.FromSeconds(12.0), from.EndAction<Golem>);
                                }
                                else
                                {
                                    number = 1044037; // You do not have sufficient metal to make that.
                                }
                            }
                            else
                            {
                                number = 1044037; // You do not have sufficient metal to make that.
                            }
                        }
                    }
                }
                else if (targeted is BaseWeapon weapon)
                {
                    var skill = m_CraftSystem.MainSkill;
                    var toWeaken = 0;

                    if (Core.AOS)
                    {
                        toWeaken = 1;
                    }
                    else if (skill != SkillName.Tailoring)
                    {
                        var skillLevel = usingDeed ? m_Deed.SkillLevel : from.Skills[skill].Base;

                        if (skillLevel >= 90.0)
                        {
                            toWeaken = 1;
                        }
                        else if (skillLevel >= 70.0)
                        {
                            toWeaken = 2;
                        }
                        else
                        {
                            toWeaken = 3;
                        }
                    }

                    if (m_CraftSystem.CraftItems.SearchForSubclass(weapon.GetType()) == null && !IsSpecialWeapon(weapon))
                    {
                        number = usingDeed
                            ? 1061136
                            : 1044277; // That item cannot be repaired. // You cannot repair that item with this type of repair contract.
                    }
                    else if (!weapon.IsChildOf(from.Backpack) && (!Core.ML || weapon.Parent != from))
                    {
                        number = 1044275; // The item must be in your backpack to repair it.
                    }
                    else if (!Core.AOS && weapon.PoisonCharges != 0)
                    {
                        number = 1005012; // You cannot repair an item while a caustic substance is on it.
                    }
                    else if (weapon.MaxHitPoints <= 0 || weapon.HitPoints == weapon.MaxHitPoints)
                    {
                        number = 1044281; // That item is in full repair
                    }
                    else if (weapon.MaxHitPoints <= toWeaken)
                    {
                        number = 1044278; // That item has been repaired many times, and will break if repairs are attempted again.
                    }
                    else
                    {
                        if (CheckWeaken(from, skill, weapon.HitPoints, weapon.MaxHitPoints))
                        {
                            weapon.MaxHitPoints -= toWeaken;
                            weapon.HitPoints = Math.Max(0, weapon.HitPoints - toWeaken);
                        }

                        if (CheckRepairDifficulty(from, skill, weapon.HitPoints, weapon.MaxHitPoints))
                        {
                            number = 1044279; // You repair the item.
                            m_CraftSystem.PlayCraftEffect(from);
                            weapon.HitPoints = weapon.MaxHitPoints;
                        }
                        else
                        {
                            number = usingDeed
                                ? 1061137
                                : 1044280; // You fail to repair the item. [And the contract is destroyed]
                            m_CraftSystem.PlayCraftEffect(from);
                        }

                        toDelete = true;
                    }
                }
                else if (targeted is BaseArmor armor)
                {
                    var skill = m_CraftSystem.MainSkill;
                    var toWeaken = 0;

                    if (Core.AOS)
                    {
                        toWeaken = 1;
                    }
                    else if (skill != SkillName.Tailoring)
                    {
                        var skillLevel = usingDeed ? m_Deed.SkillLevel : from.Skills[skill].Base;

                        if (skillLevel >= 90.0)
                        {
                            toWeaken = 1;
                        }
                        else if (skillLevel >= 70.0)
                        {
                            toWeaken = 2;
                        }
                        else
                        {
                            toWeaken = 3;
                        }
                    }

                    if (m_CraftSystem.CraftItems.SearchForSubclass(armor.GetType()) == null && !IsSpecialArmor(armor))
                    {
                        number = usingDeed
                            ? 1061136
                            : 1044277; // That item cannot be repaired. // You cannot repair that item with this type of repair contract.
                    }
                    else if (!armor.IsChildOf(from.Backpack) && (!Core.ML || armor.Parent != from))
                    {
                        number = 1044275; // The item must be in your backpack to repair it.
                    }
                    else if (armor.MaxHitPoints <= 0 || armor.HitPoints == armor.MaxHitPoints)
                    {
                        number = 1044281; // That item is in full repair
                    }
                    else if (armor.MaxHitPoints <= toWeaken)
                    {
                        number = 1044278; // That item has been repaired many times, and will break if repairs are attempted again.
                    }
                    else
                    {
                        if (CheckWeaken(from, skill, armor.HitPoints, armor.MaxHitPoints))
                        {
                            armor.MaxHitPoints -= toWeaken;
                            armor.HitPoints = Math.Max(0, armor.HitPoints - toWeaken);
                        }

                        if (CheckRepairDifficulty(from, skill, armor.HitPoints, armor.MaxHitPoints))
                        {
                            number = 1044279; // You repair the item.
                            m_CraftSystem.PlayCraftEffect(from);
                            armor.HitPoints = armor.MaxHitPoints;
                        }
                        else
                        {
                            number = usingDeed
                                ? 1061137
                                : 1044280; // You fail to repair the item. [And the contract is destroyed]
                            m_CraftSystem.PlayCraftEffect(from);
                        }

                        toDelete = true;
                    }
                }
                else if (targeted is BaseClothing clothing)
                {
                    var skill = m_CraftSystem.MainSkill;
                    var toWeaken = 0;

                    if (Core.AOS)
                    {
                        toWeaken = 1;
                    }
                    else if (skill != SkillName.Tailoring)
                    {
                        var skillLevel = usingDeed ? m_Deed.SkillLevel : from.Skills[skill].Base;

                        if (skillLevel >= 90.0)
                        {
                            toWeaken = 1;
                        }
                        else if (skillLevel >= 70.0)
                        {
                            toWeaken = 2;
                        }
                        else
                        {
                            toWeaken = 3;
                        }
                    }

                    if (m_CraftSystem.CraftItems.SearchForSubclass(clothing.GetType()) == null &&
                        !IsSpecialClothing(clothing) && !(clothing is TribalMask or HornedTribalMask))
                    {
                        number = usingDeed
                            ? 1061136
                            : 1044277; // That item cannot be repaired. // You cannot repair that item with this type of repair contract.
                    }
                    else if (!clothing.IsChildOf(from.Backpack) && (!Core.ML || clothing.Parent != from))
                    {
                        number = 1044275; // The item must be in your backpack to repair it.
                    }
                    else if (clothing.MaxHitPoints <= 0 || clothing.HitPoints == clothing.MaxHitPoints)
                    {
                        number = 1044281; // That item is in full repair
                    }
                    else if (clothing.MaxHitPoints <= toWeaken)
                    {
                        number = 1044278; // That item has been repaired many times, and will break if repairs are attempted again.
                    }
                    else
                    {
                        if (CheckWeaken(from, skill, clothing.HitPoints, clothing.MaxHitPoints))
                        {
                            clothing.MaxHitPoints -= toWeaken;
                            clothing.HitPoints = Math.Max(0, clothing.HitPoints - toWeaken);
                        }

                        if (CheckRepairDifficulty(from, skill, clothing.HitPoints, clothing.MaxHitPoints))
                        {
                            number = 1044279; // You repair the item.
                            m_CraftSystem.PlayCraftEffect(from);
                            clothing.HitPoints = clothing.MaxHitPoints;
                        }
                        else
                        {
                            number = usingDeed
                                ? 1061137
                                : 1044280; // You fail to repair the item. [And the contract is destroyed]
                            m_CraftSystem.PlayCraftEffect(from);
                        }

                        toDelete = true;
                    }
                }
                else if (!usingDeed && targeted is BlankScroll scroll)
                {
                    var skill = m_CraftSystem.MainSkill;

                    if (from.Skills[skill].Value >= 50.0)
                    {
                        scroll.Consume(1);
                        var deed = new RepairDeed(
                            RepairDeed.GetTypeFor(m_CraftSystem),
                            from.Skills[skill].Value,
                            from
                        );
                        from.AddToBackpack(deed);

                        number = 500442; // You create the item and put it in your backpack.
                    }
                    else
                    {
                        number = 1047005; // You must be at least apprentice level to create a repair service contract.
                    }
                }
                else if (targeted is Item)
                {
                    number = usingDeed
                        ? 1061136
                        : 1044277; // That item cannot be repaired. // You cannot repair that item with this type of repair contract.
                }
                else
                {
                    number = 500426; // You can't repair that.
                }

                if (!usingDeed)
                {
                    var context = m_CraftSystem.GetContext(from);
                    if (!Core.UOTD)
                    {
                        if (number > 0)
                        {
                            from.SendLocalizedMessage(number);
                        }
                        CraftItem.ShowCraftMenu(from, m_CraftSystem, m_Tool);
                    }
                    else
                    {
                        CraftItem.ShowCraftMenu(from, m_CraftSystem, m_Tool, number);
                    }
                }
                else
                {
                    from.SendLocalizedMessage(number);

                    if (toDelete)
                    {
                        m_Deed.Delete();
                    }
                }
            }
        }
    }
}
