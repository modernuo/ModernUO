using System;
using System.Reflection;
using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Targeting;
using Server.Text;
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

        public static void Configure()
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
        ) => GetPropertyInfoByName(from.AccessLevel, props, propertyName, access, out failReason);

        public static PropertyInfo GetPropertyInfoByName(
            AccessLevel level, PropertyInfo[] props, string propertyName, PropertyAccess access, out string failReason
        )
        {
            for (var i = 0; i < props.Length; i++)
            {
                var p = props[i];

                if (!p.Name.InsensitiveEquals(propertyName))
                {
                    continue;
                }

                failReason = CommandPropertyGuard.Check(p, propertyName, level, access);
                return failReason == null ? p : null;
            }

            failReason = null;
            return null;
        }

        public static PropertyInfo[] GetPropertyInfoChain(
            Mobile from, Type type, string propertyString,
            PropertyAccess access, out string failReason
        ) => GetPropertyInfoChain(from.AccessLevel, type, propertyString, access, out failReason);

        public static PropertyInfo[] GetPropertyInfoChain(
            AccessLevel level, Type type, string propertyString,
            PropertyAccess access, out string failReason
        ) => CommandPropertyGuard.ResolveChain(type, propertyString, level, access, true, out failReason);

        public static PropertyInfo GetPropertyInfo(
            Mobile from, ref object obj, string propertyName, PropertyAccess access,
            out string failReason
        )
        {
            var chain = GetPropertyInfoChain(from, obj.GetType(), propertyName, access, out failReason);

            return chain == null ? null : GetPropertyInfo(ref obj, chain, out failReason);
        }

        public static PropertyInfo GetPropertyInfo(ref object obj, PropertyInfo[] chain, out string failReason) =>
            WalkChain(null, ref obj, chain, out failReason);

        // When writer is set, the walk refuses to pass through anything a higher privileged mobile owns.
        private static PropertyInfo WalkChain(Mobile writer, ref object obj, PropertyInfo[] chain, out string failReason)
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

                if (writer != null && IsHigherPrivileged(writer, obj))
                {
                    failReason = HigherPrivilegedMessage;
                    return null;
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
            var count = args.Length / 2;
            var realObjs = new object[count];
            var realProps = new PropertyInfo[count];
            var realValues = new object[count];

            var positive = false;
            var negative = false;

            // Every increment is resolved, converted and authorized before any is applied, so a refusal applies none.
            for (var i = 0; i < count; ++i)
            {
                var name = args[i * 2];
                int offset;

                try
                {
                    var valueString = args[1 + i * 2];

                    if (valueString.StartsWithOrdinal("0x"))
                    {
                        offset = Convert.ToInt32(valueString[2..], 16);
                    }
                    else
                    {
                        offset = Convert.ToInt32(valueString);
                    }
                }
                catch
                {
                    return "Offset value could not be parsed.";
                }

                if (offset > 0)
                {
                    positive = true;
                }
                else if (offset < 0)
                {
                    negative = true;
                }
                else
                {
                    return "Zero is not a valid value to offset.";
                }

                var obj = o;
                var chain = GetPropertyInfoChain(from, obj.GetType(), name, PropertyAccess.ReadWrite, out var failReason);
                var prop = chain == null ? null : WalkChain(from, ref obj, chain, out failReason);

                if (failReason != null)
                {
                    return failReason;
                }

                if (prop == null)
                {
                    return "Property not found.";
                }

                var current = prop.GetValue(obj, null);

                if (current is not IConvertible)
                {
                    return "Property is not IConvertable.";
                }

                object toSet;

                try
                {
                    var v = (long)Convert.ChangeType(current, TypeCode.Int64) + offset;
                    toSet = Convert.ChangeType(v, prop.PropertyType);
                }
                catch
                {
                    return "Value could not be converted";
                }

                var refusal = CheckWrite(from, obj, toSet);

                if (refusal != null)
                {
                    return refusal;
                }

                realObjs[i] = obj;
                realProps[i] = prop;
                realValues[i] = toSet;
            }

            for (var i = 0; i < count; ++i)
            {
                var result = SetDirect(from, o, realObjs[i], realProps[i], args[i * 2], realValues[i], true);

                if (result != SetSuccess)
                {
                    return result;
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

            var chain = GetPropertyInfoChain(from, o.GetType(), name, PropertyAccess.Write, out var failReason);

            if (chain == null)
            {
                return failReason;
            }

            var p = WalkChain(from, ref o, chain, out failReason);

            return p == null ? failReason : InternalSetValue(from, logObject, o, p, name, value, true);
        }

        private const string HigherPrivilegedMessage = "You cannot modify a higher privileged mobile.";

        /// <summary>
        /// True when <paramref name="obj"/> is, or is an item held by, a mobile whose own or account
        /// AccessLevel exceeds <paramref name="from"/>'s.
        /// </summary>
        internal static bool IsHigherPrivileged(Mobile from, object obj)
        {
            var mob = obj switch
            {
                Mobile m => m,
                Item item => item.RootParent as Mobile,
                _ => null
            };

            return mob != null &&
                   (mob.AccessLevel > from.AccessLevel || mob.Account?.AccessLevel > from.AccessLevel);
        }

        // Returns null when from may write toSet onto obj; SetDirect applies the same check before every write.
        private static string CheckWrite(Mobile from, object obj, object toSet)
        {
            if (IsHigherPrivileged(from, obj))
            {
                return HigherPrivilegedMessage;
            }

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

            return null;
        }

        public static string SetDirect(
            Mobile from, object logObject, object obj, PropertyInfo prop, string givenName,
            object toSet, bool shouldLog
        )
        {
            try
            {
                var refusal = CheckWrite(from, obj, toSet);

                if (refusal != null)
                {
                    return refusal;
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
                return SetSuccess;
            }
            catch
            {
                return "An exception was caught, the property may not be set.";
            }
        }

        // Single entry point for props-gump writes so they share SetDirect's guards and logging.
        internal static bool ApplyFromGump(
            Mobile from, object obj, PropertyInfo prop, object toSet, PropertiesGump gump
        )
        {
            var result = SetDirect(from, obj, obj, prop, prop.Name, toSet, true);
            from.SendMessage(result);

            if (result != SetSuccess)
            {
                return false;
            }

            gump.OnValueChanged(obj, prop);
            return true;
        }

        private const string SetSuccess = "Property has been set.";

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

        public void BindTo(Type objectType, PropertyAccess desiredAccess, AccessLevel accessLevel)
        {
            if (IsBound)
            {
                throw new AlreadyBoundException(this);
            }

            var chain = CommandPropertyGuard.ResolveChain(
                objectType, Binding, accessLevel, desiredAccess, false, out var failure, out var failed, out var name
            );

            switch (failure)
            {
                case GuardFailure.NotFound:
                    {
                        throw new UnknownPropertyException(this, name);
                    }
                case GuardFailure.NoAttribute:
                    {
                        throw new InternalAccessException(this);
                    }
                case GuardFailure.ReadLevel:
                    {
                        throw new ReadAccessException(this, accessLevel, GetCPA(failed).ReadLevel);
                    }
                case GuardFailure.WriteLevel:
                    {
                        throw new WriteAccessException(this, accessLevel, GetCPA(failed).WriteLevel);
                    }
                case GuardFailure.WriteOnly:
                    {
                        throw new WriteOnlyException(this);
                    }
                case GuardFailure.ReadOnly:
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

        public static Property Parse(Type type, string binding, PropertyAccess access, AccessLevel accessLevel)
        {
            var prop = new Property(binding);

            prop.BindTo(type, access, accessLevel);

            return prop;
        }
    }
}
