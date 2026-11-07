using System;
using System.Reflection;
using Server.Text;
using static Server.Attributes;

namespace Server.Commands;

/// <summary>
/// The single AccessLevel-based check for staff access to [CommandProperty] members.
/// </summary>
public static class CommandPropertyGuard
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public;
    private const BindingFlags StaticAndInstanceFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;

    /// <summary>Returns null when <paramref name="level"/> may perform <paramref name="access"/> on the property.</summary>
    public static string Check(PropertyInfo prop, AccessLevel level, PropertyAccess access) =>
        Check(prop, prop.Name, level, access);

    internal static string Check(PropertyInfo prop, string displayName, AccessLevel level, PropertyAccess access)
    {
        var attr = GetCPA(prop);

        if (attr == null)
        {
            return $"Property '{displayName}' not found.";
        }

        if ((access & PropertyAccess.Read) != 0 && level < attr.ReadLevel)
        {
            return $"You must be at least {Mobile.GetAccessLevelName(attr.ReadLevel)} to get the property '{displayName}'.";
        }

        if ((access & PropertyAccess.Write) != 0 && level < attr.WriteLevel)
        {
            return $"You must be at least {Mobile.GetAccessLevelName(attr.WriteLevel)} to set the property '{displayName}'.";
        }

        if ((access & PropertyAccess.Read) != 0 && !prop.CanRead)
        {
            return $"Property '{displayName}' is write only.";
        }

        if ((access & PropertyAccess.Write) != 0 && (!prop.CanWrite && !attr.CanModify || attr.ReadOnly))
        {
            return $"Property '{displayName}' is read only.";
        }

        return null;
    }

    /// <summary>
    /// Resolves a dotted binding. Intermediate links need Read; only the final link gets <paramref name="access"/>.
    /// </summary>
    public static PropertyInfo[] ResolveChain(
        Type type, string binding, AccessLevel level, PropertyAccess access, bool allowStatic, out string failReason
    )
    {
        failReason = null;
        var split = binding.Split('.');

        if (split.Length == 0)
        {
            return null;
        }

        var info = new PropertyInfo[split.Length];

        for (var i = 0; i < info.Length; ++i)
        {
            var name = split[i];
            var linkAccess = i == info.Length - 1 ? access : PropertyAccess.Read;
            var p = FindMostDerived(type, name, allowStatic);

            if (p == null)
            {
                failReason = $"Property '{name}' not found.";
                return null;
            }

            failReason = Check(p, name, level, linkAccess);

            if (failReason != null)
            {
                return null;
            }

            info[i] = p;
            type = p.PropertyType;
        }

        return info;
    }

    // A `new`-hidden member is reported once per declaring type; the most derived one is the one callers mean.
    private static PropertyInfo FindMostDerived(Type type, string name, bool allowStatic)
    {
        var props = type.GetProperties(allowStatic ? StaticAndInstanceFlags : InstanceFlags);
        PropertyInfo best = null;

        for (var i = 0; i < props.Length; i++)
        {
            var p = props[i];

            if (!p.Name.InsensitiveEquals(name))
            {
                continue;
            }

            if (best == null || p.DeclaringType != null && p.DeclaringType.IsSubclassOf(best.DeclaringType!))
            {
                best = p;
            }
        }

        return best;
    }
}
