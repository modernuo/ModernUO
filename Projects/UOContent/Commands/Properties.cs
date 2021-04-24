using System;
using System.Reflection;
using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Targeting;
using CPA = Server.CommandPropertyAttribute;

using static Server.Attributes;
using static Server.Types;

namespace Server.Commands
{
    [Flags]
    public enum PropertyAccess
    {
        Read = 0x01,
        Write = 0x02,
        ReadWrite = Read | Write
    }

    public static class Properties
    {
        private static readonly object[] m_ParseParams = new object[1];

        public static void Initialize()
        {
            CommandSystem.Register("Props", AccessLevel.Counselor, Props_OnCommand);
        }

        [Usage("Props [serial]")]
        [Description("Opens a menu where you can view and edit all properties of a targeted (or specified) object.")]
        private static void Props_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 1)
            {
                var ent = World.FindEntity(e.GetUInt32(0));

                if (ent == null)
                {
                    e.Mobile.SendMessage("No object with that serial was found.");
                }
                else if (!BaseCommand.IsAccessible(e.Mobile, ent))
                {
                    e.Mobile.SendLocalizedMessage(500447); // That is not accessible.
                }
                else
                {
                    e.Mobile.SendGump(new PropertiesGump(e.Mobile, ent));
                }
            }
            else
            {
                e.Mobile.Target = new PropsTarget();
            }
        }

        private static bool CIEqual(string l, string r) => l.InsensitiveEquals(r);

        public static PropertyInfo[] GetPropertyInfoChain(
            Mobile from, Type type, string propertyString,
            PropertyAccess endAccess, ref string failReason
        )
        {
            var split = propertyString.Split('.');

            if (split.Length == 0)
            {
                return null;
            }

            var info = new PropertyInfo[split.Length];

            for (var i = 0; i < info.Length; ++i)
            {
                var propertyName = split[i];

                if (CIEqual(propertyName, "current"))
                {
                    continue;
                }

                var props = type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

                var isFinal = i == info.Length - 1;

                var access = endAccess;

                if (!isFinal)
                {
                    access |= PropertyAccess.Read;
                }

                for (var j = 0; j < props.Length; ++j)
                {
                    var p = props[j];

                    if (CIEqual(p.Name, propertyName))
                    {
                        var attr = GetCPA(p);

                        if (attr == null)
                        {
                            failReason = $"Property '{propertyName}' not found.";
                            return null;
                        }

                        if ((access & PropertyAccess.Read) != 0 && from.AccessLevel < attr.ReadLevel)
                        {
                            failReason =
                                $"You must be at least {Mobile.GetAccessLevelName(attr.ReadLevel)} to get the property '{propertyName}'.";

                            return null;
                        }

                        if ((access & PropertyAccess.Write) != 0 && from.AccessLevel < attr.WriteLevel)
                        {
                            failReason =
                                $"You must be at least {Mobile.GetAccessLevelName(attr.WriteLevel)} to set the property '{propertyName}'.";

                            return null;
                        }

                        if ((access & PropertyAccess.Read) != 0 && !p.CanRead)
                        {
                            failReason = $"Property '{propertyName}' is write only.";
                            return null;
                        }

                        if ((access & PropertyAccess.Write) != 0 && (!p.CanWrite || attr.ReadOnly) && isFinal)
                        {
                            failReason = $"Property '{propertyName}' is read only.";
                            return null;
                        }

                        info[i] = p;
                        type = p.PropertyType;
                        break;
                    }
                }

                if (info[i] == null)
                {
                    failReason = $"Property '{propertyName}' not found.";
                    return null;
                }
            }

            return info;
        }

        public static PropertyInfo GetPropertyInfo(
            Mobile from, ref object obj, string propertyName, PropertyAccess access,
            ref string failReason
        )
        {
            var chain = GetPropertyInfoChain(from, obj.GetType(), propertyName, access, ref failReason);

            return chain == null ? null : GetPropertyInfo(ref obj, chain, ref failReason);
        }

        public static PropertyInfo GetPropertyInfo(ref object obj, PropertyInfo[] chain, ref string failReason)
        {
            if (chain == null || chain.Length == 0)
            {
                failReason = "Property chain is empty.";
                return null;
            }

            for (var i = 0; i < chain.Length - 1; ++i)
            {
                if (chain[i] == null)
                {
                    continue;
                }

                obj = chain[i].GetValue(obj, null);

                if (obj == null)
                {
                    failReason = $"Property '{chain[i]}' is null.";
                    return null;
                }
            }

            return chain[^1];
        }

        public static string GetValue(Mobile from, object o, string name)
        {
            var failReason = "";

            var chain = GetPropertyInfoChain(from, o.GetType(), name, PropertyAccess.Read, ref failReason);

            if (chain == null || chain.Length == 0)
            {
                return failReason;
            }

            var p = GetPropertyInfo(ref o, chain, ref failReason);

            return p == null ? failReason : InternalGetValue(o, p, chain);
        }

        public static string IncreaseValue(Mobile from, object o, string[] args)
        {
            // Type type = o.GetType();

            var realObjs = new object[args.Length / 2];
            var realProps = new PropertyInfo[args.Length / 2];
            var realValues = new int[args.Length / 2];

            var positive = false;
            var negative = false;

            for (var i = 0; i < realProps.Length; ++i)
            {
                var name = args[i * 2];

                try
                {
                    var valueString = args[1 + i * 2];

                    if (valueString.StartsWithOrdinal("0x"))
                    {
                        realValues[i] = Convert.ToInt32(valueString[2..], 16);
                    }
                    else
                    {
                        realValues[i] = Convert.ToInt32(valueString);
                    }
                }
                catch
                {
                    return "Offset value could not be parsed.";
                }

                if (realValues[i] > 0)
                {
                    positive = true;
                }
                else if (realValues[i] < 0)
                {
                    negative = true;
                }
                else
                {
                    return "Zero is not a valid value to offset.";
                }

                string failReason = null;
                realObjs[i] = o;
                realProps[i] = GetPropertyInfo(from, ref realObjs[i], name, PropertyAccess.ReadWrite, ref failReason);

                if (failReason != null)
                {
                    return failReason;
                }

                if (realProps[i] == null)
                {
                    return "Property not found.";
                }
            }

            for (var i = 0; i < realProps.Length; ++i)
            {
                var obj = realProps[i].GetValue(realObjs[i], null);

                if (!(obj is IConvertible))
                {
                    return "Property is not IConvertable.";
                }

                try
                {
                    var v = (long)Convert.ChangeType(obj, TypeCode.Int64);
                    v += realValues[i];

                    realProps[i].SetValue(realObjs[i], Convert.ChangeType(v, realProps[i].PropertyType), null);
                }
                catch
                {
                    return "Value could not be converted";
                }
            }

            if (realProps.Length == 1)
            {
                if (positive)
                {
                    return "The property has been increased.";
                }

                return "The property has been decreased.";
            }

            if (positive && negative)
            {
                return "The properties have been changed.";
            }

            if (positive)
            {
                return "The properties have been increased.";
            }

            return "The properties have been decreased.";
        }

        private static string InternalGetValue(object o, PropertyInfo p, PropertyInfo[] chain = null)
        {
            var type = p.PropertyType;

            var value = p.GetValue(o, null);
            string toString;

            if (value == null)
            {
                toString = "null";
            }
            else if (IsNumeric(type))
            {
                toString = $"{value} (0x{value:X})";
            }
            else if (IsChar(type))
            {
                toString = $"'{value}' ({(int)value} [0x{(int)value:X}])";
            }
            else if (IsString(type))
            {
                toString = (string)value == "null" ? @"@""null""" : $"\"{value}\"";
            }
            else if (IsText(type))
            {
                toString = ((TextDefinition)value).Format(false);
            }
            else
            {
                toString = value.ToString();
            }

            if (chain == null)
            {
                return $"{p.Name} = {toString}";
            }

            var concat = new string[chain.Length * 2 + 1];

            for (var i = 0; i < chain.Length; ++i)
            {
                concat[i * 2 + 0] = chain[i].Name;
                concat[i * 2 + 1] = i < chain.Length - 1 ? "." : " = ";
            }

            concat[^1] = toString;

            return string.Concat(concat);
        }

        public static string SetValue(Mobile from, object o, string name, string value)
        {
            var logObject = o;

            var failReason = "";
            var p = GetPropertyInfo(from, ref o, name, PropertyAccess.Write, ref failReason);

            return p == null ? failReason : InternalSetValue(from, logObject, o, p, name, value, true);
        }

        private static object Parse(object o, Type t, string value)
        {
            var method = t.GetMethod("Parse", ParseTypes);

            m_ParseParams[0] = value;

            return method?.Invoke(o, m_ParseParams);
        }

        public static string ConstructFromString(Type type, object obj, string value, ref object constructed)
        {
            object toSet;
            var isSerial = IsSerial(type);

            if (isSerial) // mutate into int32
            {
                type = OfInt;
            }

            if (value == "(-null-)" && !type.IsValueType)
            {
                value = null;
            }

            if (IsEnum(type))
            {
                try
                {
                    toSet = Enum.Parse(type, value ?? "", true);
                }
                catch
                {
                    return "That is not a valid enumeration member.";
                }
            }
            else if (IsType(type))
            {
                try
                {
                    toSet = AssemblyHandler.FindTypeByName(value);

                    if (toSet == null)
                    {
                        return "No type with that name was found.";
                    }
                }
                catch
                {
                    return "No type with that name was found.";
                }
            }
            else if (IsParsable(type))
            {
                try
                {
                    toSet = Parse(obj, type, value);
                }
                catch
                {
                    return "That is not properly formatted.";
                }
            }
            else if (value == null)
            {
                toSet = null;
            }
            else if (value.StartsWithOrdinal("0x") && IsNumeric(type))
            {
                try
                {
                    toSet = Convert.ChangeType(Convert.ToUInt64(value[2..], 16), type);
                }
                catch
                {
                    return "That is not properly formatted.";
                }
            }
            else
            {
                try
                {
                    toSet = Convert.ChangeType(value, type);
                }
                catch
                {
                    return "That is not properly formatted.";
                }
            }

            if (isSerial) // mutate back
            {
                toSet = (Serial)(toSet ?? Serial.MinusOne);
            }

            constructed = toSet;
            return null;
        }

        public static string SetDirect(
            Mobile from, object logObject, object obj, PropertyInfo prop, string givenName,
            object toSet, bool shouldLog
        )
        {
            try
            {
                if (toSet is AccessLevel newLevel)
                {
                    var reqLevel = AccessLevel.Administrator;

                    if (newLevel == AccessLevel.Administrator)
                    {
                        reqLevel = AccessLevel.Developer;
                    }
                    else if (newLevel >= AccessLevel.Developer)
                    {
                        reqLevel = AccessLevel.Owner;
                    }

                    if (from.AccessLevel < reqLevel)
                    {
                        return "You do not have access to that level.";
                    }
                }

                if (shouldLog)
                {
                    CommandLogging.LogChangeProperty(
                        from,
                        logObject,
                        givenName,
                        toSet?.ToString() ?? "(-null-)"
                    );
                }

                prop.SetValue(obj, toSet, null);
                return "Property has been set.";
            }
            catch
            {
                return "An exception was caught, the property may not be set.";
            }
        }

        public static string SetDirect(object obj, PropertyInfo prop, object toSet)
        {
            try
            {
                if (toSet is AccessLevel)
                {
                    return "You do not have access to that level.";
                }

                prop.SetValue(obj, toSet, null);
                return "Property has been set.";
            }
            catch
            {
                return "An exception was caught, the property may not be set.";
            }
        }

        public static string InternalSetValue(
            Mobile from, object logobj, object o, PropertyInfo p, string pname,
            string value, bool shouldLog
        )
        {
            object toSet = null;
            var result = ConstructFromString(p.PropertyType, o, value, ref toSet);

            return result ?? SetDirect(from, logobj, o, p, pname, toSet, shouldLog);
        }

        public static string InternalSetValue(object o, PropertyInfo p, string value)
        {
            object toSet = null;
            var result = ConstructFromString(p.PropertyType, o, value, ref toSet);

            return result ?? SetDirect(o, p, toSet);
        }


        private class PropsTarget : Target
        {
            public PropsTarget() : base(-1, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (!BaseCommand.IsAccessible(from, o))
                {
                    from.SendLocalizedMessage(500447); // That is not accessible.
                }
                else
                {
                    from.SendGump(new PropertiesGump(from, o));
                }
            }
        }
    }
}

namespace Server
{
    public sealed class Property
    {
        private PropertyInfo[] m_Chain;

        public Property(string binding) => Binding = binding;

        public Property(PropertyInfo[] chain) => m_Chain = chain;

        public string Binding { get; }

        public bool IsBound => m_Chain != null;

        public PropertyAccess Access { get; private set; }

        private void NotYetBound() => throw new NotYetBoundException(this);

        public PropertyInfo[] Chain
        {
            get
            {
                if (!IsBound)
                {
                    NotYetBound();
                }

                return m_Chain;
            }
        }

        public Type Type
        {
            get
            {
                if (!IsBound)
                {
                    NotYetBound();
                }

                return m_Chain[^1].PropertyType;
            }
        }

        public bool CheckAccess(Mobile from)
        {
            if (!IsBound)
            {
                throw new NotYetBoundException(this);
            }

            for (var i = 0; i < m_Chain.Length; ++i)
            {
                var prop = m_Chain[i];

                var isFinal = i == m_Chain.Length - 1;

                var access = Access;

                if (!isFinal)
                {
                    access |= PropertyAccess.Read;
                }

                var security = GetCPA(prop);

                if (security == null)
                {
                    throw new InternalAccessException(this);
                }

                if ((access & PropertyAccess.Read) != 0 && from.AccessLevel < security.ReadLevel)
                {
                    throw new ReadAccessException(this, from.AccessLevel, security.ReadLevel);
                }

                if ((access & PropertyAccess.Write) != 0 && (from.AccessLevel < security.WriteLevel || security.ReadOnly))
                {
                    throw new WriteAccessException(this, from.AccessLevel, security.ReadLevel);
                }
            }

            return true;
        }

        public void BindTo(Type objectType, PropertyAccess desiredAccess)
        {
            if (IsBound)
            {
                throw new AlreadyBoundException(this);
            }

            var split = Binding.Split('.');

            var chain = new PropertyInfo[split.Length];

            for (var i = 0; i < split.Length; ++i)
            {
                var isFinal = i == chain.Length - 1;

                chain[i] = objectType.GetProperty(
                    split[i],
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase
                );

                if (chain[i] == null)
                {
                    throw new UnknownPropertyException(this, split[i]);
                }

                objectType = chain[i].PropertyType;

                var access = desiredAccess;

                if (!isFinal)
                {
                    access |= PropertyAccess.Read;
                }

                if ((access & PropertyAccess.Read) != 0 && !chain[i].CanRead)
                {
                    throw new WriteOnlyException(this);
                }

                if ((access & PropertyAccess.Write) != 0 && !chain[i].CanWrite)
                {
                    throw new ReadOnlyException(this);
                }
            }

            Access = desiredAccess;
            m_Chain = chain;
        }

        public override string ToString()
        {
            if (!IsBound)
            {
                return Binding;
            }

            var toJoin = new string[m_Chain.Length];

            for (var i = 0; i < toJoin.Length; ++i)
            {
                toJoin[i] = m_Chain[i].Name;
            }

            return string.Join(".", toJoin);
        }

        public static Property Parse(Type type, string binding, PropertyAccess access)
        {
            var prop = new Property(binding);

            prop.BindTo(type, access);

            return prop;
        }
    }
}
