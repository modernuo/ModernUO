using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Factions;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Utilities;

namespace Server.Engines.Craft
{
    public enum ConsumeType
    {
        All,
        Half,
        None
    }

    public interface ICraftable
    {
        int OnCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
            CraftItem craftItem, int resHue
        );
    }

    public class CraftItem
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(CraftItem));

        private static readonly Dictionary<Type, int> _itemIds = new();

        private static readonly int[] m_HeatSources =
        {
            0x461, 0x48E,   // Sandstone oven/fireplace
            0x92B, 0x96C,   // Stone oven/fireplace
            0xDE3, 0xDE9,   // Campfire
            0xFAC, 0xFAC,   // Firepit
            0x184A, 0x184C, // Heating stand (left)
            0x184E, 0x1850, // Heating stand (right)
            0x398C, 0x399F, // Fire field
            0x2DDB, 0x2DDC, // Elven stove
            0x19AA, 0x19BB, // Veteran Reward Brazier
            0x197A, 0x19A9, // Large Forge
            0x0FB1, 0x0FB1, // Small Forge
            0x2DD8, 0x2DD8  // Elven Forge
        };

        private static readonly int[] m_Ovens =
        {
            0x461, 0x46F,  // Sandstone oven
            0x92B, 0x93F,  // Stone oven
            0x2DDB, 0x2DDC // Elven stove
        };

        private static readonly int[] m_Mills =
        {
            0x1920, 0x1921, 0x1922, 0x1923, 0x1924, 0x1295, 0x1926, 0x1928,
            0x192C, 0x192D, 0x192E, 0x129F, 0x1930, 0x1931, 0x1932, 0x1934
        };

        private static readonly Type[][] m_TypesTable =
        {
            new[] { typeof(Log), typeof(Board) },
            new[] { typeof(HeartwoodLog), typeof(HeartwoodBoard) },
            new[] { typeof(BloodwoodLog), typeof(BloodwoodBoard) },
            new[] { typeof(FrostwoodLog), typeof(FrostwoodBoard) },
            new[] { typeof(OakLog), typeof(OakBoard) },
            new[] { typeof(AshLog), typeof(AshBoard) },
            new[] { typeof(YewLog), typeof(YewBoard) },
            new[] { typeof(Leather), typeof(Hides) },
            new[] { typeof(SpinedLeather), typeof(SpinedHides) },
            new[] { typeof(HornedLeather), typeof(HornedHides) },
            new[] { typeof(BarbedLeather), typeof(BarbedHides) },
            new[] { typeof(BlankMap), typeof(BlankScroll) },
            new[] { typeof(Cloth), typeof(UncutCloth) },
            new[] { typeof(CheeseWheel), typeof(CheeseWedge) },
            new[] { typeof(Pumpkin), typeof(SmallPumpkin) },
            new[] { typeof(WoodenBowlOfPeas), typeof(PewterBowlOfPeas) }
        };

        private static readonly Type[] m_ColoredItemTable =
        {
            typeof(BaseWeapon), typeof(BaseArmor), typeof(BaseClothing),
            typeof(BaseJewel), typeof(DragonBardingDeed)
        };

        private static readonly Type[] m_ColoredResourceTable =
        {
            typeof(BaseIngot), typeof(BaseOre),
            typeof(BaseLeather), typeof(BaseHides),
            typeof(UncutCloth), typeof(Cloth),
            typeof(BaseGranite), typeof(BaseScales)
        };

        private static readonly Type[] m_MarkableTable =
        {
            typeof(BaseArmor),
            typeof(BaseWeapon),
            typeof(BaseClothing),
            typeof(BaseInstrument),
            typeof(DragonBardingDeed),
            typeof(BaseTool),
            typeof(BaseHarvestTool),
            typeof(FukiyaDarts), typeof(Shuriken),
            typeof(Spellbook), typeof(Runebook),
            typeof(BaseQuiver)
        };

        private static readonly Type[] m_NeverColorTable =
        {
            typeof(OrcHelm)
        };

        private int m_ResAmount;

        private int m_ResHue;
        private CraftSystem m_System;
        private int _itemId;

        public CraftItem(Type type, TextDefinition groupName, TextDefinition name) : this(type, groupName, name, -1)
        {
        }

        public CraftItem(Type type, TextDefinition groupName, TextDefinition name, int itemId)
        {
            Resources = new List<CraftRes>();
            Skills = new List<CraftSkill>();

            ItemType = type;

            GroupNameString = groupName;
            NameString = name;

            GroupNameNumber = groupName;
            NameNumber = name;

            RequiredBeverage = BeverageType.Water;
            _itemId = itemId;
        }

        public bool ForceNonExceptional { get; set; }

        public Expansion RequiredExpansion { get; set; }

        public Recipe Recipe { get; private set; }

        public BeverageType RequiredBeverage { get; set; }

        public int Mana { get; set; }

        public int Hits { get; set; }

        public int Stam { get; set; }

        public bool UseSubRes2 { get; set; }

        public bool UseAllRes { get; set; }

        public bool NeedHeat { get; set; }

        public bool NeedOven { get; set; }

        public bool NeedMill { get; set; }

        public Type ItemType { get; }

        public int ItemHue { get; set; }

        public string GroupNameString { get; }

        public int GroupNameNumber { get; }

        public string NameString { get; }

        public int NameNumber { get; }

        public int ItemId => _itemId == -1 ? _itemId = ItemIDOf(ItemType) : _itemId;

        public List<CraftRes> Resources { get; }

        public List<CraftSkill> Skills { get; }

        public void AddRecipe(int id, CraftSystem system)
        {
            if (Recipe != null)
            {
                logger.Warning(
                    "Attempted add of recipe #{Id} to the crafting of {ItemTypeName} in CraftSystem {CraftSystem}.",
                    id,
                    ItemType.Name,
                    system
                );
                return;
            }

            Recipe = new Recipe(id, system, this);
        }

        public static int LabelNumber(Type type)
        {
            var number = ItemIDOf(type);

            return number + (number >= 0x4000 ? 1078872 : 1020000);
        }

        public static int ItemIDOf(Type type)
        {
            if (_itemIds.TryGetValue(type, out var itemId))
            {
                return itemId;
            }

            if (type == typeof(FactionExplosionTrap))
            {
                itemId = 14034;
            }
            else if (type == typeof(FactionGasTrap))
            {
                itemId = 4523;
            }
            else if (type == typeof(FactionSawTrap))
            {
                itemId = 4359;
            }
            else if (type == typeof(FactionSpikeTrap))
            {
                itemId = 4517;
            }

            if (itemId == 0)
            {
                var attrs = type.GetCustomAttributes(typeof(CraftItemIDAttribute), false);

                if (attrs.Length > 0)
                {
                    var craftItemID = (CraftItemIDAttribute)attrs[0];
                    itemId = craftItemID.ItemID;
                }
            }

            if (itemId == 0)
            {
                Item item = null;

                try
                {
                    item = type.CreateInstance<Item>();
                }
                catch
                {
                    // ignored
                }

                if (item != null)
                {
                    itemId = item.ItemID;
                    item.Delete();
                }
            }

            _itemIds[type] = itemId;

            return itemId;
        }

        public void AddRes(Type type, int amount, TextDefinition message) => AddRes(type, null, amount, message);

        public void AddRes(Type type, TextDefinition name, int amount) => AddRes(type, name, amount, "");

        public void AddRes(Type type, TextDefinition name, int amount, TextDefinition message)
        {
            var craftRes = new CraftRes(type, name, amount, message);
            Resources.Add(craftRes);
        }

        public void AddSkill(SkillName skillToMake, double minSkill, double maxSkill)
        {
            var craftSkill = new CraftSkill(skillToMake, minSkill, maxSkill);
            Skills.Add(craftSkill);
        }

        public bool ConsumeAttributes(Mobile from, ref TextDefinition message, bool consume)
        {
            if (Hits > 0 && from.Hits < Hits)
            {
                message = "You lack the required hit points to make that.";
                return false;
            }

            if (Mana > 0 && from.Mana < Mana)
            {
                message = "You lack the required mana to make that.";
                return false;
            }

            if (Stam > 0 && from.Stam < Stam)
            {
                message = "You lack the required stamina to make that.";
                return false;
            }

            if (consume)
            {
                from.Mana -= Mana;
                from.Hits -= Hits;
                from.Stam -= Stam;
            }

            return true;
        }

        public bool IsMarkable(Type type)
        {
            if (ForceNonExceptional) // Don't even display the stuff for marking if it can't ever be exceptional.
            {
                return false;
            }

            for (var i = 0; i < m_MarkableTable.Length; ++i)
            {
                if (type == m_MarkableTable[i] || type.IsSubclassOf(m_MarkableTable[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool RetainsColor(Type type)
        {
            var neverColor = false;

            for (var i = 0; !neverColor && i < m_NeverColorTable.Length; ++i)
            {
                neverColor = type == m_NeverColorTable[i] || type.IsSubclassOf(m_NeverColorTable[i]);
            }

            if (neverColor)
            {
                return false;
            }

            var inItemTable = false;

            for (var i = 0; !inItemTable && i < m_ColoredItemTable.Length; ++i)
            {
                inItemTable = type == m_ColoredItemTable[i] || type.IsSubclassOf(m_ColoredItemTable[i]);
            }

            return inItemTable;
        }

        public bool RetainsColorFrom(CraftSystem system, Type type)
        {
            if (system.RetainsColorFrom(this, type))
            {
                return true;
            }

            var inItemTable = RetainsColor(ItemType);

            if (!inItemTable)
            {
                return false;
            }

            var inResourceTable = false;

            for (var i = 0; !inResourceTable && i < m_ColoredResourceTable.Length; ++i)
            {
                inResourceTable = type == m_ColoredResourceTable[i] || type.IsSubclassOf(m_ColoredResourceTable[i]);
            }

            return inResourceTable;
        }

        public static bool Find(Mobile from, int[] itemIDs)
        {
            var map = from.Map;

            if (map == null)
            {
                return false;
            }

            var eable = map.GetItemsInRange(from.Location, 2);
            foreach (var item in eable)
            {
                if (item.Z + 16 > item.Z && item.Z + 16 > item.Z && Find(item.ItemID, itemIDs))
                {
                    eable.Free();
                    return true;
                }
            }

            for (var x = -2; x <= 2; ++x)
            {
                for (var y = -2; y <= 2; ++y)
                {
                    var vx = from.X + x;
                    var vy = from.Y + y;

                    var tiles = map.Tiles.GetStaticTiles(vx, vy, true);

                    for (var i = 0; i < tiles.Length; ++i)
                    {
                        var z = tiles[i].Z;
                        var id = tiles[i].ID;

                        if (z + 16 > from.Z && from.Z + 16 > z && Find(id, itemIDs))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool Find(int itemID, int[] itemIDs)
        {
            var contains = false;

            for (var i = 0; !contains && i < itemIDs.Length; i += 2)
            {
                contains = itemID >= itemIDs[i] && itemID <= itemIDs[i + 1];
            }

            return contains;
        }

        public static bool IsQuantityType(Type[][] types)
        {
            for (int i = 0; i < types.Length; ++i)
            {
                Type[] check = types[i];

                for (int j = 0; j < check.Length; ++j)
                {
                    if (typeof(IHasQuantity).IsAssignableFrom(check[j]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public int ConsumeQuantity(Container cont, Type[][] types, int[] amounts)
        {
            if (types.Length != amounts.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(types));
            }

            // TODO: Optimize allocation
            var items = new Item[types.Length][];
            var totals = new int[types.Length];

            for (var i = 0; i < types.Length; ++i)
            {
                items[i] = cont.FindItemsByType(types[i]);

                for (var j = 0; j < items[i].Length; ++j)
                {
                    if (items[i][j] is not IHasQuantity hq)
                    {
                        totals[i] += items[i][j].Amount;
                    }
                    else if (hq is not BaseBeverage beverage || beverage.Content == RequiredBeverage)
                    {
                        totals[i] += hq.Quantity;
                    }
                }

                if (totals[i] < amounts[i])
                {
                    return i;
                }
            }

            for (var i = 0; i < types.Length; ++i)
            {
                var need = amounts[i];

                for (var j = 0; j < items[i].Length; ++j)
                {
                    var item = items[i][j];

                    if (item is not IHasQuantity hq)
                    {
                        var theirAmount = item.Amount;

                        if (theirAmount < need)
                        {
                            item.Delete();
                            need -= theirAmount;
                        }
                        else
                        {
                            item.Consume(need);
                            break;
                        }
                    }
                    else
                    {
                        if (hq is BaseBeverage beverage && beverage.Content != RequiredBeverage)
                        {
                            continue;
                        }

                        var theirAmount = hq.Quantity;

                        if (theirAmount < need)
                        {
                            hq.Quantity -= theirAmount;
                            need -= theirAmount;
                        }
                        else
                        {
                            hq.Quantity -= need;
                            break;
                        }
                    }
                }
            }

            return -1;
        }

        public int GetQuantity(Container cont, Type[] types)
        {
            var items = cont.FindItemsByType(types);

            var amount = 0;

            for (var i = 0; i < items.Length; ++i)
            {
                if (items[i] is not IHasQuantity hq)
                {
                    amount += items[i].Amount;
                }
                else
                {
                    if ((hq as BaseBeverage)?.Content != RequiredBeverage)
                    {
                        continue;
                    }

                    amount += hq.Quantity;
                }
            }

            return amount;
        }

        public bool ConsumeRes(
            Mobile from, Type typeRes, CraftSystem craftSystem, ref int resHue, ref int maxAmount,
            ConsumeType consumeType, ref TextDefinition message
        ) =>
            ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, consumeType, ref message, false);

        public bool ConsumeRes(
            Mobile from, Type typeRes, CraftSystem craftSystem, ref int resHue, ref int maxAmount,
            ConsumeType consumeType, ref TextDefinition message, bool isFailure
        )
        {
            var ourPack = from.Backpack;

            if (ourPack == null)
            {
                return false;
            }

            if (NeedHeat && !Find(from, m_HeatSources))
            {
                message = 1044487; // You must be near a fire source to cook.
                return false;
            }

            if (NeedOven && !Find(from, m_Ovens))
            {
                message = 1044493; // You must be near an oven to bake that.
                return false;
            }

            if (NeedMill && !Find(from, m_Mills))
            {
                message = 1044491; // You must be near a flour mill to do that.
                return false;
            }

            var types = new Type[Resources.Count][];
            var amounts = new int[Resources.Count];

            maxAmount = int.MaxValue;

            var resCol = UseSubRes2 ? craftSystem.CraftSubRes2 : craftSystem.CraftSubRes;

            CraftRes res;
            for (var i = 0; i < types.Length; ++i)
            {
                var craftRes = Resources[i];
                var baseType = craftRes.ItemType;

                // Resource Mutation
                if (baseType == resCol.ResType && typeRes != null)
                {
                    baseType = typeRes;

                    var subResource = resCol.SearchFor(baseType);

                    if (subResource != null && from.Skills[craftSystem.MainSkill].Base < subResource.RequiredSkill)
                    {
                        message = subResource.Message;
                        return false;
                    }
                }
                // ******************

                for (var j = 0; types[i] == null && j < m_TypesTable.Length; ++j)
                {
                    if (m_TypesTable[j][0] == baseType)
                    {
                        types[i] = m_TypesTable[j];
                    }
                }

                types[i] ??= new[] { baseType };
                amounts[i] = craftRes.Amount;

                // For stackable items that can be crafted more than one at a time
                if (UseAllRes)
                {
                    var tempAmount = ourPack.GetAmount(types[i]);
                    tempAmount /= amounts[i];
                    if (tempAmount < maxAmount)
                    {
                        maxAmount = tempAmount;

                        if (maxAmount == 0)
                        {
                            res = Resources[i];

                            if (res.Message.Number > 0)
                            {
                                message = res.Message.Number;
                            }
                            else if (!string.IsNullOrEmpty(res.Message.String))
                            {
                                message = res.Message.String;
                            }
                            else
                            {
                                message = 502925; // You don't have the resources required to make that item.
                            }

                            return false;
                        }
                    }
                }
                // ****************************

                if (isFailure && !craftSystem.ConsumeOnFailure(from, types[i][0], this))
                {
                    amounts[i] = 0;
                }
            }

            // We adjust the amount of each resource to consume the max possible
            if (UseAllRes)
            {
                for (var i = 0; i < amounts.Length; ++i)
                {
                    amounts[i] *= maxAmount;
                }
            }
            else
            {
                maxAmount = -1;
            }

            RecallRune consumeExtra = null;

            if (NameNumber == 1041267)
            {
                // Runebooks are a special case, they need a blank recall rune
                consumeExtra = ourPack.FindItemsByType<RecallRune>().Find(rune => !rune.Marked);

                if (consumeExtra == null)
                {
                    message = 1044253; // You don't have the components needed to make that.
                    return false;
                }
            }

            int index;

            if (consumeType == ConsumeType.None)
            {
                index = -1;

                var isQuantityType = IsQuantityType(types);

                // TODO: Optimize this
                for (var i = 0; i < types.Length; i++)
                {
                    var quantity = isQuantityType
                        ? GetQuantity(ourPack, types[i])
                        : ourPack.GetBestGroupAmount(types[i], true, CheckHueGrouping);

                    if (quantity < amounts[i])
                    {
                        index = i;
                        break;
                    }
                }
            }
            else
            {
                if (consumeType == ConsumeType.Half)
                {
                    for (var i = 0; i < amounts.Length; i++)
                    {
                        amounts[i] /= 2;

                        if (amounts[i] < 1)
                        {
                            amounts[i] = 1;
                        }
                    }
                }

                m_ResHue = 0;
                m_ResAmount = 0;
                m_System = craftSystem;

                index = IsQuantityType(types)
                    ? ConsumeQuantity(ourPack, types, amounts)
                    : ourPack.ConsumeTotalGrouped(types, amounts, true, OnResourceConsumed, CheckHueGrouping);

                resHue = m_ResHue;
            }

            if (index == -1)
            {
                if (consumeType != ConsumeType.None)
                {
                    consumeExtra?.Delete();
                }

                return true;
            }

            res = Resources[index];

            if (res.Message.Number > 0)
            {
                message = res.Message.Number;
            }
            else if (!string.IsNullOrEmpty(res.Message.String))
            {
                message = res.Message.String;
            }
            else
            {
                message = 502925; // You don't have the resources required to make that item.
            }

            return false;
        }

        private void OnResourceConsumed(Item item, int amount)
        {
            if (!RetainsColorFrom(m_System, item.GetType()))
            {
                return;
            }

            if (amount >= m_ResAmount)
            {
                m_ResHue = item.Hue;
                m_ResAmount = amount;
            }
        }

        private static int CheckHueGrouping(Item a, Item b) => b.Hue.CompareTo(a.Hue);

        public double GetExceptionalChance(CraftSystem system, double chance, Mobile from)
        {
            if (ForceNonExceptional)
            {
                return 0.0;
            }

            var bonus = 0.0;

            if (from.Talisman is BaseTalisman talisman && talisman.Skill == system.MainSkill)
            {
                chance -= talisman.SuccessBonus / 100.0;
                bonus = talisman.ExceptionalBonus / 100.0;
            }

            chance = system.ECA switch
            {
                CraftECA.FiftyPercentChanceMinusTenPercent => chance * 0.5 - 0.1,
                CraftECA.ChanceMinusSixtyToFortyFive => chance - Math.Clamp(
                    0.60 - (from.Skills[system.MainSkill].Value - 95.0) * 0.03,
                    0.45,
                    0.60
                ),
                _ => chance - 0.6
            };

            return chance > 0 ? chance + bonus : chance;
        }

        public bool CheckSkills(
            Mobile from, Type typeRes, CraftSystem craftSystem, ref int quality, out bool allRequiredSkills
        ) => CheckSkills(from, typeRes, craftSystem, ref quality, out allRequiredSkills, true);

        public bool CheckSkills(
            Mobile from, Type typeRes, CraftSystem craftSystem, ref int quality,
            out bool allRequiredSkills, bool gainSkills
        )
        {
            var chance = GetSuccessChance(from, typeRes, craftSystem, gainSkills, out allRequiredSkills);

            if (GetExceptionalChance(craftSystem, chance, from) > Utility.RandomDouble())
            {
                quality = 2;
            }

            return chance > Utility.RandomDouble();
        }

        public double GetSuccessChance(
            Mobile from, Type typeRes, CraftSystem craftSystem, bool gainSkills,
            out bool allRequiredSkills
        )
        {
            var minMainSkill = 0.0;
            var maxMainSkill = 0.0;
            var valMainSkill = 0.0;

            allRequiredSkills = true;

            for (var i = 0; i < Skills.Count; i++)
            {
                var craftSkill = Skills[i];

                var minSkill = craftSkill.MinSkill;
                var maxSkill = craftSkill.MaxSkill;
                var valSkill = from.Skills[craftSkill.SkillToMake].Value;

                if (valSkill < minSkill)
                {
                    allRequiredSkills = false;
                }

                if (craftSkill.SkillToMake == craftSystem.MainSkill)
                {
                    minMainSkill = minSkill;
                    maxMainSkill = maxSkill;
                    valMainSkill = valSkill;
                }

                if (gainSkills) // This is a passive check. Success chance is entirely dependant on the main skill
                {
                    from.CheckSkill(craftSkill.SkillToMake, minSkill, maxSkill);
                }
            }

            if (!allRequiredSkills)
            {
                return 0;
            }

            var minChance = craftSystem.GetChanceAtMin(this);

            double chance = minChance + (valMainSkill - minMainSkill) / (maxMainSkill - minMainSkill) * (1.0 - minChance);

            if (from.Talisman is BaseTalisman talisman && talisman.Skill == craftSystem.MainSkill)
            {
                chance += talisman.SuccessBonus / 100.0;
            }

            return chance;
        }

        public void Craft(Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool)
        {
            if (!from.BeginAction<CraftSystem>())
            {
                from.SendLocalizedMessage(500119); // You must wait to perform another action
                return;
            }

            if (RequiredExpansion != Expansion.None && from.NetState?.SupportsExpansion(RequiredExpansion) != true)
            {
                from.EndAction<CraftSystem>();
                from.SendGump(
                    new CraftGump(
                        from,
                        craftSystem,
                        tool,
                        // The {0} expansion is required to attempt this item.
                        RequiredExpansionMessage(RequiredExpansion)
                    )
                );
                return;
            }

            var chance = GetSuccessChance(from, typeRes, craftSystem, false, out var allRequiredSkills);

            if (!allRequiredSkills || chance <= 0.0)
            {
                from.EndAction<CraftSystem>();
                from.SendGump(
                    new CraftGump(
                        from,
                        craftSystem,
                        tool,
                        1044153 // You don't have the required skills to attempt this item.
                    )
                );
                return;
            }

            if (Recipe != null && (from as PlayerMobile)?.HasRecipe(Recipe) == false)
            {
                from.EndAction<CraftSystem>();
                from.SendGump(
                    new CraftGump(
                        from,
                        craftSystem,
                        tool,
                        1072847 // You must learn that recipe from a scroll.
                    )
                );
                return;
            }

            var badCraft = craftSystem.CanCraft(from, tool, ItemType);

            if (badCraft > 0)
            {
                from.EndAction<CraftSystem>();
                from.SendGump(new CraftGump(from, craftSystem, tool, badCraft));
                return;
            }

            var resHue = 0;
            var maxAmount = 0;
            TextDefinition message = null;

            if (!ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, ConsumeType.None, ref message))
            {
                from.EndAction<CraftSystem>();
                from.SendGump(new CraftGump(from, craftSystem, tool, message));
                return;
            }

            message = null;

            if (!ConsumeAttributes(from, ref message, false))
            {
                from.EndAction<CraftSystem>();
                from.SendGump(new CraftGump(from, craftSystem, tool, message));
                return;
            }

            var context = craftSystem.GetContext(from);

            context?.OnMade(this);

            var iMin = craftSystem.MinCraftEffect;
            var iMax = craftSystem.MaxCraftEffect - iMin + 1;
            var iRandom = Utility.Random(iMax);
            iRandom += iMin + 1;
            new InternalTimer(from, craftSystem, this, typeRes, tool, iRandom).Start();
        }

        private static TextDefinition RequiredExpansionMessage(Expansion expansion)
        {
            return expansion switch
            {
                Expansion.SE => 1063307, // The "Samurai Empire" expansion is required to attempt this item.
                Expansion.ML => 1072650, // The "Mondain's Legacy" expansion is required to attempt this item.
                _ => $"The \"{ExpansionInfo.GetInfo(expansion).Name}\" expansion is required to attempt this item."
            };
        }

        public void CompleteCraft(
            int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes,
            BaseTool tool, CustomCraft customCraft
        )
        {
            var badCraft = craftSystem.CanCraft(from, tool, ItemType);

            if (badCraft > 0)
            {
                if (tool?.Deleted == false && tool.UsesRemaining > 0)
                {
                    from.SendGump(new CraftGump(from, craftSystem, tool, badCraft));
                }
                else
                {
                    from.SendLocalizedMessage(badCraft);
                }

                return;
            }

            int checkResHue = 0, checkMaxAmount = 0;
            TextDefinition checkMessage = null;

            // Not enough resource to craft it
            if (!(ConsumeRes(
                      from,
                      typeRes,
                      craftSystem,
                      ref checkResHue,
                      ref checkMaxAmount,
                      ConsumeType.None,
                      ref checkMessage
                  )
                  && ConsumeAttributes(from, ref checkMessage, false)))
            {
                if (tool?.Deleted == false && tool.UsesRemaining > 0)
                {
                    from.SendGump(new CraftGump(from, craftSystem, tool, checkMessage));
                }
                else if (checkMessage.Number > 0)
                {
                    from.SendLocalizedMessage(checkMessage.Number);
                }
                else
                {
                    from.SendMessage(checkMessage.String);
                }

                return;
            }

            var toolBroken = false;

            var ignored = 1;
            var endquality = 1;
            var resHue = 0;
            var maxAmount = 0;
            TextDefinition message = null;
            var num = 0;

            if (CheckSkills(from, typeRes, craftSystem, ref ignored, out var allRequiredSkills))
            {
                // Not enough resource to craft it
                if (!(ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, ConsumeType.All, ref message)
                      && ConsumeAttributes(from, ref message, true)))
                {
                    if (tool?.Deleted == false && tool.UsesRemaining > 0)
                    {
                        from.SendGump(new CraftGump(from, craftSystem, tool, message));
                    }
                    else if (message != null)
                    {
                        if (message.Number > 0)
                        {
                            from.SendLocalizedMessage(message.Number);
                        }
                        else
                        {
                            from.SendMessage(message.String);
                        }
                    }

                    return;
                }

                tool.UsesRemaining--;

                if (craftSystem is DefBlacksmithy &&
                    from.FindItemOnLayer(Layer.OneHanded) is AncientSmithyHammer hammer && hammer != tool)
                {
                    hammer.UsesRemaining--;
                    if (hammer.UsesRemaining < 1)
                    {
                        hammer.Delete();
                    }
                }

                if (tool.UsesRemaining < 1 && tool.BreakOnDepletion)
                {
                    toolBroken = true;
                }

                if (toolBroken)
                {
                    tool.Delete();
                }

                Item item;
                if (customCraft != null)
                {
                    item = customCraft.CompleteCraft(out num);
                }
                else if (typeof(MapItem).IsAssignableFrom(ItemType) && from.Map != Map.Trammel && from.Map != Map.Felucca)
                {
                    item = new IndecipherableMap();
                    from.SendLocalizedMessage(1070800); // The map you create becomes mysteriously indecipherable.
                }
                else
                {
                    item = ItemType.CreateInstance<Item>();
                }

                if (item != null)
                {
                    if (item is ICraftable craftable)
                    {
                        endquality = craftable.OnCraft(quality, makersMark, from, craftSystem, typeRes, tool, this, resHue);
                    }
                    else if (item.Hue == 0)
                    {
                        item.Hue = resHue;
                    }

                    if (maxAmount > 0)
                    {
                        if (!item.Stackable && item is IUsesRemaining remaining)
                        {
                            remaining.UsesRemaining *= maxAmount;
                        }
                        else
                        {
                            item.Amount = maxAmount;
                        }
                    }

                    from.AddToBackpack(item);

                    if (from.AccessLevel > AccessLevel.Player)
                    {
                        CommandLogging.WriteLine(
                            from,
                            "Crafting {0} with craft system {1}",
                            CommandLogging.Format(item),
                            craftSystem.GetType().Name
                        );
                    }

                    // from.PlaySound( 0x57 );
                }

                if (num == 0)
                {
                    num = craftSystem.PlayEndingEffect(from, false, true, toolBroken, endquality, makersMark, this);
                }

                var queryFactionImbue = false;
                var availableSilver = 0;
                FactionItemDefinition def = null;
                Faction faction = null;

                if (item is IFactionItem)
                {
                    def = FactionItemDefinition.Identify(item);

                    if (def != null)
                    {
                        faction = Faction.Find(from);

                        if (faction != null)
                        {
                            var town = Town.FromRegion(from.Region);

                            if (town?.Owner == faction)
                            {
                                var pack = from.Backpack;

                                if (pack != null)
                                {
                                    availableSilver = pack.GetAmount(typeof(Silver));

                                    if (availableSilver >= def.SilverCost)
                                    {
                                        queryFactionImbue = Faction.IsNearType(from, def.VendorType, 12);
                                    }
                                }
                            }
                        }
                    }
                }

                // TODO: Scroll imbuing

                if (queryFactionImbue)
                {
                    from.SendGump(
                        new FactionImbueGump(
                            quality,
                            item,
                            from,
                            craftSystem,
                            tool,
                            num,
                            availableSilver,
                            faction,
                            def
                        )
                    );
                }
                else if (tool?.Deleted == false && tool.UsesRemaining > 0)
                {
                    from.SendGump(new CraftGump(from, craftSystem, tool, num));
                }
                else if (num > 0)
                {
                    from.SendLocalizedMessage(num);
                }

                return;
            }

            if (!allRequiredSkills)
            {
                if (tool?.Deleted == false && tool.UsesRemaining > 0)
                {
                    from.SendGump(new CraftGump(from, craftSystem, tool, 1044153));
                }
                else
                {
                    from.SendLocalizedMessage(1044153); // You don't have the required skills to attempt this item.
                }

                return;
            }

            var consumeType = UseAllRes ? ConsumeType.Half : ConsumeType.All;

            // Not enough resource to craft it
            if (!ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, consumeType, ref message, true))
            {
                if (tool?.Deleted == false && tool.UsesRemaining > 0)
                {
                    from.SendGump(new CraftGump(from, craftSystem, tool, message));
                }
                else if (message != null)
                {
                    if (message.Number > 0)
                    {
                        from.SendLocalizedMessage(message.Number);
                    }
                    else
                    {
                        from.SendMessage(message.String);
                    }
                }

                return;
            }

            tool.UsesRemaining--;

            if (tool.UsesRemaining < 1 && tool.BreakOnDepletion)
            {
                toolBroken = true;
            }

            if (toolBroken)
            {
                tool.Delete();
            }

            // SkillCheck failed.
            num = craftSystem.PlayEndingEffect(from, true, true, toolBroken, endquality, false, this);

            if (!tool.Deleted && tool.UsesRemaining > 0)
            {
                from.SendGump(new CraftGump(from, craftSystem, tool, num));
            }
            else if (num > 0)
            {
                from.SendLocalizedMessage(num);
            }
        }

        private class InternalTimer : Timer
        {
            private readonly CraftItem m_CraftItem;
            private readonly CraftSystem m_CraftSystem;
            private readonly Mobile m_From;
            private readonly int m_iCountMax;
            private readonly BaseTool m_Tool;
            private readonly Type m_TypeRes;
            private int m_iCount;

            public InternalTimer(
                Mobile from, CraftSystem craftSystem, CraftItem craftItem, Type typeRes, BaseTool tool,
                int iCountMax
            ) : base(TimeSpan.Zero, TimeSpan.FromSeconds(craftSystem.Delay), iCountMax)
            {
                m_From = from;
                m_CraftItem = craftItem;
                m_iCount = 0;
                m_iCountMax = iCountMax;
                m_CraftSystem = craftSystem;
                m_TypeRes = typeRes;
                m_Tool = tool;
            }

            protected override void OnTick()
            {
                m_iCount++;

                m_From.DisruptiveAction();

                if (m_iCount < m_iCountMax)
                {
                    m_CraftSystem.PlayCraftEffect(m_From);
                    return;
                }

                m_From.EndAction<CraftSystem>();

                var badCraft = m_CraftSystem.CanCraft(m_From, m_Tool, m_CraftItem.ItemType);

                if (badCraft > 0)
                {
                    if (m_Tool?.Deleted == false && m_Tool.UsesRemaining > 0)
                    {
                        m_From.SendGump(new CraftGump(m_From, m_CraftSystem, m_Tool, badCraft));
                    }
                    else
                    {
                        m_From.SendLocalizedMessage(badCraft);
                    }

                    return;
                }

                var quality = 1;

                m_CraftItem.CheckSkills(m_From, m_TypeRes, m_CraftSystem, ref quality, out _, false);

                var context = m_CraftSystem.GetContext(m_From);

                if (context == null)
                {
                    return;
                }

                if (typeof(CustomCraft).IsAssignableFrom(m_CraftItem.ItemType))
                {
                    try
                    {
                        m_CraftItem.ItemType.CreateInstance<CustomCraft>(
                            m_From,
                            m_CraftItem,
                            m_CraftSystem,
                            m_TypeRes,
                            m_Tool,
                            quality
                        )?.EndCraftAction();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    return;
                }

                var makersMark = false;

                if (quality == 2 && m_From.Skills[m_CraftSystem.MainSkill].Base >= 100.0)
                {
                    makersMark = m_CraftItem.IsMarkable(m_CraftItem.ItemType);
                }

                if (makersMark && context.MarkOption == CraftMarkOption.PromptForMark)
                {
                    m_From.SendGump(
                        new QueryMakersMarkGump(
                            quality,
                            m_From,
                            m_CraftItem,
                            m_CraftSystem,
                            m_TypeRes,
                            m_Tool
                        )
                    );
                }
                else
                {
                    if (context.MarkOption == CraftMarkOption.DoNotMark)
                    {
                        makersMark = false;
                    }

                    m_CraftItem.CompleteCraft(quality, makersMark, m_From, m_CraftSystem, m_TypeRes, m_Tool, null);
                }
            }
        }
    }
}
