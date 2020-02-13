using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Server.Items;
using Server.Mobiles;
using Server.Utilities;

namespace Server.Regions
{
  public abstract class SpawnDefinition
  {
    public abstract ISpawnable Spawn(SpawnEntry entry);

    public abstract bool CanSpawn(params Type[] types);

    public static SpawnDefinition GetSpawnDefinition(XmlElement xml)
    {
      switch (xml.Name)
      {
        case "object":
        {
          Type type = null;
          if (!Region.ReadType(xml, "type", ref type))
            return null;

          if (typeof(Mobile).IsAssignableFrom(type)) return SpawnMobile.Get(type);

          if (typeof(Item).IsAssignableFrom(type)) return SpawnItem.Get(type);
          Console.WriteLine("Invalid type '{0}' in a SpawnDefinition", type.FullName);
          return null;
        }
        case "group":
        {
          string group = null;
          if (!Region.ReadString(xml, "name", ref group))
            return null;

          if (!SpawnGroup.Table.TryGetValue(group, out SpawnGroup def))
          {
            Console.WriteLine("Could not find group '{0}' in a SpawnDefinition", group);
            return null;
          }

          return def;
        }
        case "treasureChest":
        {
          int itemID = 0xE43;
          Region.ReadInt32(xml, "itemID", ref itemID, false);

          BaseTreasureChest.TreasureLevel level = BaseTreasureChest.TreasureLevel.Level2;

          Region.ReadEnum(xml, "level", ref level, false);

          return new SpawnTreasureChest(itemID, level);
        }
        default:
        {
          return null;
        }
      }
    }
  }

  public abstract class SpawnType : SpawnDefinition
  {
    private bool m_Init;

    protected SpawnType(Type type)
    {
      Type = type;
      m_Init = false;
    }

    public Type Type{ get; }

    public abstract int Height{ get; }
    public abstract bool Land{ get; }
    public abstract bool Water{ get; }

    protected void EnsureInit()
    {
      if (m_Init)
        return;

      Init();
      m_Init = true;
    }

    protected virtual void Init()
    {
    }

    public override ISpawnable Spawn(SpawnEntry entry)
    {
      Region region = entry.Region;
      Map map = region.Map;

      Point3D loc = entry.RandomSpawnLocation(Height, Land, Water);

      if (loc == Point3D.Zero)
        return null;

      return Construct(entry, loc, map);
    }

    protected abstract ISpawnable Construct(SpawnEntry entry, Point3D loc, Map map);

    public override bool CanSpawn(params Type[] types)
    {
      for (int i = 0; i < types.Length; i++)
        if (types[i] == Type)
          return true;

      return false;
    }
  }

  public class SpawnMobile : SpawnType
  {
    private static Dictionary<Type, SpawnMobile> m_Table = new Dictionary<Type, SpawnMobile>();

    private bool m_Land;
    private bool m_Water;

    public SpawnMobile(Type type) : base(type)
    {
    }

    public override int Height => 16;

    public override bool Land
    {
      get
      {
        EnsureInit();
        return m_Land;
      }
    }

    public override bool Water
    {
      get
      {
        EnsureInit();
        return m_Water;
      }
    }

    public static SpawnMobile Get(Type type)
    {
      if (!m_Table.TryGetValue(type, out SpawnMobile sm))
        m_Table[type] = sm = new SpawnMobile(type);

      return sm;
    }

    protected override void Init()
    {
      Mobile mob = (Mobile)ActivatorUtil.CreateInstance(Type);

      m_Land = !mob.CantWalk;
      m_Water = mob.CanSwim;

      mob.Delete();
    }

    protected override ISpawnable Construct(SpawnEntry entry, Point3D loc, Map map)
    {
      Mobile mobile = CreateMobile();

      if (mobile is BaseCreature creature)
      {
        creature.Home = entry.HomeLocation;
        creature.HomeMap = map;
        creature.RangeHome = entry.HomeRange;
      }

      if (entry.Direction != SpawnEntry.InvalidDirection)
        mobile.Direction = entry.Direction;

      mobile.OnBeforeSpawn(loc, map);
      mobile.MoveToWorld(loc, map);
      mobile.OnAfterSpawn();

      return mobile;
    }

    protected virtual Mobile CreateMobile() => (Mobile)ActivatorUtil.CreateInstance(Type);
  }

  public class SpawnItem : SpawnType
  {
    private static Dictionary<Type, SpawnItem> m_Table = new Dictionary<Type, SpawnItem>();

    protected int m_Height;

    protected SpawnItem(Type type) : base(type)
    {
    }

    public override int Height
    {
      get
      {
        EnsureInit();
        return m_Height;
      }
    }

    public override bool Land => true;
    public override bool Water => false;

    public static SpawnItem Get(Type type)
    {
      if (!m_Table.TryGetValue(type, out SpawnItem si))
        m_Table[type] = si = new SpawnItem(type);

      return si;
    }

    protected override void Init()
    {
      Item item = (Item)ActivatorUtil.CreateInstance(Type);

      m_Height = item.ItemData.Height;

      item.Delete();
    }

    protected override ISpawnable Construct(SpawnEntry entry, Point3D loc, Map map)
    {
      Item item = CreateItem();

      item.OnBeforeSpawn(loc, map);
      item.MoveToWorld(loc, map);
      item.OnAfterSpawn();

      return item;
    }

    protected virtual Item CreateItem() => (Item)ActivatorUtil.CreateInstance(Type);
  }

  public class SpawnTreasureChest : SpawnItem
  {
    public SpawnTreasureChest(int itemID, BaseTreasureChest.TreasureLevel level) : base(typeof(BaseTreasureChest))
    {
      ItemID = itemID;
      Level = level;
    }

    public int ItemID{ get; }

    public BaseTreasureChest.TreasureLevel Level{ get; }

    protected override void Init()
    {
      m_Height = TileData.ItemTable[ItemID & TileData.MaxItemValue].Height;
    }

    protected override Item CreateItem() => new BaseTreasureChest(ItemID, Level);
  }

  public class SpawnGroupElement
  {
    public SpawnGroupElement(SpawnDefinition spawnDefinition, int weight)
    {
      SpawnDefinition = spawnDefinition;
      Weight = weight;
    }

    public SpawnDefinition SpawnDefinition{ get; }

    public int Weight{ get; }
  }

  public class SpawnGroup : SpawnDefinition
  {
    private int m_TotalWeight;

    static SpawnGroup()
    {
      string path = Path.Combine(Core.BaseDirectory, "Data/SpawnDefinitions.xml");
      if (!File.Exists(path))
        return;

      try
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(path);

        XmlElement root = doc["spawnDefinitions"];
        if (root == null)
          return;

        foreach (XmlElement xmlDef in root.SelectNodes("spawnGroup"))
        {
          string name = null;
          if (!Region.ReadString(xmlDef, "name", ref name))
            continue;

          List<SpawnGroupElement> list = new List<SpawnGroupElement>();
          foreach (XmlNode node in xmlDef.ChildNodes)
            if (node is XmlElement el)
            {
              SpawnDefinition def = GetSpawnDefinition(el);
              if (def == null)
                continue;

              int weight = 1;
              Region.ReadInt32(el, "weight", ref weight, false);

              SpawnGroupElement groupElement = new SpawnGroupElement(def, weight);
              list.Add(groupElement);
            }

          SpawnGroupElement[] elements = list.ToArray();
          SpawnGroup group = new SpawnGroup(name, elements);
          Register(group);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Could not load SpawnDefinitions.xml: {ex.Message}");
      }
    }

    public SpawnGroup(string name, SpawnGroupElement[] elements)
    {
      Name = name;
      Elements = elements;

      m_TotalWeight = 0;
      for (int i = 0; i < elements.Length; i++)
        m_TotalWeight += elements[i].Weight;
    }

    public static Dictionary<string, SpawnGroup> Table{ get; } = new Dictionary<string, SpawnGroup>();

    public string Name{ get; }

    public SpawnGroupElement[] Elements{ get; }

    public static void Register(SpawnGroup group)
    {
      if (Table.ContainsKey(group.Name))
        Console.WriteLine("Warning: Double SpawnGroup name '{0}'", group.Name);
      else
        Table[group.Name] = group;
    }

    public override ISpawnable Spawn(SpawnEntry entry)
    {
      int index = Utility.Random(m_TotalWeight);

      for (int i = 0; i < Elements.Length; i++)
      {
        SpawnGroupElement element = Elements[i];

        if (index < element.Weight)
          return element.SpawnDefinition.Spawn(entry);

        index -= element.Weight;
      }

      return null;
    }

    public override bool CanSpawn(params Type[] types)
    {
      for (int i = 0; i < Elements.Length; i++)
        if (Elements[i].SpawnDefinition.CanSpawn(types))
          return true;

      return false;
    }
  }
}
