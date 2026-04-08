using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Server.Commands.Generic;
using Server.Network;
using static Server.Types;
using static Server.Attributes;
using static Server.Gumps.PropsConfig;

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
        private const int NameWidth = 107;
        private const int ValueWidth = 128;

        private const int MaxEntriesPerPage = 15;

        private const int TypeWidth = NameWidth + OffsetSize + ValueWidth;

        private const int TotalWidth = OffsetSize + NameWidth + OffsetSize + ValueWidth + OffsetSize + SetWidth + OffsetSize;

        protected const int BackWidth = BorderSize + TotalWidth + BorderSize;

        protected readonly List<object> m_List;
        protected readonly Mobile m_Mobile;
        protected readonly object m_Object;
        protected readonly Stack<StackEntry> m_Stack;
        protected readonly Type m_Type;
        protected int m_Page;
        protected int m_EntryCount;

        protected virtual int TotalHeight => OffsetSize + (EntryHeight + OffsetSize) * (m_EntryCount + 1);

        public PropertiesGump(Mobile mobile, object o) : base(GumpOffsetX, GumpOffsetY)
        {
            m_Mobile = mobile;
            m_Object = o;
            m_Type = o.GetType();
            m_List = BuildList();

            Initialize(0);
        }

        public PropertiesGump(Mobile mobile, object o, Stack<StackEntry> stack, StackEntry parent) : base(
            GumpOffsetX,
            GumpOffsetY
        )
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

        public PropertiesGump(Mobile mobile, object o, Stack<StackEntry> stack, List<object> list, int page) : base(
            GumpOffsetX,
            GumpOffsetY
        )
        {
            m_Mobile = mobile;
            m_Object = o;

            if (o != null)
            {
                m_Type = o.GetType();
            }

            m_List = list;
            m_Stack = stack;

            Initialize(page);
        }

        protected virtual void Initialize(int page)
        {
            m_Page = page;
            var indexOnPage = m_Page * MaxEntriesPerPage;

            m_EntryCount = Math.Clamp(m_List.Count - indexOnPage, 0, MaxEntriesPerPage);
            var lastIndex = indexOnPage + m_EntryCount - 1;
            if (lastIndex >= 0 && lastIndex < m_List.Count && m_List[lastIndex] == null)
            {
                --m_EntryCount;
            }

            AddPage(0);

            this.AddPropsFrame(TotalWidth, m_EntryCount + 1, out var x, out var y);
            this.AddPropsHeader(
                TotalWidth, ref x, ref y,
                m_Type?.Name.Center(0xFAFAFA),
                page > 0, 1,
                (page + 1) * MaxEntriesPerPage < m_List.Count, 2,
                nextType: GumpButtonType.Reply, nextParam: 1
            );

            for (int i = 0, index = page * MaxEntriesPerPage; i < m_EntryCount && index < m_List.Count; ++i, ++index)
            {
                PropsLayout.NextRow(ref x, ref y);

                var o = m_List[index];

                if (o == null)
                {
                    this.AddPropsEntryBlank(x, y, TotalWidth);
                }
                else if (o is Type type)
                {
                    this.AddPropsEntryType(ref x, ref y, TypeWidth, type.Name);
                }
                else if (o is PropertyInfo prop)
                {
                    var cpa = GetCPA(prop);
                    var canModify = cpa?.ReadOnly == false && m_Mobile.AccessLevel >= cpa.WriteLevel && (prop.CanWrite || cpa.CanModify);

                    this.AddPropsEntryNameValue(
                        ref x, ref y, NameWidth, ValueWidth,
                        prop.Name, ValueToString(prop),
                        canModify, i + 3
                    );
                }
            }
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            var from = state.Mobile;

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
                            var entry = m_Stack.Pop();
                            from.SendGump(new PropertiesGump(from, entry.m_Object, m_Stack, null));
                        }

                        break;
                    }
                case 1: // Previous
                    {
                        if (m_Page > 0)
                        {
                            from.SendGump(new PropertiesGump(from, m_Object, m_Stack, m_List, m_Page - 1));
                        }

                        break;
                    }
                case 2: // Next
                    {
                        if ((m_Page + 1) * MaxEntriesPerPage < m_List.Count)
                        {
                            from.SendGump(new PropertiesGump(from, m_Object, m_Stack, m_List, m_Page + 1));
                        }

                        break;
                    }
                default:
                    {
                        var index = m_Page * MaxEntriesPerPage + (info.ButtonID - 3);

                        if (index < 0 || index >= m_List.Count)
                        {
                            break;
                        }

                        var prop = m_List[index] as PropertyInfo;

                        if (prop == null)
                        {
                            return;
                        }

                        var cpa = GetCPA(prop);

                        if (cpa == null || !(prop.CanWrite || cpa.CanModify) || cpa.ReadOnly ||
                            from.AccessLevel < cpa.WriteLevel)
                        {
                            return;
                        }

                        var obj = prop.GetValue(m_Object, null);
                        var type = obj?.GetType() ?? prop.PropertyType;

                        if (IsType(type, OfEntity))
                        {
                            from.SendGump(new SetObjectGump(prop, from, m_Object, type, this));
                        }
                        else if (IsType(type, OfType))
                        {
                            from.Target = new SetObjectTarget(prop, from, m_Object, type, this);
                        }
                        else if (IsType(type, OfPoint3D))
                        {
                            from.SendGump(new SetPoint3DGump(prop, from, m_Object, this));
                        }
                        else if (IsType(type, OfPoint2D))
                        {
                            from.SendGump(new SetPoint2DGump(prop, from, m_Object, this));
                        }
                        else if (IsType(type, OfTimeSpan))
                        {
                            from.SendGump(new SetTimeSpanGump(prop, from, m_Object, this));
                        }
                        else if (IsCustomEnum(type))
                        {
                            from.SendGump(
                                new SetCustomEnumGump(
                                    prop,
                                    from,
                                    m_Object,
                                    this,
                                    GetCustomEnumNames(type)
                                )
                            );
                        }
                        else if (IsType(type, OfEnum))
                        {
                            from.SendGump(
                                new SetListOptionGump(
                                    prop,
                                    from,
                                    m_Object,
                                    this,
                                    Enum.GetNames(type),
                                    GetObjects(Enum.GetValues(type))
                                )
                            );
                        }
                        else if (IsType(type, OfBool))
                        {
                            from.SendGump(
                                new SetListOptionGump(
                                    prop,
                                    from,
                                    m_Object,
                                    this,
                                    BoolNames,
                                    BoolValues
                                )
                            );
                        }
                        else if (IsType(type, OfPoison))
                        {
                            from.SendGump(
                                new SetListOptionGump(
                                    prop,
                                    from,
                                    m_Object,
                                    this,
                                    PoisonNames,
                                    PoisonValues
                                )
                            );
                        }
                        else if (IsType(type, OfMap))
                        {
                            from.SendGump(
                                new SetListOptionGump(
                                    prop,
                                    from,
                                    m_Object,
                                    this,
                                    Map.GetMapNames(),
                                    Map.GetMapValues().ToArray<object>()
                                )
                            );
                        }
                        else if (IsType(type, OfSkills) && m_Object is Mobile mobile)
                        {
                            from.SendGump(new PropertiesGump(from, mobile, m_Stack, m_List, m_Page));
                            from.SendGump(new SkillsGump(from, mobile));
                        }
                        else if (HasAttribute(type, OfPropertyObject, true))
                        {
                            from.SendGump(
                                obj != null
                                    ? new PropertiesGump(from, obj, m_Stack, new StackEntry(m_Object, prop))
                                    : new PropertiesGump(from, m_Object, m_Stack, m_List, m_Page)
                            );
                        }
                        else if (IsType(type, OfString) || IsParsable(type))
                        {
                            from.SendGump(new SetGump(prop, from, m_Object, this));
                        }

                        break;
                    }
            }
        }

        public virtual void SendPropertiesGump() =>
            m_Mobile.SendGump(new PropertiesGump(m_Mobile, m_Object, m_Stack, m_List, m_Page));

        private static object[] GetObjects(Array a)
        {
            var list = new object[a.Length];

            for (var i = 0; i < list.Length; ++i)
            {
                list[i] = a.GetValue(i);
            }

            return list;
        }

        private static bool IsCustomEnum(Type type) => type.IsDefined(OfCustomEnum, false);

        public void OnValueChanged(object obj, PropertyInfo prop)
        {
            if (m_Stack == null || m_Stack.Count == 0)
            {
                return;
            }

            if (!prop.PropertyType.IsValueType)
            {
                return;
            }

            var peek = m_Stack.Peek();

            if (peek.m_Property.CanWrite)
            {
                peek.m_Property.SetValue(peek.m_Object, obj, null);
            }
        }

        private static string[] GetCustomEnumNames(Type type)
        {
            var attrs = type.GetCustomAttributes(OfCustomEnum, false);

            if (attrs.Length == 0)
            {
                return [];
            }

            if (attrs[0] is not CustomEnumAttribute ce)
            {
                return [];
            }

            return ce.Names;
        }

        private static bool HasAttribute(Type type, Type check, bool inherit) =>
            type.GetCustomAttributes(check, inherit).Length > 0;

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

        private static string ValueToString(object o)
        {
            if (o == null)
            {
                return "-null-";
            }

            if (o is string s)
            {
                return $"\"{s}\"";
            }

            if (o is bool)
            {
                return o.ToString();
            }

            if (o is char c)
            {
                return $"0x{(int)c:X} '{c}'";
            }

            if (o is Serial serial)
            {
                if (serial.IsValid)
                {
                    if (serial.IsItem)
                    {
                        return $"(I) {serial}";
                    }

                    if (serial.IsMobile)
                    {
                        return $"(M) {serial}";
                    }
                }

                return $"(?) {serial}";
            }

            if (o is byte or sbyte or short or ushort or int or uint or long or ulong)
            {
                return $"{o} (0x{o:X})";
            }

            if (o is Mobile mobile)
            {
                return $"(M) {mobile.Serial} \"{mobile.Name}\"";
            }

            if (o is Item item)
            {
                return $"(I) {item.Serial}";
            }

            if (o is Type type)
            {
                return type.Name;
            }

            if (o is TextDefinition definition)
            {
                return definition.Format() ?? "-empty-";
            }

            return o.ToString();
        }

        private List<object> BuildList()
        {
            var list = new List<object>();

            if (m_Type == null)
            {
                return list;
            }

            var props = m_Type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

            var groups = GetGroups(m_Type, props);

            for (var i = 0; i < groups.Count; ++i)
            {
                var kvp = groups[i];

                if (!HasAttribute(kvp.Key, OfNoSort, false))
                {
                    kvp.Value.Sort(PropertySorter.Instance);
                }

                if (i != 0)
                {
                    list.Add(null);
                }

                list.Add(kvp.Key);

                foreach (var item in kvp.Value)
                {
                    if (ShowAttribute(item.Name))
                    {
                        list.Add(item);
                    }
                }
            }

            return list;
        }

        protected virtual bool ShowAttribute(string name) => true;

        private List<KeyValuePair<Type, List<PropertyInfo>>> GetGroups(Type objectType, PropertyInfo[] props)
        {
            var groups = new Dictionary<Type, List<PropertyInfo>>();

            for (var i = 0; i < props.Length; ++i)
            {
                var prop = props[i];

                if (prop.CanRead)
                {
                    var attr = GetCPA(prop);

                    if (attr != null && m_Mobile.AccessLevel >= attr.ReadLevel)
                    {
                        var type = prop.DeclaringType;

                        while (true)
                        {
                            var baseType = type?.BaseType;

                            if (baseType == OfObject || baseType?.GetProperty(prop.Name, prop.PropertyType) == null)
                            {
                                break;
                            }

                            type = baseType;
                        }

                        if (type != null)
                        {
                            if (groups.TryGetValue(type, out var result))
                            {
                                result.Add(prop);
                            }
                            else
                            {
                                groups[type] = [prop];
                            }
                        }
                    }
                }
            }

            var list = groups.ToList();
            list.Sort(new GroupComparer(objectType));

            return list;
        }

        private class PropertySorter : IComparer<PropertyInfo>
        {
            public static readonly PropertySorter Instance = new();

            private PropertySorter()
            {
            }

            public int Compare(PropertyInfo x, PropertyInfo y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                return x.Name.CompareOrdinal(y?.Name);
            }
        }

        private class GroupComparer : IComparer<KeyValuePair<Type, List<PropertyInfo>>>
        {
            private readonly Type m_Start;

            public GroupComparer(Type start) => m_Start = start;

            public int Compare(KeyValuePair<Type, List<PropertyInfo>> x, KeyValuePair<Type, List<PropertyInfo>> y) =>
                GetDistance(x.Key).CompareTo(GetDistance(y.Key));

            private int GetDistance(Type type)
            {
                var current = m_Start;

                int dist;

                for (dist = 0; current != null && current != OfObject && current != type; ++dist)
                {
                    current = current.BaseType;
                }

                return dist;
            }
        }
    }
}
