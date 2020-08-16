using System;
using System.Collections.Generic;
using System.Linq;
using Server.Commands;
using Server.Factions;
using Server.Items;
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
    int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
      CraftItem craftItem, int resHue);
  }

  public class CraftItem
  {
    private static readonly Dictionary<Type, int> _itemIds = new Dictionary<Type, int>();
    private int m_ResAmount;

    private int m_ResHue;
    private CraftSystem m_System;

    public CraftItem(Type type, TextDefinition groupName, TextDefinition name)
    {
      Resources = new List<CraftRes>();
      Skills = new List<CraftSkill>();

      ItemType = type;

      GroupNameString = groupName;
      NameString = name;

      GroupNameNumber = groupName;
      NameNumber = name;

      RequiredBeverage = BeverageType.Water;
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

    public List<CraftRes> Resources { get; }

    public List<CraftSkill> Skills { get; }

    public void AddRecipe(int id, CraftSystem system)
    {
      if (Recipe != null)
      {
        Console.WriteLine("Warning: Attempted add of recipe #{0} to the crafting of {1} in CraftSystem {2}.", id,
          ItemType.Name, system);
        return;
      }

      Recipe = new Recipe(id, system, this);
    }

    public static int LabelNumber(Type type)
    {
      int number = ItemIDOf(type);

      if (number >= 0x4000)
        number += 1078872;
      else
        number += 1020000;

      return number;
    }

    public static int ItemIDOf(Type type)
    {
      if (_itemIds.TryGetValue(type, out int itemId))
        return itemId;

      if (type == typeof(FactionExplosionTrap))
        itemId = 14034;
      else if (type == typeof(FactionGasTrap))
        itemId = 4523;
      else if (type == typeof(FactionSawTrap))
        itemId = 4359;
      else if (type == typeof(FactionSpikeTrap)) itemId = 4517;

      if (itemId == 0)
      {
        object[] attrs = type.GetCustomAttributes(typeof(CraftItemIDAttribute), false);

        if (attrs.Length > 0)
        {
          CraftItemIDAttribute craftItemID = (CraftItemIDAttribute)attrs[0];
          itemId = craftItemID.ItemID;
        }
      }

      if (itemId == 0)
      {
        Item item = null;

        try
        {
          item = ActivatorUtil.CreateInstance(type) as Item;
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

    public void AddRes(Type type, TextDefinition name, int amount)
    {
      AddRes(type, name, amount, "");
    }

    public void AddRes(Type type, TextDefinition name, int amount, TextDefinition message)
    {
      CraftRes craftRes = new CraftRes(type, name, amount, message);
      Resources.Add(craftRes);
    }

    public void AddSkill(SkillName skillToMake, double minSkill, double maxSkill)
    {
      CraftSkill craftSkill = new CraftSkill(skillToMake, minSkill, maxSkill);
      Skills.Add(craftSkill);
    }

    public bool ConsumeAttributes(Mobile from, ref object message, bool consume)
    {
      bool consumMana;
      bool consumHits;
      bool consumStam;

      if (Hits > 0 && from.Hits < Hits)
      {
        message = "You lack the required hit points to make that.";
        return false;
      }

      consumHits = consume;

      if (Mana > 0 && from.Mana < Mana)
      {
        message = "You lack the required mana to make that.";
        return false;
      }

      consumMana = consume;

      if (Stam > 0 && from.Stam < Stam)
      {
        message = "You lack the required stamina to make that.";
        return false;
      }

      consumStam = consume;

      if (consumMana)
        from.Mana -= Mana;

      if (consumHits)
        from.Hits -= Hits;

      if (consumStam)
        from.Stam -= Stam;

      return true;
    }

    public bool IsMarkable(Type type)
    {
      if (ForceNonExceptional) // Don't even display the stuff for marking if it can't ever be exceptional.
        return false;

      for (int i = 0; i < m_MarkableTable.Length; ++i)
        if (type == m_MarkableTable[i] || type.IsSubclassOf(m_MarkableTable[i]))
          return true;

      return false;
    }

    public static bool RetainsColor(Type type)
    {
      bool neverColor = false;

      for (int i = 0; !neverColor && i < m_NeverColorTable.Length; ++i)
        neverColor = type == m_NeverColorTable[i] || type.IsSubclassOf(m_NeverColorTable[i]);

      if (neverColor)
        return false;

      bool inItemTable = false;

      for (int i = 0; !inItemTable && i < m_ColoredItemTable.Length; ++i)
        inItemTable = type == m_ColoredItemTable[i] || type.IsSubclassOf(m_ColoredItemTable[i]);

      return inItemTable;
    }

    public bool RetainsColorFrom(CraftSystem system, Type type)
    {
      if (system.RetainsColorFrom(this, type))
        return true;

      bool inItemTable = RetainsColor(ItemType);

      if (!inItemTable)
        return false;

      bool inResourceTable = false;

      for (int i = 0; !inResourceTable && i < m_ColoredResourceTable.Length; ++i)
        inResourceTable = type == m_ColoredResourceTable[i] || type.IsSubclassOf(m_ColoredResourceTable[i]);

      return inResourceTable;
    }

    public bool Find(Mobile from, int[] itemIDs)
    {
      Map map = from.Map;

      if (map == null)
        return false;

      IPooledEnumerable<Item> eable = map.GetItemsInRange(from.Location, 2);
      bool found = eable.Any(item => item.Z + 16 > item.Z && item.Z + 16 > item.Z && Find(item.ItemID, itemIDs));
      eable.Free();

      if (found)
        return true;

      for (int x = -2; x <= 2; ++x)
        for (int y = -2; y <= 2; ++y)
        {
          int vx = from.X + x;
          int vy = from.Y + y;

          StaticTile[] tiles = map.Tiles.GetStaticTiles(vx, vy, true);

          for (int i = 0; i < tiles.Length; ++i)
          {
            int z = tiles[i].Z;
            int id = tiles[i].ID;

            if (z + 16 > from.Z && from.Z + 16 > z && Find(id, itemIDs))
              return true;
          }
        }

      return false;
    }

    public static bool Find(int itemID, int[] itemIDs)
    {
      bool contains = false;

      for (int i = 0; !contains && i < itemIDs.Length; i += 2)
        contains = itemID >= itemIDs[i] && itemID <= itemIDs[i + 1];

      return contains;
    }

    public bool IsQuantityType(Type[][] types) =>
      types.Any(check => check.Any(t => typeof(IHasQuantity).IsAssignableFrom(t)));

    public int ConsumeQuantity(Container cont, Type[][] types, int[] amounts)
    {
      if (types.Length != amounts.Length)
        throw new ArgumentException();

      Item[][] items = new Item[types.Length][];
      int[] totals = new int[types.Length];

      for (int i = 0; i < types.Length; ++i)
      {
        items[i] = cont.FindItemsByType(types[i]);

        for (int j = 0; j < items[i].Length; ++j)
          if (!(items[i][j] is IHasQuantity hq))
          {
            totals[i] += items[i][j].Amount;
          }
          else
          {
            if (hq is BaseBeverage beverage && beverage.Content != RequiredBeverage)
              continue;

            totals[i] += hq.Quantity;
          }

        if (totals[i] < amounts[i])
          return i;
      }

      for (int i = 0; i < types.Length; ++i)
      {
        int need = amounts[i];

        for (int j = 0; j < items[i].Length; ++j)
        {
          Item item = items[i][j];

          if (!(item is IHasQuantity hq))
          {
            int theirAmount = item.Amount;

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
              continue;

            int theirAmount = hq.Quantity;

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
      Item[] items = cont.FindItemsByType(types);

      int amount = 0;

      for (int i = 0; i < items.Length; ++i)
        if (!(items[i] is IHasQuantity hq))
        {
          amount += items[i].Amount;
        }
        else
        {
          if (hq is BaseBeverage beverage && beverage.Content != RequiredBeverage)
            continue;

          amount += hq.Quantity;
        }

      return amount;
    }

    public bool ConsumeRes(Mobile from, Type typeRes, CraftSystem craftSystem, ref int resHue, ref int maxAmount,
      ConsumeType consumeType, ref object message) =>
      ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, consumeType, ref message, false);

    public bool ConsumeRes(Mobile from, Type typeRes, CraftSystem craftSystem, ref int resHue, ref int maxAmount,
      ConsumeType consumeType, ref object message, bool isFailure)
    {
      Container ourPack = from.Backpack;

      if (ourPack == null)
        return false;

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

      Type[][] types = new Type[Resources.Count][];
      int[] amounts = new int[Resources.Count];

      maxAmount = int.MaxValue;

      CraftSubResCol resCol = UseSubRes2 ? craftSystem.CraftSubRes2 : craftSystem.CraftSubRes;

      CraftRes res;
      for (int i = 0; i < types.Length; ++i)
      {
        CraftRes craftRes = Resources[i];
        Type baseType = craftRes.ItemType;

        // Resource Mutation
        if (baseType == resCol.ResType && typeRes != null)
        {
          baseType = typeRes;

          CraftSubRes subResource = resCol.SearchFor(baseType);

          if (subResource != null && from.Skills[craftSystem.MainSkill].Base < subResource.RequiredSkill)
          {
            message = subResource.Message;
            return false;
          }
        }
        // ******************

        for (int j = 0; types[i] == null && j < m_TypesTable.Length; ++j)
          if (m_TypesTable[j][0] == baseType)
            types[i] = m_TypesTable[j];

        if (types[i] == null)
          types[i] = new[] { baseType };

        amounts[i] = craftRes.Amount;

        // For stackable items that can ben crafted more than one at a time
        if (UseAllRes)
        {
          int tempAmount = ourPack.GetAmount(types[i]);
          tempAmount /= amounts[i];
          if (tempAmount < maxAmount)
          {
            maxAmount = tempAmount;

            if (maxAmount == 0)
            {
              res = Resources[i];

              if (res.MessageNumber > 0)
                message = res.MessageNumber;
              else if (!string.IsNullOrEmpty(res.MessageString))
                message = res.MessageString;
              else
                message = 502925; // You don't have the resources required to make that item.

              return false;
            }
          }
        }
        // ****************************

        if (isFailure && !craftSystem.ConsumeOnFailure(from, types[i][0], this))
          amounts[i] = 0;
      }

      // We adjust the amount of each resource to consume the max possible
      if (UseAllRes)
        for (int i = 0; i < amounts.Length; ++i)
          amounts[i] *= maxAmount;
      else
        maxAmount = -1;

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

      // Consume ALL
      if (consumeType == ConsumeType.All)
      {
        m_ResHue = 0;
        m_ResAmount = 0;
        m_System = craftSystem;

        if (IsQuantityType(types))
          index = ConsumeQuantity(ourPack, types, amounts);
        else
          index = ourPack.ConsumeTotalGrouped(types, amounts, true, OnResourceConsumed, CheckHueGrouping);

        resHue = m_ResHue;
      }
      // Consume Half ( for use all resource craft type )
      else if (consumeType == ConsumeType.Half)
      {
        for (int i = 0; i < amounts.Length; i++)
        {
          amounts[i] /= 2;

          if (amounts[i] < 1)
            amounts[i] = 1;
        }

        m_ResHue = 0;
        m_ResAmount = 0;
        m_System = craftSystem;

        if (IsQuantityType(types))
          index = ConsumeQuantity(ourPack, types, amounts);
        else
          index = ourPack.ConsumeTotalGrouped(types, amounts, true, OnResourceConsumed, CheckHueGrouping);

        resHue = m_ResHue;
      }
      else // ConstumeType.None ( it's basically used to know if the crafter has enough resource before starting the process )
      {
        index = -1;

        // TODO: Optimize this
        if (IsQuantityType(types))
          for (int i = 0; i < types.Length; i++)
            if (GetQuantity(ourPack, types[i]) < amounts[i])
            {
              index = i;
              break;
            }
            else
            {
              for (int j = 0; j < types.Length; j++)
                if (ourPack.GetBestGroupAmount(types[j], true, CheckHueGrouping) < amounts[j])
                {
                  index = j;
                  break;
                }
            }
      }

      if (index == -1)
      {
        if (consumeType != ConsumeType.None)
          consumeExtra?.Delete();

        return true;
      }

      res = Resources[index];

      if (res.MessageNumber > 0)
        message = res.MessageNumber;
      else if (!string.IsNullOrEmpty(res.MessageString))
        message = res.MessageString;
      else
        message = 502925; // You don't have the resources required to make that item.

      return false;
    }

    private void OnResourceConsumed(Item item, int amount)
    {
      if (!RetainsColorFrom(m_System, item.GetType()))
        return;

      if (amount >= m_ResAmount)
      {
        m_ResHue = item.Hue;
        m_ResAmount = amount;
      }
    }

    private int CheckHueGrouping(Item a, Item b) => b.Hue.CompareTo(a.Hue);

    public double GetExceptionalChance(CraftSystem system, double chance, Mobile from)
    {
      if (ForceNonExceptional)
        return 0.0;

      double bonus = 0.0;

      if (from.Talisman is BaseTalisman talisman && talisman.Skill == system.MainSkill)
      {
        chance -= talisman.SuccessBonus / 100.0;
        bonus = talisman.ExceptionalBonus / 100.0;
      }

      switch (system.ECA)
      {
        default:
          chance -= 0.6;
          break;
        case CraftECA.FiftyPercentChanceMinusTenPercent:
          chance = chance * 0.5 - 0.1;
          break;
        case CraftECA.ChanceMinusSixtyToFourtyFive:
          chance -= Math.Clamp(0.60 - (from.Skills[system.MainSkill].Value - 95.0) * 0.03, 0.45, 0.60);
          break;
      }

      return chance > 0 ? chance + bonus : chance;
    }

    public bool CheckSkills(Mobile from, Type typeRes, CraftSystem craftSystem, ref int quality,
      ref bool allRequiredSkills) =>
      CheckSkills(from, typeRes, craftSystem, ref quality, out allRequiredSkills, true);

    public bool CheckSkills(Mobile from, Type typeRes, CraftSystem craftSystem, ref int quality,
      out bool allRequiredSkills, bool gainSkills)
    {
      double chance = GetSuccessChance(from, typeRes, craftSystem, gainSkills, out allRequiredSkills);

      if (GetExceptionalChance(craftSystem, chance, from) > Utility.RandomDouble())
        quality = 2;

      return chance > Utility.RandomDouble();
    }

    public double GetSuccessChance(Mobile from, Type typeRes, CraftSystem craftSystem, bool gainSkills,
      out bool allRequiredSkills)
    {
      double minMainSkill = 0.0;
      double maxMainSkill = 0.0;
      double valMainSkill = 0.0;

      allRequiredSkills = true;

      for (int i = 0; i < Skills.Count; i++)
      {
        CraftSkill craftSkill = Skills[i];

        double minSkill = craftSkill.MinSkill;
        double maxSkill = craftSkill.MaxSkill;
        double valSkill = from.Skills[craftSkill.SkillToMake].Value;

        if (valSkill < minSkill)
          allRequiredSkills = false;

        if (craftSkill.SkillToMake == craftSystem.MainSkill)
        {
          minMainSkill = minSkill;
          maxMainSkill = maxSkill;
          valMainSkill = valSkill;
        }

        if (gainSkills) // This is a passive check. Success chance is entirely dependant on the main skill
          from.CheckSkill(craftSkill.SkillToMake, minSkill, maxSkill);
      }

      double chance;

      if (allRequiredSkills)
        chance = craftSystem.GetChanceAtMin(this) + (valMainSkill - minMainSkill) / (maxMainSkill - minMainSkill) *
          (1.0 - craftSystem.GetChanceAtMin(this));
      else
        chance = 0.0;

      if (allRequiredSkills && from.Talisman is BaseTalisman talisman && talisman.Skill == craftSystem.MainSkill)
        chance += talisman.SuccessBonus / 100.0;

      if (allRequiredSkills && valMainSkill == maxMainSkill)
        chance = 1.0;

      return chance;
    }

    public void Craft(Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool)
    {
      if (from.BeginAction<CraftSystem>())
      {
        if (RequiredExpansion == Expansion.None ||
            from.NetState?.SupportsExpansion(RequiredExpansion) == true)
        {
          bool allRequiredSkills;
          double chance = GetSuccessChance(from, typeRes, craftSystem, false, out allRequiredSkills);

          if (allRequiredSkills && chance >= 0.0)
          {
            if (Recipe == null || (from as PlayerMobile)?.HasRecipe(Recipe) != false)
            {
              int badCraft = craftSystem.CanCraft(from, tool, ItemType);

              if (badCraft <= 0)
              {
                int resHue = 0;
                int maxAmount = 0;
                object message = null;

                if (ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, ConsumeType.None,
                  ref message))
                {
                  message = null;

                  if (ConsumeAttributes(from, ref message, false))
                  {
                    CraftContext context = craftSystem.GetContext(from);

                    context?.OnMade(this);

                    int iMin = craftSystem.MinCraftEffect;
                    int iMax = craftSystem.MaxCraftEffect - iMin + 1;
                    int iRandom = Utility.Random(iMax);
                    iRandom += iMin + 1;
                    new InternalTimer(from, craftSystem, this, typeRes, tool, iRandom).Start();
                  }
                  else
                  {
                    from.EndAction<CraftSystem>();
                    from.SendGump(new CraftGump(from, craftSystem, tool, message));
                  }
                }
                else
                {
                  from.EndAction<CraftSystem>();
                  from.SendGump(new CraftGump(from, craftSystem, tool, message));
                }
              }
              else
              {
                from.EndAction<CraftSystem>();
                from.SendGump(new CraftGump(from, craftSystem, tool, badCraft));
              }
            }
            else
            {
              from.EndAction<CraftSystem>();
              from.SendGump(new CraftGump(from, craftSystem, tool,
                1072847)); // You must learn that recipe from a scroll.
            }
          }
          else
          {
            from.EndAction<CraftSystem>();
            from.SendGump(new CraftGump(from, craftSystem, tool,
              1044153)); // You don't have the required skills to attempt this item.
          }
        }
        else
        {
          from.EndAction<CraftSystem>();
          from.SendGump(new CraftGump(from, craftSystem, tool,
            RequiredExpansionMessage(RequiredExpansion))); // The {0} expansion is required to attempt this item.
        }
      }
      else
      {
        from.SendLocalizedMessage(500119); // You must wait to perform another action
      }
    }

    // Eventually convert to TextDefinition, but that requires that we convert all the gumps to ues it too.  Not that it wouldn't be a bad idea.
    private object RequiredExpansionMessage(Expansion expansion)
    {
      return expansion switch
      {
        Expansion.SE => 1063307, // The "Samurai Empire" expansion is required to attempt this item.
        Expansion.ML => 1072650, // The "Mondain's Legacy" expansion is required to attempt this item.
        _ => $"The \"{ExpansionInfo.GetInfo(expansion).Name}\" expansion is required to attempt this item."
      };
    }

    public void CompleteCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes,
      BaseTool tool, CustomCraft customCraft)
    {
      int badCraft = craftSystem.CanCraft(from, tool, ItemType);

      if (badCraft > 0)
      {
        if (tool?.Deleted == false && tool.UsesRemaining > 0)
          from.SendGump(new CraftGump(from, craftSystem, tool, badCraft));
        else
          from.SendLocalizedMessage(badCraft);

        return;
      }

      int checkResHue = 0, checkMaxAmount = 0;
      object checkMessage = null;

      // Not enough resource to craft it
      if (!(ConsumeRes(from, typeRes, craftSystem, ref checkResHue, ref checkMaxAmount, ConsumeType.None,
              ref checkMessage)
            && ConsumeAttributes(from, ref checkMessage, false)))
      {
        if (tool?.Deleted == false && tool.UsesRemaining > 0)
          from.SendGump(new CraftGump(from, craftSystem, tool, checkMessage));
        else if (checkMessage is int messageInt && messageInt > 0)
          from.SendLocalizedMessage(messageInt);
        else
          from.SendMessage(checkMessage.ToString());

        return;
      }

      bool toolBroken = false;

      int ignored = 1;
      int endquality = 1;

      bool allRequiredSkills = true;

      if (CheckSkills(from, typeRes, craftSystem, ref ignored, ref allRequiredSkills))
      {
        // Resource
        int resHue = 0;
        int maxAmount = 0;

        object message = null;

        // Not enough resource to craft it
        if (!(ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, ConsumeType.All, ref message)
              && ConsumeAttributes(from, ref message, true)))
        {
          if (tool?.Deleted == false && tool.UsesRemaining > 0)
            from.SendGump(new CraftGump(from, craftSystem, tool, message));
          else if (message is int messageIn && messageIn > 0)
            from.SendLocalizedMessage(messageIn);
          else
            from.SendMessage(message.ToString());

          return;
        }

        tool.UsesRemaining--;

        if (craftSystem is DefBlacksmithy)
          if (from.FindItemOnLayer(Layer.OneHanded) is AncientSmithyHammer hammer && hammer != tool)
          {
            hammer.UsesRemaining--;
            if (hammer.UsesRemaining < 1)
              hammer.Delete();
          }

        if (tool.UsesRemaining < 1 && tool.BreakOnDepletion)
          toolBroken = true;

        if (toolBroken)
          tool.Delete();

        int num = 0;

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
          item = ActivatorUtil.CreateInstance(ItemType) as Item;
        }

        if (item != null)
        {
          if (item is ICraftable craftable)
            endquality = craftable.OnCraft(quality, makersMark, from, craftSystem, typeRes, tool, this, resHue);
          else if (item.Hue == 0)
            item.Hue = resHue;

          if (maxAmount > 0)
          {
            if (!item.Stackable && item is IUsesRemaining remaining)
              remaining.UsesRemaining *= maxAmount;
            else
              item.Amount = maxAmount;
          }

          from.AddToBackpack(item);

          if (from.AccessLevel > AccessLevel.Player)
            CommandLogging.WriteLine(from, "Crafting {0} with craft system {1}", CommandLogging.Format(item),
              craftSystem.GetType().Name);

          // from.PlaySound( 0x57 );
        }

        if (num == 0)
          num = craftSystem.PlayEndingEffect(from, false, true, toolBroken, endquality, makersMark, this);

        bool queryFactionImbue = false;
        int availableSilver = 0;
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
              Town town = Town.FromRegion(from.Region);

              if (town?.Owner == faction)
              {
                Container pack = from.Backpack;

                if (pack != null)
                {
                  availableSilver = pack.GetAmount(typeof(Silver));

                  if (availableSilver >= def.SilverCost)
                    queryFactionImbue = Faction.IsNearType(from, def.VendorType, 12);
                }
              }
            }
          }
        }

        // TODO: Scroll imbuing

        if (queryFactionImbue)
          from.SendGump(new FactionImbueGump(quality, item, from, craftSystem, tool, num, availableSilver, faction,
            def));
        else if (tool?.Deleted == false && tool.UsesRemaining > 0)
          from.SendGump(new CraftGump(from, craftSystem, tool, num));
        else if (num > 0)
          from.SendLocalizedMessage(num);
      }
      else if (!allRequiredSkills)
      {
        if (tool?.Deleted == false && tool.UsesRemaining > 0)
          from.SendGump(new CraftGump(from, craftSystem, tool, 1044153));
        else
          from.SendLocalizedMessage(1044153); // You don't have the required skills to attempt this item.
      }
      else
      {
        ConsumeType consumeType = UseAllRes ? ConsumeType.Half : ConsumeType.All;
        int resHue = 0;
        int maxAmount = 0;

        object message = null;

        // Not enough resource to craft it
        if (!ConsumeRes(from, typeRes, craftSystem, ref resHue, ref maxAmount, consumeType, ref message, true))
        {
          if (tool?.Deleted == false && tool.UsesRemaining > 0)
            from.SendGump(new CraftGump(from, craftSystem, tool, message));
          else if (message is int messageInt && messageInt > 0)
            from.SendLocalizedMessage(messageInt);
          else
            from.SendMessage(message.ToString());

          return;
        }

        tool.UsesRemaining--;

        if (tool.UsesRemaining < 1 && tool.BreakOnDepletion)
          toolBroken = true;

        if (toolBroken)
          tool.Delete();

        // SkillCheck failed.
        int num = craftSystem.PlayEndingEffect(from, true, true, toolBroken, endquality, false, this);

        if (!tool.Deleted && tool.UsesRemaining > 0)
          from.SendGump(new CraftGump(from, craftSystem, tool, num));
        else if (num > 0)
          from.SendLocalizedMessage(num);
      }
    }

    private class InternalTimer : Timer
    {
      private readonly CraftItem m_CraftItem;
      private readonly CraftSystem m_CraftSystem;
      private readonly Mobile m_From;
      private int m_iCount;
      private readonly int m_iCountMax;
      private readonly BaseTool m_Tool;
      private readonly Type m_TypeRes;

      public InternalTimer(Mobile from, CraftSystem craftSystem, CraftItem craftItem, Type typeRes, BaseTool tool,
        int iCountMax) : base(TimeSpan.Zero, TimeSpan.FromSeconds(craftSystem.Delay), iCountMax)
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
        }
        else
        {
          m_From.EndAction<CraftSystem>();

          int badCraft = m_CraftSystem.CanCraft(m_From, m_Tool, m_CraftItem.ItemType);

          if (badCraft > 0)
          {
            if (m_Tool?.Deleted == false && m_Tool.UsesRemaining > 0)
              m_From.SendGump(new CraftGump(m_From, m_CraftSystem, m_Tool, badCraft));
            else
              m_From.SendLocalizedMessage(badCraft);

            return;
          }

          int quality = 1;
          bool allRequiredSkills = true;

          m_CraftItem.CheckSkills(m_From, m_TypeRes, m_CraftSystem, ref quality, out allRequiredSkills, false);

          CraftContext context = m_CraftSystem.GetContext(m_From);

          if (context == null)
            return;

          if (typeof(CustomCraft).IsAssignableFrom(m_CraftItem.ItemType))
          {
            CustomCraft cc = null;

            try
            {
              cc = ActivatorUtil.CreateInstance(m_CraftItem.ItemType, m_From, m_CraftItem, m_CraftSystem,
                m_TypeRes, m_Tool, quality) as CustomCraft;
            }
            catch
            {
              // ignored
            }

            cc?.EndCraftAction();

            return;
          }

          bool makersMark = false;

          if (quality == 2 && m_From.Skills[m_CraftSystem.MainSkill].Base >= 100.0)
            makersMark = m_CraftItem.IsMarkable(m_CraftItem.ItemType);

          if (makersMark && context.MarkOption == CraftMarkOption.PromptForMark)
          {
            m_From.SendGump(new QueryMakersMarkGump(quality, m_From, m_CraftItem, m_CraftSystem, m_TypeRes,
              m_Tool));
          }
          else
          {
            if (context.MarkOption == CraftMarkOption.DoNotMark)
              makersMark = false;

            m_CraftItem.CompleteCraft(quality, makersMark, m_From, m_CraftSystem, m_TypeRes, m_Tool, null);
          }
        }
      }
    }

    private static readonly int[] m_HeatSources =
    {
      0x461, 0x48E, // Sandstone oven/fireplace
      0x92B, 0x96C, // Stone oven/fireplace
      0xDE3, 0xDE9, // Campfire
      0xFAC, 0xFAC, // Firepit
      0x184A, 0x184C, // Heating stand (left)
      0x184E, 0x1850, // Heating stand (right)
      0x398C, 0x399F, // Fire field
      0x2DDB, 0x2DDC, // Elven stove
      0x19AA, 0x19BB, // Veteran Reward Brazier
      0x197A, 0x19A9, // Large Forge
      0x0FB1, 0x0FB1, // Small Forge
      0x2DD8, 0x2DD8 // Elven Forge
    };

    private static readonly int[] m_Ovens =
    {
      0x461, 0x46F, // Sandstone oven
      0x92B, 0x93F, // Stone oven
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
  }
}
