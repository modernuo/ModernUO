using Server.Ethics;
using Server.Items;
using Server.Targeting;
using Server.Utilities;

namespace Server.Engines.Craft
{
    public enum SmeltResult
    {
        Success,
        Invalid,
        NoSkill
    }

    public static class Resmelt
    {
        public static void Do(Mobile from, CraftSystem craftSystem, BaseTool tool)
        {
            var num = craftSystem.CanCraft(from, tool, null);

            if (num > 0 && num != 1044267)
            {
                from.SendGump(new CraftGump(from, craftSystem, tool, num));
            }
            else
            {
                from.Target = new InternalTarget(craftSystem, tool);
                from.SendLocalizedMessage(1044273); // Target an item to recycle.
            }
        }

        private class InternalTarget : Target
        {
            private readonly CraftSystem m_CraftSystem;
            private readonly BaseTool m_Tool;

            public InternalTarget(CraftSystem craftSystem, BaseTool tool) : base(2, false, TargetFlags.None)
            {
                m_CraftSystem = craftSystem;
                m_Tool = tool;
            }

            private SmeltResult Resmelt(Mobile from, Item item, CraftResource resource)
            {
                try
                {
                    if (Ethic.IsImbued(item))
                    {
                        return SmeltResult.Invalid;
                    }

                    if (CraftResources.GetType(resource) != CraftResourceType.Metal)
                    {
                        return SmeltResult.Invalid;
                    }

                    var info = CraftResources.GetInfo(resource);

                    if (info == null || info.ResourceTypes.Length == 0)
                    {
                        return SmeltResult.Invalid;
                    }

                    var craftItem = m_CraftSystem.CraftItems.SearchFor(item.GetType());

                    if (craftItem == null || craftItem.Resources.Count == 0)
                    {
                        return SmeltResult.Invalid;
                    }

                    var craftResource = craftItem.Resources[0];

                    if (craftResource.Amount < 2)
                    {
                        return SmeltResult.Invalid; // Not enough metal to resmelt
                    }

                    var difficulty = resource switch
                    {
                        CraftResource.DullCopper => 65.0,
                        CraftResource.ShadowIron => 70.0,
                        CraftResource.Copper     => 75.0,
                        CraftResource.Bronze     => 80.0,
                        CraftResource.Gold       => 85.0,
                        CraftResource.Agapite    => 90.0,
                        CraftResource.Verite     => 95.0,
                        CraftResource.Valorite   => 99.0,
                        _                        => 0.0
                    };

                    if (difficulty > from.Skills.Mining.Value)
                    {
                        return SmeltResult.NoSkill;
                    }

                    var resourceType = info.ResourceTypes[0];
                    var ingot = resourceType.CreateInstance<Item>();

                    if (item is DragonBardingDeed || item is BaseArmor armor && armor.PlayerConstructed ||
                        item is BaseWeapon weapon && weapon.PlayerConstructed ||
                        item is BaseClothing clothing && clothing.PlayerConstructed)
                    {
                        ingot.Amount = craftResource.Amount / 2;
                    }
                    else
                    {
                        ingot.Amount = 1;
                    }

                    item.Delete();
                    from.AddToBackpack(ingot);

                    from.PlaySound(0x2A);
                    from.PlaySound(0x240);
                    return SmeltResult.Success;
                }
                catch
                {
                    // ignored
                }

                return SmeltResult.Invalid;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                var num = m_CraftSystem.CanCraft(from, m_Tool, null);

                if (num > 0)
                {
                    if (num == 1044267)
                    {
                        DefBlacksmithy.CheckAnvilAndForge(from, 2, out var anvil, out var forge);

                        if (!anvil)
                        {
                            num = 1044266; // You must be near an anvil
                        }
                        else if (!forge)
                        {
                            num = 1044265; // You must be near a forge.
                        }
                    }

                    from.SendGump(new CraftGump(from, m_CraftSystem, m_Tool, num));
                }
                else
                {
                    var result = SmeltResult.Invalid;
                    var isStoreBought = false;
                    int message;

                    if (targeted is BaseArmor armor)
                    {
                        result = Resmelt(from, armor, armor.Resource);
                        isStoreBought = !armor.PlayerConstructed;
                    }
                    else if (targeted is BaseWeapon weapon)
                    {
                        result = Resmelt(from, weapon, weapon.Resource);
                        isStoreBought = !weapon.PlayerConstructed;
                    }
                    else if (targeted is DragonBardingDeed deed)
                    {
                        result = Resmelt(from, deed, deed.Resource);
                    }

                    message = result switch
                    {
                        SmeltResult.Invalid => 1044272,
                        SmeltResult.NoSkill => 1044269,
                        SmeltResult.Success => isStoreBought ? 500418 : 1044270,
                        _                   => 1044272
                    };

                    from.SendGump(new CraftGump(from, m_CraftSystem, m_Tool, message));
                }
            }
        }
    }
}
