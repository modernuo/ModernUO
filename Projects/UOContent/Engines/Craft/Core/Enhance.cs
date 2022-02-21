using System;
using Server.Items;
using Server.Targeting;

namespace Server.Engines.Craft
{
    public enum EnhanceResult
    {
        None,
        NotInBackpack,
        BadItem,
        BadResource,
        AlreadyEnhanced,
        Success,
        Failure,
        Broken,
        NoResources,
        NoSkill
    }

    public static class Enhance
    {
        public static EnhanceResult Invoke(
            Mobile from, CraftSystem craftSystem, BaseTool tool, Item item,
            CraftResource resource, Type resType, ref TextDefinition resMessage
        )
        {
            if (item == null)
            {
                return EnhanceResult.BadItem;
            }

            if (!item.IsChildOf(from.Backpack))
            {
                return EnhanceResult.NotInBackpack;
            }

            if (item is not BaseArmor && item is not BaseWeapon)
            {
                return EnhanceResult.BadItem;
            }

            if (item is IArcaneEquip eq && eq.IsArcane)
            {
                return EnhanceResult.BadItem;
            }

            if (CraftResources.IsStandard(resource))
            {
                return EnhanceResult.BadResource;
            }

            var num = craftSystem.CanCraft(from, tool, item.GetType());

            if (num > 0)
            {
                resMessage = num;
                return EnhanceResult.None;
            }

            var craftItem = craftSystem.CraftItems.SearchFor(item.GetType());

            if (craftItem == null || craftItem.Resources.Count == 0)
            {
                return EnhanceResult.BadItem;
            }

            if (craftItem.GetSuccessChance(from, resType, craftSystem, false, out _) <= 0.0)
            {
                return EnhanceResult.NoSkill;
            }

            var info = CraftResources.GetInfo(resource);

            if (info == null || info.ResourceTypes.Length == 0)
            {
                return EnhanceResult.BadResource;
            }

            var attributes = info.AttributeInfo;

            if (attributes == null)
            {
                return EnhanceResult.BadResource;
            }

            int resHue = 0, maxAmount = 0;

            if (!craftItem.ConsumeRes(
                from,
                resType,
                craftSystem,
                ref resHue,
                ref maxAmount,
                ConsumeType.None,
                ref resMessage
            ))
            {
                return EnhanceResult.NoResources;
            }

            if (craftSystem is DefBlacksmithy)
            {
                if (from.FindItemOnLayer(Layer.OneHanded) is AncientSmithyHammer hammer)
                {
                    hammer.UsesRemaining--;
                    if (hammer.UsesRemaining < 1)
                    {
                        hammer.Delete();
                    }
                }
            }

            int phys = 0, fire = 0, cold = 0, pois = 0, nrgy = 0;
            int dura, luck, lreq, dinc = 0;
            int baseChance;

            var physBonus = false;
            bool fireBonus;
            bool coldBonus;
            bool nrgyBonus;
            bool poisBonus;
            bool duraBonus;
            bool luckBonus;
            bool lreqBonus;
            bool dincBonus;

            if (item is BaseWeapon weapon)
            {
                if (!CraftResources.IsStandard(weapon.Resource))
                {
                    return EnhanceResult.AlreadyEnhanced;
                }

                baseChance = 20;

                dura = weapon.MaxHitPoints;
                luck = weapon.Attributes.Luck;
                lreq = weapon.WeaponAttributes.LowerStatReq;
                dinc = weapon.Attributes.WeaponDamage;

                fireBonus = attributes.WeaponFireDamage > 0;
                coldBonus = attributes.WeaponColdDamage > 0;
                nrgyBonus = attributes.WeaponEnergyDamage > 0;
                poisBonus = attributes.WeaponPoisonDamage > 0;

                duraBonus = attributes.WeaponDurability > 0;
                luckBonus = attributes.WeaponLuck > 0;
                lreqBonus = attributes.WeaponLowerRequirements > 0;
                dincBonus = dinc > 0;
            }
            else
            {
                var armor = (BaseArmor)item;

                if (!CraftResources.IsStandard(armor.Resource))
                {
                    return EnhanceResult.AlreadyEnhanced;
                }

                baseChance = 20;

                phys = armor.PhysicalResistance;
                fire = armor.FireResistance;
                cold = armor.ColdResistance;
                pois = armor.PoisonResistance;
                nrgy = armor.EnergyResistance;

                dura = armor.MaxHitPoints;
                luck = armor.Attributes.Luck;
                lreq = armor.ArmorAttributes.LowerStatReq;

                physBonus = attributes.ArmorPhysicalResist > 0;
                fireBonus = attributes.ArmorFireResist > 0;
                coldBonus = attributes.ArmorColdResist > 0;
                nrgyBonus = attributes.ArmorEnergyResist > 0;
                poisBonus = attributes.ArmorPoisonResist > 0;

                duraBonus = attributes.ArmorDurability > 0;
                luckBonus = attributes.ArmorLuck > 0;
                lreqBonus = attributes.ArmorLowerRequirements > 0;
                dincBonus = false;
            }

            var skill = from.Skills[craftSystem.MainSkill].Fixed / 10;

            if (skill >= 100)
            {
                baseChance -= (skill - 90) / 10;
            }

            var res = EnhanceResult.Success;

            if (physBonus)
            {
                CheckResult(ref res, baseChance + phys);
            }

            if (fireBonus)
            {
                CheckResult(ref res, baseChance + fire);
            }

            if (coldBonus)
            {
                CheckResult(ref res, baseChance + cold);
            }

            if (nrgyBonus)
            {
                CheckResult(ref res, baseChance + nrgy);
            }

            if (poisBonus)
            {
                CheckResult(ref res, baseChance + pois);
            }

            if (duraBonus)
            {
                CheckResult(ref res, baseChance + dura / 40);
            }

            if (luckBonus)
            {
                CheckResult(ref res, baseChance + 10 + luck / 2);
            }

            if (lreqBonus)
            {
                CheckResult(ref res, baseChance + lreq / 4);
            }

            if (dincBonus)
            {
                CheckResult(ref res, baseChance + dinc / 4);
            }

            switch (res)
            {
                case EnhanceResult.Broken:
                    {
                        if (!craftItem.ConsumeRes(
                            from,
                            resType,
                            craftSystem,
                            ref resHue,
                            ref maxAmount,
                            ConsumeType.Half,
                            ref resMessage
                        ))
                        {
                            return EnhanceResult.NoResources;
                        }

                        item.Delete();
                        break;
                    }
                case EnhanceResult.Success:
                    {
                        if (!craftItem.ConsumeRes(
                            from,
                            resType,
                            craftSystem,
                            ref resHue,
                            ref maxAmount,
                            ConsumeType.All,
                            ref resMessage
                        ))
                        {
                            return EnhanceResult.NoResources;
                        }

                        if (item is BaseWeapon w)
                        {
                            w.Resource = resource;

                            var hue = w.GetElementalDamageHue();
                            if (hue > 0)
                            {
                                w.Hue = hue;
                            }
                        }
                        else
                        {
                            ((BaseArmor)item).Resource = resource;
                        }

                        break;
                    }
                case EnhanceResult.Failure:
                    {
                        if (!craftItem.ConsumeRes(
                            from,
                            resType,
                            craftSystem,
                            ref resHue,
                            ref maxAmount,
                            ConsumeType.Half,
                            ref resMessage
                        ))
                        {
                            return EnhanceResult.NoResources;
                        }

                        break;
                    }
            }

            return res;
        }

        public static void CheckResult(ref EnhanceResult res, int chance)
        {
            if (res != EnhanceResult.Success)
            {
                return; // we've already failed..
            }

            var random = Utility.Random(100);

            if (random < 10)
            {
                res = EnhanceResult.Failure;
            }
            else if (chance > random)
            {
                res = EnhanceResult.Broken;
            }
        }

        public static void BeginTarget(Mobile from, CraftSystem craftSystem, BaseTool tool)
        {
            var context = craftSystem.GetContext(from);

            if (context == null)
            {
                return;
            }

            var lastRes = context.LastResourceIndex;
            var subRes = craftSystem.CraftSubRes;

            if (lastRes >= 0 && lastRes < subRes.Count)
            {
                var res = subRes.GetAt(lastRes);

                if (from.Skills[craftSystem.MainSkill].Value < res.RequiredSkill)
                {
                    from.SendGump(new CraftGump(from, craftSystem, tool, res.Message));
                }
                else
                {
                    var resource = CraftResources.GetFromType(res.ItemType);

                    if (resource != CraftResource.None)
                    {
                        from.Target = new InternalTarget(craftSystem, tool, res.ItemType, resource);
                        // Target an item to enhance with the properties of your selected material.
                        from.SendLocalizedMessage(1061004);
                    }
                    else
                    {
                        from.SendGump(
                            new CraftGump(
                                from,
                                craftSystem,
                                tool,
                                // You must select a special material in order to enhance an item with its properties.
                                1061010
                            )
                        );
                    }
                }
            }
            else
            {
                from.SendGump(
                    new CraftGump(
                        from,
                        craftSystem,
                        tool,
                        // You must select a special material in order to enhance an item with its properties.
                        1061010
                    )
                );
            }
        }

        private class InternalTarget : Target
        {
            private readonly CraftSystem m_CraftSystem;
            private readonly CraftResource m_Resource;
            private readonly Type m_ResourceType;
            private readonly BaseTool m_Tool;

            public InternalTarget(CraftSystem craftSystem, BaseTool tool, Type resourceType, CraftResource resource) : base(
                2,
                false,
                TargetFlags.None
            )
            {
                m_CraftSystem = craftSystem;
                m_Tool = tool;
                m_ResourceType = resourceType;
                m_Resource = resource;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Item item)
                {
                    TextDefinition message = null;
                    var res = Enhance.Invoke(
                        from,
                        m_CraftSystem,
                        m_Tool,
                        item,
                        m_Resource,
                        m_ResourceType,
                        ref message
                    );

                    message = res switch
                    {
                        EnhanceResult.NotInBackpack   => 1061005,
                        EnhanceResult.AlreadyEnhanced => 1061012,
                        EnhanceResult.BadItem         => 1061011,
                        EnhanceResult.BadResource     => 1061010,
                        EnhanceResult.Broken          => 1061080,
                        EnhanceResult.Failure         => 1061082,
                        EnhanceResult.Success         => 1061008,
                        EnhanceResult.NoSkill         => 1044153,
                        _                             => message
                    };

                    from.SendGump(new CraftGump(from, m_CraftSystem, m_Tool, message));
                }
            }
        }
    }
}
