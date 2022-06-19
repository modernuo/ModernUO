using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Engines.Craft
{
    public enum CraftECA
    {
        ChanceMinusSixty,
        FiftyPercentChanceMinusTenPercent,
        ChanceMinusSixtyToFourtyFive
    }

    public abstract class CraftSystem
    {
        private readonly Dictionary<Mobile, CraftContext> m_ContextTable = new();
        private readonly List<int> m_RareRecipes;
        private readonly List<int> m_Recipes;

        public CraftSystem(int minCraftEffect, int maxCraftEffect, double delay)
        {
            MinCraftEffect = minCraftEffect;
            MaxCraftEffect = maxCraftEffect;
            Delay = delay;

            CraftItems = new List<CraftItem>();
            CraftGroups = new List<CraftGroup>();
            CraftSubRes = new CraftSubResCol();
            CraftSubRes2 = new CraftSubResCol();

            m_Recipes = new List<int>();
            m_RareRecipes = new List<int>();

            InitCraftList();
        }

        public int MinCraftEffect { get; }

        public int MaxCraftEffect { get; }

        public double Delay { get; }

        public List<CraftItem> CraftItems { get; }

        public List<CraftGroup> CraftGroups { get; }

        public CraftSubResCol CraftSubRes { get; }

        public CraftSubResCol CraftSubRes2 { get; }

        public abstract SkillName MainSkill { get; }

        public virtual TextDefinition GumpTitle => 0;

        public virtual CraftECA ECA => CraftECA.ChanceMinusSixty;

        public bool Resmelt { get; set; }

        public bool Repair { get; set; }

        public bool MarkOption { get; set; }

        public bool CanEnhance { get; set; }

        public abstract double GetChanceAtMin(CraftItem item);

        public virtual bool RetainsColorFrom(CraftItem item, Type type) => false;

        public CraftContext GetContext(Mobile m)
        {
            if (m == null)
            {
                return null;
            }

            if (m.Deleted)
            {
                m_ContextTable.Remove(m);
                return null;
            }

            if (!m_ContextTable.TryGetValue(m, out var c))
            {
                m_ContextTable[m] = c = new CraftContext();
            }

            return c;
        }

        public void OnMade(Mobile m, CraftItem item)
        {
            GetContext(m)?.OnMade(item);
        }

        public virtual bool ConsumeOnFailure(Mobile from, Type resourceType, CraftItem craftItem) => true;

        public void CreateItem(Mobile from, Type type, Type typeRes, BaseTool tool, CraftItem realCraftItem)
        {
            // Verify if the type is in the list of the craftable item
            if (CraftItems.SearchFor(type) != null)
            {
                realCraftItem.Craft(from, this, typeRes, tool);
            }
        }

        public int RandomRecipe()
        {
            if (m_Recipes.Count == 0)
            {
                return -1;
            }

            return m_Recipes.RandomElement();
        }

        public int RandomRareRecipe()
        {
            if (m_RareRecipes.Count == 0)
            {
                return -1;
            }

            return m_RareRecipes.RandomElement();
        }

        public int AddCraft(
            Type typeItem, TextDefinition group, TextDefinition name, double minSkill, double maxSkill,
            Type typeRes, TextDefinition nameRes, int amount
        ) =>
            AddCraft(typeItem, group, name, MainSkill, minSkill, maxSkill, typeRes, nameRes, amount, "");

        public int AddCraft(
            Type typeItem, TextDefinition group, TextDefinition name, double minSkill, double maxSkill,
            Type typeRes, TextDefinition nameRes, int amount, TextDefinition message
        ) =>
            AddCraft(typeItem, group, name, MainSkill, minSkill, maxSkill, typeRes, nameRes, amount, message);

        public int AddCraft(
            Type typeItem, TextDefinition group, TextDefinition name, SkillName skillToMake, double minSkill,
            double maxSkill, Type typeRes, TextDefinition nameRes, int amount
        ) =>
            AddCraft(typeItem, group, name, skillToMake, minSkill, maxSkill, typeRes, nameRes, amount, "");

        public int AddCraft(
            Type typeItem, TextDefinition group, TextDefinition name, SkillName skillToMake, double minSkill,
            double maxSkill, Type typeRes, TextDefinition nameRes, int amount, TextDefinition message
        )
        {
            var craftItem = new CraftItem(typeItem, group, name);
            craftItem.AddRes(typeRes, nameRes, amount, message);
            craftItem.AddSkill(skillToMake, minSkill, maxSkill);

            DoGroup(group, craftItem);
            CraftItems.Add(craftItem);
            return CraftItems.Count - 1;
        }

        private void DoGroup(TextDefinition groupName, CraftItem craftItem)
        {
            var index = CraftGroups.SearchFor(groupName);

            if (index == -1)
            {
                var craftGroup = new CraftGroup(groupName);
                craftGroup.AddCraftItem(craftItem);
                CraftGroups.Add(craftGroup);
            }
            else
            {
                CraftGroups[index].AddCraftItem(craftItem);
            }
        }

        public void SetItemHue(int index, int hue)
        {
            CraftItems[index].ItemHue = hue;
        }

        public void SetManaReq(int index, int mana)
        {
            CraftItems[index].Mana = mana;
        }

        public void SetStamReq(int index, int stam)
        {
            CraftItems[index].Stam = stam;
        }

        public void SetHitsReq(int index, int hits)
        {
            CraftItems[index].Hits = hits;
        }

        public void SetUseAllRes(int index, bool useAll)
        {
            CraftItems[index].UseAllRes = useAll;
        }

        public void SetNeedHeat(int index, bool needHeat)
        {
            CraftItems[index].NeedHeat = needHeat;
        }

        public void SetNeedOven(int index, bool needOven)
        {
            CraftItems[index].NeedOven = needOven;
        }

        public void SetBeverageType(int index, BeverageType requiredBeverage)
        {
            CraftItems[index].RequiredBeverage = requiredBeverage;
        }

        public void SetNeedMill(int index, bool needMill)
        {
            CraftItems[index].NeedMill = needMill;
        }

        public void SetNeededExpansion(int index, Expansion expansion)
        {
            CraftItems[index].RequiredExpansion = expansion;
        }

        public void AddRes(int index, Type type, TextDefinition name, int amount)
        {
            AddRes(index, type, name, amount, "");
        }

        public void AddRes(int index, Type type, TextDefinition name, int amount, TextDefinition message)
        {
            CraftItems[index].AddRes(type, name, amount, message);
        }

        public void AddSkill(int index, SkillName skillToMake, double minSkill, double maxSkill)
        {
            CraftItems[index].AddSkill(skillToMake, minSkill, maxSkill);
        }

        public void SetUseSubRes2(int index, bool val)
        {
            CraftItems[index].UseSubRes2 = val;
        }

        private void AddRecipeBase(int index, int id)
        {
            CraftItems[index].AddRecipe(id, this);
        }

        public void AddRecipe(int index, int id)
        {
            AddRecipeBase(index, id);
            m_Recipes.Add(id);
        }

        public void AddRareRecipe(int index, int id)
        {
            AddRecipeBase(index, id);
            m_RareRecipes.Add(id);
        }

        public void AddQuestRecipe(int index, int id)
        {
            AddRecipeBase(index, id);
        }

        public void ForceNonExceptional(int index)
        {
            CraftItems[index].ForceNonExceptional = true;
        }

        public void SetSubRes(Type type, TextDefinition name)
        {
            CraftSubRes.ResType = type;
            CraftSubRes.Name = name;
            CraftSubRes.Init = true;
        }

        public void AddSubRes(Type type, TextDefinition name, double reqSkill, int genericName, TextDefinition message)
        {
            var craftSubRes = new CraftSubRes(type, name, reqSkill, genericName, message);
            CraftSubRes.Add(craftSubRes);
        }

        public void AddSubRes(Type type, TextDefinition name, double reqSkill, TextDefinition message)
        {
            var craftSubRes = new CraftSubRes(type, name, reqSkill, message);
            CraftSubRes.Add(craftSubRes);
        }

        public void SetSubRes2(Type type, TextDefinition name)
        {
            CraftSubRes2.ResType = type;
            CraftSubRes2.Name = name;
            CraftSubRes2.Init = true;
        }

        public void AddSubRes2(Type type, TextDefinition name, double reqSkill, int genericName, TextDefinition message)
        {
            var craftSubRes = new CraftSubRes(type, name, reqSkill, genericName, message);
            CraftSubRes2.Add(craftSubRes);
        }

        public void AddSubRes2(Type type, TextDefinition name, double reqSkill, TextDefinition message)
        {
            var craftSubRes = new CraftSubRes(type, name, reqSkill, message);
            CraftSubRes2.Add(craftSubRes);
        }

        public abstract void InitCraftList();

        public abstract void PlayCraftEffect(Mobile from);

        public abstract int PlayEndingEffect(
            Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality,
            bool makersMark, CraftItem item
        );

        public abstract int CanCraft(Mobile from, BaseTool tool, Type itemType);
    }
}
