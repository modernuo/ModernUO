using Server.Commands.Generic;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CPA = Server.CommandPropertyAttribute;
/*
** modified properties gumps taken from RC0 properties gump scripts to support the special XmlSpawner properties gump
*/

namespace Server.Gumps;

public class XmlPropertiesGump : Gump
{
    private readonly ArrayList m_List;
    private int m_Page;
    private readonly Mobile m_Mobile;
    private readonly object m_Object;
    private readonly Stack<StackEntry> m_Stack;

    public static readonly bool OldStyle = PropsConfig.OldStyle;

    public static readonly int GumpOffsetX = PropsConfig.GumpOffsetX;
    public static readonly int GumpOffsetY = PropsConfig.GumpOffsetY;

    public static readonly int TextHue = PropsConfig.TextHue;
    public static readonly int TextOffsetX = PropsConfig.TextOffsetX;

    public static readonly int OffsetGumpID = PropsConfig.OffsetGumpID;
    public static readonly int EntryGumpID = PropsConfig.EntryGumpID;
    public static readonly int BackGumpID = PropsConfig.BackGumpID;
    public static readonly int SetGumpID = PropsConfig.SetGumpID;

    public static readonly int SetWidth = PropsConfig.SetWidth;
    public static readonly int SetOffsetX = PropsConfig.SetOffsetX, SetOffsetY = PropsConfig.SetOffsetY;
    public static readonly int SetButtonID1 = PropsConfig.SetButtonID1;
    public static readonly int SetButtonID2 = PropsConfig.SetButtonID2;

    public static readonly int OffsetSize = PropsConfig.OffsetSize;

    public static readonly int EntryHeight = PropsConfig.EntryHeight;
    public static readonly int BorderSize = PropsConfig.BorderSize;

    private static readonly int NameWidth = 103;
    private static readonly int ValueWidth = 82;

    private static readonly int EntryCount = 66;
    private static readonly int ColumnEntryCount = 22;

    private static readonly int TypeWidth = NameWidth + OffsetSize + ValueWidth;

    private static readonly int TotalWidth = OffsetSize + NameWidth + OffsetSize + ValueWidth + OffsetSize + SetWidth + OffsetSize;

    public XmlPropertiesGump(Mobile mobile, object o) : base(GumpOffsetX, GumpOffsetY)
    {
        m_Mobile = mobile;
        m_Object = o;
        m_List = BuildList();

        Initialize(0);
    }

    public XmlPropertiesGump(Mobile mobile, object o, Stack<StackEntry> stack, StackEntry parent) : base(GumpOffsetX, GumpOffsetY)
    {
        m_Mobile = mobile;
        m_Object = o;
        m_Stack = stack;
        m_List = BuildList();

        if (parent != null)
        {
            if (m_Stack == null)
            {
                m_Stack = new Stack<Server.Gumps.StackEntry>();
            }

            m_Stack.Push(parent);
        }

        Initialize(0);
    }

    public XmlPropertiesGump(Mobile mobile, object o, Stack<StackEntry> stack, ArrayList list, int page) : base(GumpOffsetX, GumpOffsetY)
    {
        m_Mobile = mobile;
        m_Object = o;
        m_List = list;
        m_Stack = stack;

        Initialize(page);
    }

    private void Initialize(int page)
    {
        m_Page = page;

        int count = m_List.Count - page * EntryCount;

        if (count < 0)
        {
            count = 0;
        }
        else if (count > EntryCount)
        {
            count = EntryCount;
        }

        int lastIndex = page * EntryCount + count - 1;

        if (lastIndex >= 0 && lastIndex < m_List.Count && m_List[lastIndex] == null)
        {
            --count;
        }

        int totalHeight = OffsetSize + (EntryHeight + OffsetSize) * (ColumnEntryCount + 1);

        AddPage(0);

        AddBackground(0, 0, TotalWidth * 3 + BorderSize * 2, BorderSize + totalHeight + BorderSize, BackGumpID);
        AddImageTiled(BorderSize, BorderSize + EntryHeight, (TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0)) * 3, totalHeight - EntryHeight, OffsetGumpID);

        int x = BorderSize + OffsetSize;
        int y = BorderSize;

        if (m_Object is Item item)
        {
            AddLabelCropped(x + TextOffsetX, y, TypeWidth - TextOffsetX, EntryHeight, TextHue, item.Name);
        }

        int propcount = 0;
        for (int i = 0, index = page * EntryCount; i <= count && index < m_List.Count; ++i, ++index)
        {
            // do the multi column display
            int column = propcount / ColumnEntryCount;
            if (propcount % ColumnEntryCount == 0)
            {
                y = BorderSize;
            }

            x = BorderSize + OffsetSize + column * (ValueWidth + NameWidth + OffsetSize * 2 + SetOffsetX + SetWidth);
            y += EntryHeight + OffsetSize;

            object o = m_List[index];

            if (o == null)
            {
                AddImageTiled(x - OffsetSize, y, TotalWidth, EntryHeight, BackGumpID + 4);
                propcount++;
            }
            else if (o is PropertyInfo prop)
            {
                propcount++;

                // look for the default value of the equivalent property in the XmlSpawnerDefaults.DefaultEntry class

                int huemodifier = TextHue;
                Mobiles.XmlSpawnerDefaults.DefaultEntry de = new Mobiles.XmlSpawnerDefaults.DefaultEntry();
                Type ftype = de.GetType();

                var finfo = ftype.GetField(prop.Name);

                // is there an equivalent default field?
                if (finfo != null)
                {
                    // see if the value is different from the default
                    if (ValueToString(finfo.GetValue(de)) != ValueToString(prop))
                    {
                        huemodifier = 68;
                    }
                }

                AddImageTiled(x, y, NameWidth, EntryHeight, EntryGumpID);
                AddLabelCropped(x + TextOffsetX, y, NameWidth - TextOffsetX, EntryHeight, huemodifier, prop.Name);
                x += NameWidth + OffsetSize;
                AddImageTiled(x, y, ValueWidth, EntryHeight, EntryGumpID);
                AddLabelCropped(x + TextOffsetX, y, ValueWidth - TextOffsetX, EntryHeight, huemodifier, ValueToString(prop));
                x += ValueWidth + OffsetSize;

                if (SetGumpID != 0)
                {
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
                }

                CPA cpa = GetCPA(prop);

                if (prop.CanWrite && cpa != null && m_Mobile.AccessLevel >= cpa.WriteLevel)
                {
                    AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, i + 3);
                }
            }
        }
    }

    public static string[] m_BoolNames = { "True", "False" };
    public static object[] m_BoolValues = { true, false };

    public static string[] m_PoisonNames = { "None", "Lesser", "Regular", "Greater", "Deadly", "Lethal" };
    public static object[] m_PoisonValues = { null, Poison.Lesser, Poison.Regular, Poison.Greater, Poison.Deadly, Poison.Lethal };

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
                    if (m_Stack != null && m_Stack.Count > 0)
                    {
                        StackEntry entry = m_Stack.Pop();
                        from.SendGump(new XmlPropertiesGump(from, entry.m_Object, m_Stack, null));
                    }

                    break;
                }
            case 1: // Previous
                {
                    if (m_Page > 0)
                    {
                        from.SendGump(new XmlPropertiesGump(from, m_Object, m_Stack, m_List, m_Page - 1));
                    }

                    break;
                }
            case 2: // Next
                {
                    if ((m_Page + 1) * EntryCount < m_List.Count)
                    {
                        from.SendGump(new XmlPropertiesGump(from, m_Object, m_Stack, m_List, m_Page + 1));
                    }

                    break;
                }
            default:
                {
                    int index = m_Page * EntryCount + (info.ButtonID - 3);

                    if (index >= 0 && index < m_List.Count)
                    {
                        PropertyInfo prop = m_List[index] as PropertyInfo;

                        if (prop == null)
                        {
                            return;
                        }

                        CPA attr = GetCPA(prop);

                        if (!prop.CanWrite || attr == null || from.AccessLevel < attr.WriteLevel)
                        {
                            return;
                        }

                        Type type = prop.PropertyType;

                        if (IsType(type, typeofMobile) || IsType(type, typeofItem))
                        {
                            from.SendGump(new XmlSetObjectGump(prop, from, m_Object, m_Stack, type, m_Page, m_List));
                        }
                        else if (IsType(type, typeofType))
                        {
                            from.Target = new XmlSetObjectTarget(prop, from, m_Object, m_Stack, type, m_Page, m_List);
                        }
                        else if (IsType(type, typeofPoint3D))
                        {
                            from.SendGump(new XmlSetPoint3DGump(prop, from, m_Object, m_Stack, m_Page, m_List));
                        }
                        else if (IsType(type, typeofPoint2D))
                        {
                            from.SendGump(new XmlSetPoint2DGump(prop, from, m_Object, m_Stack, m_Page, m_List));
                        }
                        else if (IsType(type, typeofTimeSpan))
                        {
                            from.SendGump(new XmlSetTimeSpanGump(prop, from, m_Object, m_Stack, m_Page, m_List));
                        }
                        else if (IsCustomEnum(type))
                        {
                            from.SendGump(new XmlSetCustomEnumGump(prop, from, m_Object, m_Stack, m_Page, m_List, GetCustomEnumNames(type)));
                        }
                        else if (IsType(type, typeofEnum))
                        {
                            from.SendGump(new XmlSetListOptionGump(prop, from, m_Object, m_Stack, m_Page, m_List, Enum.GetNames(type), GetObjects(Enum.GetValues(type))));
                        }
                        else if (IsType(type, typeofBool))
                        {
                            from.SendGump(new XmlSetListOptionGump(prop, from, m_Object, m_Stack, m_Page, m_List, m_BoolNames, m_BoolValues));
                        }
                        else if (IsType(type, typeofString) || IsType(type, typeofReal) || IsType(type, typeofNumeric))
                        {
                            from.SendGump(new XmlSetGump(prop, from, m_Object, m_Stack, m_Page, m_List));
                        }
                        else if (IsType(type, typeofPoison))
                        {
                            from.SendGump(new XmlSetListOptionGump(prop, from, m_Object, m_Stack, m_Page, m_List, m_PoisonNames, m_PoisonValues));
                        }
                        else if (IsType(type, typeofMap))
                        {
                            from.SendGump(new XmlSetListOptionGump(prop, from, m_Object, m_Stack, m_Page, m_List, Map.GetMapNames(), Map.GetMapValues()));
                        }
                        else if (IsType(type, typeofSkills) && m_Object is Mobile mobile)
                        {
                            from.SendGump(new XmlPropertiesGump(from, mobile, m_Stack, m_List, m_Page));
                            from.SendGump(new SkillsGump(from, mobile));
                        }
                        else if (HasAttribute(type, typeofPropertyObject, true))
                        {
                            object obj = prop.GetValue(m_Object, null);

                            from.SendGump(obj != null
                                ? new XmlPropertiesGump(from, obj, m_Stack,
                                    new StackEntry(m_Object, prop))
                                : new XmlPropertiesGump(from, m_Object, m_Stack, m_List, m_Page));
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
        {
            list[i] = a.GetValue(i);
        }

        return list;
    }

    private static bool IsCustomEnum(Type type) => type.IsDefined(typeofCustomEnum, false);

    private static string[] GetCustomEnumNames(Type type)
    {
        object[] attrs = type.GetCustomAttributes(typeofCustomEnum, false);

        if (attrs.Length == 0)
        {
            return new string[0];
        }

        CustomEnumAttribute ce = attrs[0] as CustomEnumAttribute;

        if (ce == null)
        {
            return new string[0];
        }

        return ce.Names;
    }

    private static bool HasAttribute(Type type, Type check, bool inherit)
    {
        object[] objs = type.GetCustomAttributes(check, inherit);

        return objs.Length > 0;
    }

    private static bool IsType(Type type, Type check) => type == check || type.IsSubclassOf(check);

    private static bool IsType(Type type, Type[] check)
    {
        for (int i = 0; i < check.Length; ++i)
        {
            if (IsType(type, check[i]))
            {
                return true;
            }
        }

        return false;
    }

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
        if (o == null)
        {
            return "-null-";
        }

        if (o is string s1)
        {
            return $"\"{s1}\"";
        }

        if (o is bool)
        {
            return o.ToString();
        }

        if (o is char c)
        {
            return $"0x{(int)c:X} '{c}'";
        }

        if (o is Serial s)
        {
            if (s.IsValid)
            {
                if (s.IsItem)
                {
                    return $"(I) 0x{s.Value:X}";
                }
                if (s.IsMobile)
                {
                    return $"(M) 0x{s.Value:X}";
                }
            }

            return $"(?) 0x{s.Value:X}";
        }

        if (o is byte || o is sbyte || o is short || o is ushort || o is int || o is uint || o is long || o is ulong)
        {
            return string.Format("{0} (0x{0:X})", o);
        }

        if (o is double)
        {
            return o.ToString();
        }

        if (o is Mobile mobile)
        {
            return $"(M) 0x{mobile.Serial.Value:X} \"{mobile.Name}\"";
        }

        if (o is Item item)
        {
            return $"(I) 0x{item.Serial:X}";
        }

        if (o is Type type)
        {
            return type.Name;
        }

        return o.ToString();
    }

    private ArrayList BuildList()
    {
        Type type = m_Object.GetType();

        PropertyInfo[] props = type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

        ArrayList groups = GetGroups(type, props);
        ArrayList list = new ArrayList();

        for (int i = 0; i < groups.Count; ++i)
        {
            DictionaryEntry de = (DictionaryEntry)groups[i];
            ArrayList groupList = (ArrayList)de.Value;

            if (!HasAttribute((Type)de.Key, typeofNoSort, false))
            {
                groupList.Sort(PropertySorter.Instance);
            }

            if (i != 0)
            {
                list.Add(null);
            }

            list.Add(de.Key);
            list.AddRange(groupList);
        }

        return list;
    }

    private static readonly Type typeofCPA = typeof(CPA);
    private static readonly Type typeofObject = typeof(object);

    private static CPA GetCPA(PropertyInfo prop)
    {
        object[] attrs = prop.GetCustomAttributes(typeofCPA, false);

        if (attrs.Length > 0)
        {
            return attrs[0] as CPA;
        }

        return null;
    }

    private ArrayList GetGroups(Type objectType, PropertyInfo[] props)
    {
        Hashtable groups = new Hashtable();

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
                        Type baseType = type.BaseType;

                        if (baseType == null || baseType == typeofObject)
                        {
                            break;
                        }

                        if (baseType.GetProperty(prop.Name, prop.PropertyType) != null)
                        {
                            type = baseType;
                        }
                        else
                        {
                            break;
                        }
                    }

                    ArrayList list = (ArrayList)groups[type];

                    if (list == null)
                    {
                        groups[type] = list = new ArrayList();
                    }

                    list.Add(prop);
                }
            }
        }

        ArrayList sorted = new ArrayList(groups);

        sorted.Sort(new GroupComparer(objectType));

        return sorted;
    }

    public static object GetObjectFromString(Type t, string s)
    {
        if (t == typeof(string))
        {
            return s;
        }

        if (t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) || t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong))
        {
            if (s.StartsWith("0x"))
            {
                if (t == typeof(ulong) || t == typeof(uint) || t == typeof(ushort) || t == typeof(byte))
                {
                    return Convert.ChangeType(Convert.ToUInt64(s.Substring(2), 16), t);
                }

                return Convert.ChangeType(Convert.ToInt64(s.Substring(2), 16), t);
            }

            return Convert.ChangeType(s, t);
        }

        if (t == typeof(double) || t == typeof(float))
        {
            return Convert.ChangeType(s, t);
        }
        if (t.IsDefined(typeof(ParsableAttribute), false))
        {
            MethodInfo parseMethod = t.GetMethod("Parse", new[] { typeof(string) });

            return parseMethod.Invoke(null, new object[] { s });
        }

        throw new Exception("bad");
    }

    private class PropertySorter : IComparer
    {
        public static readonly PropertySorter Instance = new();

        private PropertySorter()
        {
        }

        public int Compare(object x, object y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            PropertyInfo a = x as PropertyInfo;
            PropertyInfo b = y as PropertyInfo;

            if (a == null || b == null)
            {
                throw new ArgumentException();
            }

            return a.Name.CompareTo(b.Name);
        }
    }

    private class GroupComparer : IComparer
    {
        private readonly Type m_Start;

        public GroupComparer(Type start) => m_Start = start;

        private static readonly Type typeofObject = typeof(object);

        private int GetDistance(Type type)
        {
            Type current = m_Start;

            int dist;

            for (dist = 0; current != null && current != typeofObject && current != type; ++dist)
            {
                current = current.BaseType;
            }

            return dist;
        }

        public int Compare(object x, object y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            if (!(x is DictionaryEntry de1) || !(y is DictionaryEntry de2))
            {
                throw new ArgumentException();
            }

            Type a = (Type)de1.Key;
            Type b = (Type)de2.Key;

            return GetDistance(a).CompareTo(GetDistance(b));
        }
    }
}
