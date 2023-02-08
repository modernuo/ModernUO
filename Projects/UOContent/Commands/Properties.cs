using System;
using System.Reflection;
using Server.Buffers;
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
                var ent = World.FindEntity((Serial)e.GetUInt32(0));

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

        public static PropertyInfo GetPropertyInfoByName(
            Mobile from, PropertyInfo[] props, string propertyName, PropertyAccess access, out string failReason
        )
        {
            for (var i = 0; i < props.Length; i++)
            {
                var p = props[i];

                if (!p.Name.InsensitiveEquals(propertyName))
                {
                    continue;
                }

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

                if ((access & PropertyAccess.Write) != 0 && (!p.CanWrite && !attr.CanModify || attr.ReadOnly))
                {
                    failReason = $"Property '{propertyName}' is read only.";
                    return null;
                }

                failReason = null;
                return p;
            }

            failReason = null;
            return null;
        }

        public static PropertyInfo[] GetPropertyInfoChain(
            Mobile from, Type type, string propertyString,
            PropertyAccess access, out string failReason
        )
        {
            failReason = null;
            var split = propertyString.Split('.');

            if (split.Length == 0)
            {
                return null;
            }

            var info = new PropertyInfo[split.Length];

            for (var i = 0; i < info.Length; ++i)
            {
                var propertyName = split[i];
                var props = type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

                var p = GetPropertyInfoByName(from, props, propertyName, access, out failReason);

                if (p == null)
                {
                    failReason ??= $"Property '{propertyName}' not found.";
                    return null;
                }

                info[i] = p;
                type = p.PropertyType;
            }

            return info;
        }

        public static PropertyInfo GetPropertyInfo(
            Mobile from, ref object obj, string propertyName, PropertyAccess access,
            out string failReason
        )
        {
            var chain = GetPropertyInfoChain(from, obj.GetType(), propertyName, access, out failReason);

            return chain == null ? null : GetPropertyInfo(ref obj, chain, out failReason);
        }

        public static PropertyInfo GetPropertyInfo(ref object obj, PropertyInfo[] chain, out string failReason)
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

            failReason = null;
            return chain[^1];
        }

        public static string GetValue(Mobile from, object o, string name)
        {
            var failReason = "";

            var chain = GetPropertyInfoChain(from, o.GetType(), name, PropertyAccess.Read, out failReason);

            if (chain == null || chain.Length == 0)
            {
                return failReason;
            }

            var p = GetPropertyInfo(ref o, chain, out failReason);

            return p == null ? failReason : InternalGetValue(o, p, chain);
        }

        public static string IncreaseValue(Mobile from, object o, string[] args)
        {
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

                realObjs[i] = o;
                realProps[i] = GetPropertyInfo(from, ref realObjs[i], name, PropertyAccess.ReadWrite, out var failReason);

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

                if (obj is not IConvertible)
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
                return positive ? "The property has been increased." : "The property has been decreased.";
            }

            if (positive && negative)
            {
                return "The properties have been changed.";
            }

            return positive ? "The properties have been increased." : "The properties have been decreased.";
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
                toString = ((TextDefinition)value).Format() ?? "empty";
            }
            else
            {
                toString = value.ToString();
            }

            if (chain == null)
            {
                return $"{p.Name} = {toString}";
            }

            using var builder = ValueStringBuilder.Create();
            for (var i = 0; i < chain.Length; i++)
            {
                builder.Append(chain[i].Name);
                if (i < chain.Length - 1)
                {
                    builder.Append(".");
                }
            }

            builder.Append(" = ");
            builder.Append(toString);

            return builder.ToString();
        }

        public static string SetValue(Mobile from, object o, string name, string value)
        {
            var logObject = o;

            var p = GetPropertyInfo(from, ref o, name, PropertyAccess.Write, out var failReason);

            return p == null ? failReason : InternalSetValue(from, logObject, o, p, name, value, true);
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
                    var reqLevel = newLevel switch
                    {
                        AccessLevel.Administrator => AccessLevel.Developer,
                        >= AccessLevel.Developer  => AccessLevel.Owner,
                        _                         => AccessLevel.Administrator
                    };

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

        public static string InternalSetValue(
            Mobile from, object logobj, object o, PropertyInfo p, string pname,
            string value, bool shouldLog
        ) =>
            TryParse(p.PropertyType, value, out var toSet) ??
            SetDirect(from, logobj, o, p, pname, toSet, shouldLog);

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
