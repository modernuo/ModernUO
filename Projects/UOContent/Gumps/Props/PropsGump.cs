using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Server.Commands.Generic;
using Server.Network;
using CPA = Server.CommandPropertyAttribute;

namespace Server.Gumps
{
  public class StackEntry
  {
    public object m_Object;
    public PropertyInfo m_Property;

    public StackEntry(object obj, PropertyInfo prop)
    {
      m_Object = obj;
      m_Property = prop;
    }
  }

  public class PropertiesGump : Gump
  {
    public static readonly bool OldStyle = PropsConfig.OldStyle;

    public static readonly int GumpOffsetX = PropsConfig.GumpOffsetX;
    public static readonly int GumpOffsetY = PropsConfig.GumpOffsetY;

    public static readonly int TextHue = PropsConfig.TextHue;
    public static readonly int TextOffsetX = PropsConfig.TextOffsetX;

    public static readonly int OffsetGumpID = PropsConfig.OffsetGumpID;
    public static readonly int HeaderGumpID = PropsConfig.HeaderGumpID;
    public static readonly int EntryGumpID = PropsConfig.EntryGumpID;
    public static readonly int BackGumpID = PropsConfig.BackGumpID;
    public static readonly int SetGumpID = PropsConfig.SetGumpID;

    public static readonly int SetWidth = PropsConfig.SetWidth;
    public static readonly int SetOffsetX = PropsConfig.SetOffsetX, SetOffsetY = PropsConfig.SetOffsetY;
    public static readonly int SetButtonID1 = PropsConfig.SetButtonID1;
    public static readonly int SetButtonID2 = PropsConfig.SetButtonID2;

    public static readonly int PrevWidth = PropsConfig.PrevWidth;
    public static readonly int PrevOffsetX = PropsConfig.PrevOffsetX, PrevOffsetY = PropsConfig.PrevOffsetY;
    public static readonly int PrevButtonID1 = PropsConfig.PrevButtonID1;
    public static readonly int PrevButtonID2 = PropsConfig.PrevButtonID2;

    public static readonly int NextWidth = PropsConfig.NextWidth;
    public static readonly int NextOffsetX = PropsConfig.NextOffsetX, NextOffsetY = PropsConfig.NextOffsetY;
    public static readonly int NextButtonID1 = PropsConfig.NextButtonID1;
    public static readonly int NextButtonID2 = PropsConfig.NextButtonID2;

    public static readonly int OffsetSize = PropsConfig.OffsetSize;

    public static readonly int EntryHeight = PropsConfig.EntryHeight;
    public static readonly int BorderSize = PropsConfig.BorderSize;

    private static readonly bool PrevLabel = OldStyle;
    private static readonly bool NextLabel = OldStyle;
    private static readonly bool TypeLabel = !OldStyle;

    private static readonly int PrevLabelOffsetX = PrevWidth + 1;
    private static readonly int PrevLabelOffsetY = 0;

    private static readonly int NextLabelOffsetX = -29;
    private static readonly int NextLabelOffsetY = 0;

    private static readonly int NameWidth = 107;
    private static readonly int ValueWidth = 128;

    private static readonly int EntryCount = 15;

    private static readonly int TypeWidth = NameWidth + OffsetSize + ValueWidth;

    private static readonly int TotalWidth =
      OffsetSize + NameWidth + OffsetSize + ValueWidth + OffsetSize + SetWidth + OffsetSize;

    private static readonly int TotalHeight = OffsetSize + (EntryHeight + OffsetSize) * (EntryCount + 1);

    private static readonly int BackWidth = BorderSize + TotalWidth + BorderSize;
    private static readonly int BackHeight = BorderSize + TotalHeight + BorderSize;

    public static string[] m_BoolNames = { "True", "False" };
    public static object[] m_BoolValues = { true, false };

    public static string[] m_PoisonNames = { "None", "Lesser", "Regular", "Greater", "Deadly", "Lethal" };

    public static object[] m_PoisonValues =
      { null, Poison.Lesser, Poison.Regular, Poison.Greater, Poison.Deadly, Poison.Lethal };

    private static readonly Type typeofMobile = typeof(Mobile);
    private static readonly Type typeofItem = typeof(Item);
    private static readonly Type typeofType = typeof(Type);
    private static readonly Type typeofPoint3D = typeof(Point3D);
    private static readonly Type typeofPoint2D = typeof(Point2D);
    private static readonly Type typeofTimeSpan = typeof(TimeSpan);
    private static readonly Type typeofCustomEnum = typeof(CustomEnumAttribute);
    private static readonly Type typeofEnum = typeof(Enum);
    private static readonly Type typeofBool = typeof(bool);
    private static readonly Type typeofString = typeof(string);
    private static readonly Type typeofText = typeof(TextDefinition);
    private static readonly Type typeofPoison = typeof(Poison);
    private static readonly Type typeofMap = typeof(Map);
    private static readonly Type typeofSkills = typeof(Skills);
    private static readonly Type typeofPropertyObject = typeof(PropertyObjectAttribute);
    private static readonly Type typeofNoSort = typeof(NoSortAttribute);

    private static readonly Type[] typeofReal =
    {
      typeof(float),
      typeof(double)
    };

    private static readonly Type[] typeofNumeric =
    {
      typeof(byte),
      typeof(short),
      typeof(int),
      typeof(long),
      typeof(sbyte),
      typeof(ushort),
      typeof(uint),
      typeof(ulong)
    };

    private static readonly Type typeofCPA = typeof(CPA);
    private static readonly Type typeofObject = typeof(object);
    private readonly List<object> m_List;
    private readonly Mobile m_Mobile;
    private readonly object m_Object;
    private int m_Page;
    private readonly Stack<StackEntry> m_Stack;
    private readonly Type m_Type;

    public PropertiesGump(Mobile mobile, object o) : base(GumpOffsetX, GumpOffsetY)
    {
      m_Mobile = mobile;
      m_Object = o;
      m_Type = o.GetType();
      m_List = BuildList();

      Initialize(0);
    }

    public PropertiesGump(Mobile mobile, object o, Stack<StackEntry> stack, StackEntry parent) : base(GumpOffsetX,
      GumpOffsetY)
    {
      m_Mobile = mobile;
      m_Object = o;
      m_Type = o.GetType();
      m_Stack = stack;
      m_List = BuildList();

      if (parent != null)
      {
        m_Stack ??= new Stack<StackEntry>();
        m_Stack.Push(parent);
      }

      Initialize(0);
    }

    public PropertiesGump(Mobile mobile, object o, Stack<StackEntry> stack, List<object> list, int page) : base(GumpOffsetX,
      GumpOffsetY)
    {
      m_Mobile = mobile;
      m_Object = o;

      if (o != null)
        m_Type = o.GetType();

      m_List = list;
      m_Stack = stack;

      Initialize(page);
    }

    private void Initialize(int page)
    {
      m_Page = page;

      int count = Math.Clamp(m_List.Count - page * EntryCount, 0, EntryCount);

      int lastIndex = page * EntryCount + count - 1;

      if (lastIndex >= 0 && lastIndex < m_List.Count && m_List[lastIndex] == null)
        --count;

      int totalHeight = OffsetSize + (EntryHeight + OffsetSize) * (count + 1);

      AddPage(0);

      AddBackground(0, 0, BackWidth, BorderSize + totalHeight + BorderSize, BackGumpID);
      AddImageTiled(BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), totalHeight,
        OffsetGumpID);

      int x = BorderSize + OffsetSize;
      int y = BorderSize + OffsetSize;

      int emptyWidth = TotalWidth - PrevWidth - NextWidth - OffsetSize * 4 - (OldStyle ? SetWidth + OffsetSize : 0);

      if (OldStyle)
        AddImageTiled(x, y, TotalWidth - OffsetSize * 3 - SetWidth, EntryHeight, HeaderGumpID);
      else
        AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);

      if (page > 0)
      {
        AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, 1);

        if (PrevLabel)
          AddLabel(x + PrevLabelOffsetX, y + PrevLabelOffsetY, TextHue, "Previous");
      }

      x += PrevWidth + OffsetSize;

      if (!OldStyle)
        AddImageTiled(x, y, emptyWidth, EntryHeight, HeaderGumpID);

      if (TypeLabel && m_Type != null)
        AddHtml(x, y, emptyWidth, EntryHeight,
          $"<BASEFONT COLOR=#FAFAFA><CENTER>{m_Type.Name}</CENTER></BASEFONT>");

      x += emptyWidth + OffsetSize;

      if (!OldStyle)
        AddImageTiled(x, y, NextWidth, EntryHeight, HeaderGumpID);

      if ((page + 1) * EntryCount < m_List.Count)
      {
        AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, 2, GumpButtonType.Reply, 1);

        if (NextLabel)
          AddLabel(x + NextLabelOffsetX, y + NextLabelOffsetY, TextHue, "Next");
      }

      for (int i = 0, index = page * EntryCount; i < count && index < m_List.Count; ++i, ++index)
      {
        x = BorderSize + OffsetSize;
        y += EntryHeight + OffsetSize;

        object o = m_List[index];

        if (o == null)
        {
          AddImageTiled(x - OffsetSize, y, TotalWidth, EntryHeight, BackGumpID + 4);
        }
        else if (o is Type type)
        {
          AddImageTiled(x, y, TypeWidth, EntryHeight, EntryGumpID);
          AddLabelCropped(x + TextOffsetX, y, TypeWidth - TextOffsetX, EntryHeight, TextHue, type.Name);
          x += TypeWidth + OffsetSize;

          if (SetGumpID != 0)
            AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
        }
        else if (o is PropertyInfo prop)
        {
          AddImageTiled(x, y, NameWidth, EntryHeight, EntryGumpID);
          AddLabelCropped(x + TextOffsetX, y, NameWidth - TextOffsetX, EntryHeight, TextHue, prop.Name);
          x += NameWidth + OffsetSize;
          AddImageTiled(x, y, ValueWidth, EntryHeight, EntryGumpID);
          AddLabelCropped(x + TextOffsetX, y, ValueWidth - TextOffsetX, EntryHeight, TextHue, ValueToString(prop));
          x += ValueWidth + OffsetSize;

          if (SetGumpID != 0)
            AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);

          CPA cpa = GetCPA(prop);

          if ((!prop.GetType().IsValueType || prop.CanWrite) && cpa != null && m_Mobile.AccessLevel >= cpa.WriteLevel && !cpa.ReadOnly)
            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, i + 3);
        }
      }
    }

    public override void OnResponse(NetState state, RelayInfo info)
    {
      Mobile from = state.Mobile;

      if (!BaseCommand.IsAccessible(from, m_Object))
      {
        from.SendMessage("You may no longer access their properties.");
        return;
      }

      switch (info.ButtonID)
      {
        case 0: // Closed
          {
            if (m_Stack?.Count > 0)
            {
              StackEntry entry = m_Stack.Pop();
              from.SendGump(new PropertiesGump(from, entry.m_Object, m_Stack, null));
            }

            break;
          }
        case 1: // Previous
          {
            if (m_Page > 0)
              from.SendGump(new PropertiesGump(from, m_Object, m_Stack, m_List, m_Page - 1));

            break;
          }
        case 2: // Next
          {
            if ((m_Page + 1) * EntryCount < m_List.Count)
              from.SendGump(new PropertiesGump(from, m_Object, m_Stack, m_List, m_Page + 1));

            break;
          }
        default:
          {
            int index = m_Page * EntryCount + (info.ButtonID - 3);

            if (index >= 0 && index < m_List.Count)
            {
              PropertyInfo prop = m_List[index] as PropertyInfo;

              if (prop == null)
                return;

              CPA attr = GetCPA(prop);

              if ((prop.GetType().IsValueType && !prop.CanWrite) || attr == null || from.AccessLevel < attr.WriteLevel || attr.ReadOnly)
                return;

              Type type = prop.PropertyType;

              if (IsType(type, typeofMobile) || IsType(type, typeofItem))
              {
                from.SendGump(new SetObjectGump(prop, from, m_Object, m_Stack, type, m_Page, m_List));
              }
              else if (IsType(type, typeofType))
              {
                from.Target = new SetObjectTarget(prop, from, m_Object, m_Stack, type, m_Page, m_List);
              }
              else if (IsType(type, typeofPoint3D))
              {
                from.SendGump(new SetPoint3DGump(prop, from, m_Object, m_Stack, m_Page, m_List));
              }
              else if (IsType(type, typeofPoint2D))
              {
                from.SendGump(new SetPoint2DGump(prop, from, m_Object, m_Stack, m_Page, m_List));
              }
              else if (IsType(type, typeofTimeSpan))
              {
                from.SendGump(new SetTimeSpanGump(prop, from, m_Object, m_Stack, m_Page, m_List));
              }
              else if (IsCustomEnum(type))
              {
                from.SendGump(new SetCustomEnumGump(prop, from, m_Object, m_Stack, m_Page, m_List,
                  GetCustomEnumNames(type)));
              }
              else if (IsType(type, typeofEnum))
              {
                from.SendGump(new SetListOptionGump(prop, from, m_Object, m_Stack, m_Page, m_List,
                  Enum.GetNames(type), GetObjects(Enum.GetValues(type))));
              }
              else if (IsType(type, typeofBool))
              {
                from.SendGump(new SetListOptionGump(prop, from, m_Object, m_Stack, m_Page, m_List, m_BoolNames,
                  m_BoolValues));
              }
              else if (IsType(type, typeofString) || IsType(type, typeofReal) || IsType(type, typeofNumeric) ||
                       IsType(type, typeofText))
              {
                from.SendGump(new SetGump(prop, from, m_Object, m_Stack, m_Page, m_List));
              }
              else if (IsType(type, typeofPoison))
              {
                from.SendGump(new SetListOptionGump(prop, from, m_Object, m_Stack, m_Page, m_List, m_PoisonNames,
                  m_PoisonValues));
              }
              else if (IsType(type, typeofMap))
              {
                from.SendGump(new SetListOptionGump(prop, from, m_Object, m_Stack, m_Page, m_List,
                  Map.GetMapNames(), Map.GetMapValues().ToArray<object>()));
              }
              else if (IsType(type, typeofSkills) && m_Object is Mobile mobile)
              {
                from.SendGump(new PropertiesGump(from, mobile, m_Stack, m_List, m_Page));
                from.SendGump(new SkillsGump(from, mobile));
              }
              else if (HasAttribute(type, typeofPropertyObject, true))
              {
                object obj = prop.GetValue(m_Object, null);

                if (obj != null)
                  from.SendGump(new PropertiesGump(from, obj, m_Stack, new StackEntry(m_Object, prop)));
                else
                  from.SendGump(new PropertiesGump(from, m_Object, m_Stack, m_List, m_Page));
              }
            }

            break;
          }
      }
    }

    private static object[] GetObjects(Array a)
    {
      object[] list = new object[a.Length];

      for (int i = 0; i < list.Length; ++i)
        list[i] = a.GetValue(i);

      return list;
    }

    private static bool IsCustomEnum(Type type) => type.IsDefined(typeofCustomEnum, false);

    public static void OnValueChanged(object obj, PropertyInfo prop, Stack<StackEntry> stack)
    {
      if (stack == null || stack.Count == 0)
        return;

      if (!prop.PropertyType.IsValueType)
        return;

      StackEntry peek = stack.Peek();

      if (peek.m_Property.CanWrite)
        peek.m_Property.SetValue(peek.m_Object, obj, null);
    }

    private static string[] GetCustomEnumNames(Type type)
    {
      object[] attrs = type.GetCustomAttributes(typeofCustomEnum, false);

      if (attrs.Length == 0)
        return Array.Empty<string>();

      if (!(attrs[0] is CustomEnumAttribute ce))
        return Array.Empty<string>();

      return ce.Names;
    }

    private static bool HasAttribute(Type type, Type check, bool inherit) => type.GetCustomAttributes(check, inherit).Length > 0;

    private static bool IsType(Type type, Type check) => type == check || type.IsSubclassOf(check);

    private static bool IsType(Type type, Type[] check)
    {
      for (int i = 0; i < check.Length; ++i)
        if (IsType(type, check[i]))
          return true;

      return false;
    }

    private string ValueToString(PropertyInfo prop) => ValueToString(m_Object, prop);

    public static string ValueToString(object obj, PropertyInfo prop)
    {
      try
      {
        return ValueToString(prop.GetValue(obj, null));
      }
      catch (Exception e)
      {
        return $"!{e.GetType()}!";
      }
    }

    public static string ValueToString(object o)
    {
      if (o == null) return "-null-";
      if (o is string s) return $"\"{s}\"";
      if (o is bool) return o.ToString();
      if (o is char c) return $"0x{(int)c:X} '{c}'";
      if (o is Serial serial)
      {
        if (serial.IsValid)
        {
          if (serial.IsItem) return $"(I) 0x{serial.Value:X}";
          if (serial.IsMobile) return $"(M) 0x{serial.Value:X}";
        }

        return $"(?) 0x{serial.Value:X}";
      }

      if (o is byte || o is sbyte || o is short || o is ushort || o is int || o is uint || o is long || o is ulong)
        return $"{o} (0x{o:X})";
      if (o is Mobile mobile) return $"(M) 0x{mobile.Serial.Value:X} \"{mobile.Name}\"";
      if (o is Item item) return $"(I) 0x{item.Serial.Value:X}";
      if (o is Type type) return type.Name;
      if (o is TextDefinition definition) return definition.Format(true);

      return o.ToString();
    }

    private List<object> BuildList()
    {
      List<object> list = new List<object>();

      if (m_Type == null)
        return list;

      PropertyInfo[] props = m_Type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

      List<KeyValuePair<Type, List<PropertyInfo>>> groups = GetGroups(m_Type, props);

      for (int i = 0; i < groups.Count; ++i)
      {
        KeyValuePair<Type, List<PropertyInfo>> kvp = groups[i];

        if (!HasAttribute(kvp.Key, typeofNoSort, false))
          kvp.Value.Sort(PropertySorter.Instance);

        if (i != 0)
          list.Add(null);

        list.Add(kvp.Key);
        list.AddRange(kvp.Value);
      }

      return list;
    }

    private static CPA GetCPA(PropertyInfo prop)
    {
      object[] attrs = prop.GetCustomAttributes(typeofCPA, false);

      if (attrs.Length > 0)
        return attrs[0] as CPA;
      return null;
    }

    private List<KeyValuePair<Type, List<PropertyInfo>>> GetGroups(Type objectType, PropertyInfo[] props)
    {
      Dictionary<Type, List<PropertyInfo>> groups = new Dictionary<Type, List<PropertyInfo>>();

      for (int i = 0; i < props.Length; ++i)
      {
        PropertyInfo prop = props[i];

        if (prop.CanRead)
        {
          CPA attr = GetCPA(prop);

          if (attr != null && m_Mobile.AccessLevel >= attr.ReadLevel)
          {
            Type type = prop.DeclaringType;

            while (true)
            {
              Type baseType = type?.BaseType;

              if (baseType == typeofObject || baseType?.GetProperty(prop.Name, prop.PropertyType) == null)
                break;

              type = baseType;
            }

            if (type != null && !groups.ContainsKey(type))
              groups[type] = new List<PropertyInfo> { prop };
            else
              groups[type].Add(prop);
          }
        }
      }

      List<KeyValuePair<Type, List<PropertyInfo>>> list = groups.ToList();
      list.Sort(new GroupComparer(objectType));

      return list;
    }

    public static object GetObjectFromString(Type t, string s)
    {
      if (t == typeof(string)) return s;

      if (t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) || t == typeof(int) ||
          t == typeof(uint) || t == typeof(long) || t == typeof(ulong))
      {
        if (s.StartsWith("0x"))
        {
          if (t == typeof(ulong) || t == typeof(uint) || t == typeof(ushort) || t == typeof(byte))
            return Convert.ChangeType(Convert.ToUInt64(s.Substring(2), 16), t);

          return Convert.ChangeType(Convert.ToInt64(s.Substring(2), 16), t);
        }

        return Convert.ChangeType(s, t);
      }

      if (t == typeof(double) || t == typeof(float)) return Convert.ChangeType(s, t);
      if (t.IsDefined(typeof(ParsableAttribute), false))
      {
        MethodInfo parseMethod = t.GetMethod("Parse", new[] { typeof(string) });

        return parseMethod?.Invoke(null, new object[] { s });
      }

      throw new Exception("bad");
    }

    private static string GetStringFromObject(object o)
    {
      if (o == null) return "-null-";
      if (o is string s) return $"\"{s}\"";
      if (o is bool) return o.ToString();
      if (o is char c) return $"0x{(int)c:X} '{c}'";
      if (o is Serial serial)
      {
        if (serial.IsValid)
        {
          if (serial.IsItem) return $"(I) 0x{serial.Value:X}";
          if (serial.IsMobile) return $"(M) 0x{serial.Value:X}";
        }

        return $"(?) 0x{serial.Value:X}";
      }

      if (o is byte || o is sbyte || o is short || o is ushort || o is int || o is uint || o is long || o is ulong)
        return $"{o} (0x{o:X})";
      if (o is Mobile mobile) return $"(M) 0x{mobile.Serial.Value:X} \"{mobile.Name}\"";
      if (o is Item item) return $"(I) 0x{item.Serial.Value:X}";
      if (o is Type type) return type.Name;

      return o.ToString();
    }

    private class PropertySorter : IComparer<PropertyInfo>
    {
      public static readonly PropertySorter Instance = new PropertySorter();

      private PropertySorter()
      {
      }

      public int Compare(PropertyInfo x, PropertyInfo y)
      {
        if (x == null && y == null)
          return 0;
        if (x == null)
          return -1;

        return y == null ? 1 : x.Name.CompareTo(x.Name);
      }
    }

    private class GroupComparer : IComparer<KeyValuePair<Type, List<PropertyInfo>>>
    {
      private readonly Type m_Start;

      public GroupComparer(Type start) => m_Start = start;

      public int Compare(KeyValuePair<Type, List<PropertyInfo>> x, KeyValuePair<Type, List<PropertyInfo>> y) => GetDistance(x.Key).CompareTo(GetDistance(y.Key));

      private int GetDistance(Type type)
      {
        Type current = m_Start;

        int dist;

        for (dist = 0; current != null && current != typeofObject && current != type; ++dist)
          current = current.BaseType;

        return dist;
      }
    }
  }
}
