using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using Server.Items;
using Server.Utilities;

namespace Server.Commands
{
  public class Categorization
  {
    private static CategoryEntry m_RootItems, m_RootMobiles;

    private static readonly Type typeofItem = typeof(Item);
    private static readonly Type typeofMobile = typeof(Mobile);
    private static readonly Type typeofConstructible = typeof(ConstructibleAttribute);

    public static CategoryEntry Items
    {
      get
      {
        if (m_RootItems == null)
          Load();

        return m_RootItems;
      }
    }

    public static CategoryEntry Mobiles
    {
      get
      {
        if (m_RootMobiles == null)
          Load();

        return m_RootMobiles;
      }
    }

    public static void Initialize()
    {
      CommandSystem.Register("RebuildCategorization", AccessLevel.Administrator, RebuildCategorization_OnCommand);
    }

    [Usage("RebuildCategorization")]
    [Description("Rebuilds the categorization data file used by the Add command.")]
    public static void RebuildCategorization_OnCommand(CommandEventArgs e)
    {
      CategoryEntry root = new CategoryEntry(null, "Add Menu", new[] { Items, Mobiles });

      Export(root, "Data/objects.xml", "Objects");

      e.Mobile.SendMessage("Categorization menu rebuilt.");
    }

    public static void Export(CategoryEntry ce, string fileName, string title)
    {
      XmlTextWriter xml = new XmlTextWriter(fileName, Encoding.UTF8);

      xml.Indentation = 1;
      xml.IndentChar = '\t';
      xml.Formatting = Formatting.Indented;

      xml.WriteStartDocument(true);

      RecurseExport(xml, ce);

      xml.Flush();
      xml.Close();
    }

    public static void RecurseExport(XmlTextWriter xml, CategoryEntry ce)
    {
      xml.WriteStartElement("category");

      xml.WriteAttributeString("title", ce.Title);

      List<CategoryEntry> subCats = new List<CategoryEntry>(ce.SubCategories);

      subCats.Sort(new CategorySorter());

      for (int i = 0; i < subCats.Count; ++i)
        RecurseExport(xml, subCats[i]);

      ce.Matched.Sort(new CategoryTypeSorter());

      for (int i = 0; i < ce.Matched.Count; ++i)
      {
        CategoryTypeEntry cte = ce.Matched[i];

        xml.WriteStartElement("object");

        xml.WriteAttributeString("type", cte.Type.ToString());

        if (cte.Object is Item item)
        {
          int itemID = item.ItemID;

          if (item is BaseAddon addon && addon.Components.Count == 1)
            itemID = addon.Components[0].ItemID;

          if (itemID > TileData.MaxItemValue)
            itemID = 1;

          xml.WriteAttributeString("gfx", XmlConvert.ToString(itemID));

          int hue = item.Hue & 0x7FFF;

          if ((hue & 0x4000) != 0)
            hue = 0;

          if (hue != 0)
            xml.WriteAttributeString("hue", XmlConvert.ToString(hue));

          item.Delete();
        }
        else if (cte.Object is Mobile mob)
        {
          int itemID = ShrinkTable.Lookup(mob, 1);

          xml.WriteAttributeString("gfx", XmlConvert.ToString(itemID));

          int hue = mob.Hue & 0x7FFF;

          if ((hue & 0x4000) != 0)
            hue = 0;

          if (hue != 0)
            xml.WriteAttributeString("hue", XmlConvert.ToString(hue));

          mob.Delete();
        }

        xml.WriteEndElement();
      }

      xml.WriteEndElement();
    }

    public static void Load()
    {
      List<Type> types = new List<Type>();

      AddTypes(Core.Assembly, types);

      for (int i = 0; i < AssemblyHandler.Assemblies.Length; ++i)
        AddTypes(AssemblyHandler.Assemblies[i], types);

      m_RootItems = Load(types, "Data/items.cfg");
      m_RootMobiles = Load(types, "Data/mobiles.cfg");
    }

    private static CategoryEntry Load(List<Type> types, string config)
    {
      CategoryLine[] lines = CategoryLine.Load(config);

      if (lines.Length > 0)
      {
        int index = 0;
        CategoryEntry root = new CategoryEntry(null, lines, ref index);

        Fill(root, types);

        return root;
      }

      return new CategoryEntry();
    }

    private static bool IsConstructible(Type type)
    {
      if (!type.IsSubclassOf(typeofItem) && !type.IsSubclassOf(typeofMobile))
        return false;

      ConstructorInfo ctor = type.GetConstructor(Type.EmptyTypes);

      return ctor?.IsDefined(typeofConstructible, false) == true;
    }

    private static void AddTypes(Assembly asm, List<Type> types)
    {
      Type[] allTypes = asm.GetTypes();

      for (int i = 0; i < allTypes.Length; ++i)
      {
        Type type = allTypes[i];

        if (type.IsAbstract)
          continue;

        if (IsConstructible(type))
          types.Add(type);
      }
    }

    private static void Fill(CategoryEntry root, List<Type> list)
    {
      for (int i = 0; i < list.Count; ++i)
      {
        Type type = list[i];
        CategoryEntry match = GetDeepestMatch(root, type);

        if (match == null)
          continue;

        try
        {
          match.Matched.Add(new CategoryTypeEntry(type));
        }
        catch
        {
          // ignored
        }
      }
    }

    private static CategoryEntry GetDeepestMatch(CategoryEntry root, Type type)
    {
      if (!root.IsMatch(type))
        return null;

      for (int i = 0; i < root.SubCategories.Length; ++i)
      {
        CategoryEntry check = GetDeepestMatch(root.SubCategories[i], type);

        if (check != null)
          return check;
      }

      return root;
    }
  }

  public class CategorySorter : IComparer<CategoryEntry>
  {
    public int Compare(CategoryEntry x, CategoryEntry y)
    {
      string a = x?.Title;
      string b = y?.Title;

      if (a == null && b == null)
        return 0;

      if (a == null)
        return 1;

      return a.CompareTo(b);
    }
  }

  public class CategoryTypeSorter : IComparer<CategoryTypeEntry>
  {
    public int Compare(CategoryTypeEntry x, CategoryTypeEntry y)
    {
      string a = x?.Type.Name;
      string b = y?.Type.Name;

      if (a == null && b == null)
        return 0;

      if (a == null)
        return 1;

      return a.CompareTo(b);
    }
  }

  public class CategoryTypeEntry
  {
    public CategoryTypeEntry(Type type)
    {
      Type = type;
      Object = ActivatorUtil.CreateInstance(type);
    }

    public Type Type { get; }

    public object Object { get; }
  }

  public class CategoryEntry
  {
    public CategoryEntry(CategoryEntry parent = null, string title = "(empty)", CategoryEntry[] subCats = null)
    {
      Parent = parent;
      Title = title;
      SubCategories = subCats ?? Array.Empty<CategoryEntry>();
      Matches = Array.Empty<Type>();
      Matched = new List<CategoryTypeEntry>();
    }

    public CategoryEntry(CategoryEntry parent, CategoryLine[] lines, ref int index)
    {
      Parent = parent;

      string text = lines[index].Text;

      int start = text.IndexOf('(');

      if (start < 0)
        throw new FormatException($"Input string not correctly formatted ('{text}')");

      Title = text.Substring(0, start).Trim();

      int end = text.IndexOf(')', ++start);

      if (end < start)
        throw new FormatException($"Input string not correctly formatted ('{text}')");

      text = text.Substring(start, end - start);
      string[] split = text.Split(';');

      List<Type> list = new List<Type>();

      for (int i = 0; i < split.Length; ++i)
      {
        Type type = AssemblyHandler.FindFirstTypeForName(split[i].Trim());

        if (type == null)
          Console.WriteLine("Match type not found ('{0}')", split[i].Trim());
        else
          list.Add(type);
      }

      Matches = list.ToArray();
      list.Clear();

      int ourIndentation = lines[index].Indentation;

      ++index;

      List<CategoryEntry> entryList = new List<CategoryEntry>();

      while (index < lines.Length && lines[index].Indentation > ourIndentation)
        entryList.Add(new CategoryEntry(this, lines, ref index));

      SubCategories = entryList.ToArray();
      entryList.Clear();

      Matched = new List<CategoryTypeEntry>();
    }

    public string Title { get; }

    public Type[] Matches { get; }

    public CategoryEntry Parent { get; }

    public CategoryEntry[] SubCategories { get; }

    public List<CategoryTypeEntry> Matched { get; }

    public bool IsMatch(Type type)
    {
      bool isMatch = false;

      for (int i = 0; !isMatch && i < Matches.Length; ++i)
        isMatch = type == Matches[i] || type.IsSubclassOf(Matches[i]);

      return isMatch;
    }
  }

  public class CategoryLine
  {
    public CategoryLine(string input)
    {
      int index;

      for (index = 0; index < input.Length; ++index)
        if (char.IsLetter(input, index))
          break;

      if (index >= input.Length)
        throw new FormatException($"Input string not correctly formatted ('{input}')");

      Indentation = index;
      Text = input.Substring(index);
    }

    public int Indentation { get; }

    public string Text { get; }

    public static CategoryLine[] Load(string path)
    {
      List<CategoryLine> list = new List<CategoryLine>();

      if (File.Exists(path))
      {
        using StreamReader ip = new StreamReader(path);
        string line;

        while ((line = ip.ReadLine()) != null)
          list.Add(new CategoryLine(line));
      }

      return list.ToArray();
    }
  }
}
