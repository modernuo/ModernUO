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
        var failure = Evaluate(prop, level, access);
        return failure == GuardFailure.None ? null : Describe(failure, prop, displayName);
    }

    // Callers that map failures to typed exceptions need the rule that failed, not the message text.
    internal static GuardFailure Evaluate(PropertyInfo prop, AccessLevel level, PropertyAccess access)
    {
        var attr = GetCPA(prop);

        if (attr == null)
        {
            return GuardFailure.NoAttribute;
        }

        if ((access & PropertyAccess.Read) != 0 && level < attr.ReadLevel)
        {
            return GuardFailure.ReadLevel;
        }

        if ((access & PropertyAccess.Write) != 0 && level < attr.WriteLevel)
        {
            return GuardFailure.WriteLevel;
        }

        if ((access & PropertyAccess.Read) != 0 && !prop.CanRead)
        {
            return GuardFailure.WriteOnly;
        }

        if ((access & PropertyAccess.Write) != 0 && (!prop.CanWrite && !attr.CanModify || attr.ReadOnly))
        {
            return GuardFailure.ReadOnly;
        }

        return GuardFailure.None;
    }

    private static string Describe(GuardFailure failure, PropertyInfo prop, string displayName)
    {
        var attr = GetCPA(prop);

        return failure switch
        {
            GuardFailure.NoAttribute => $"Property '{displayName}' not found.",
            GuardFailure.ReadLevel   => $"You must be at least {Mobile.GetAccessLevelName(attr.ReadLevel)} to get the property '{displayName}'.",
            GuardFailure.WriteLevel  => $"You must be at least {Mobile.GetAccessLevelName(attr.WriteLevel)} to set the property '{displayName}'.",
            GuardFailure.WriteOnly   => $"Property '{displayName}' is write only.",
            _                        => $"Property '{displayName}' is read only."
        };
    }

    /// <summary>
    /// Resolves a dotted binding. Intermediate links need Read; only the final link gets <paramref name="access"/>.
    /// </summary>
    public static PropertyInfo[] ResolveChain(
        Type type, string binding, AccessLevel level, PropertyAccess access, bool allowStatic, out string failReason
    )
    {
        var chain = ResolveChain(type, binding, level, access, allowStatic, out var failure, out var failed, out var name);
        failReason = failure switch
        {
            GuardFailure.None     => null,
            GuardFailure.NotFound => $"Property '{name}' not found.",
            _                     => Describe(failure, failed, name)
        };
        return chain;
    }

    internal static PropertyInfo[] ResolveChain(
        Type type, string binding, AccessLevel level, PropertyAccess access, bool allowStatic,
        out GuardFailure failure, out PropertyInfo failedProp, out string failedName
    )
    {
        failure = GuardFailure.None;
        failedProp = null;
        failedName = null;
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
                failure = GuardFailure.NotFound;
                failedName = name;
                return null;
            }

            failure = Evaluate(p, level, linkAccess);

            if (failure != GuardFailure.None)
            {
                failedProp = p;
                failedName = name;
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
        var flags = allowStatic ? StaticAndInstanceFlags : InstanceFlags;

        // A direct lookup avoids copying every property; it throws only when hiding or casing makes the name ambiguous.
        try
        {
            return type.GetProperty(name, flags | BindingFlags.IgnoreCase);
        }
        catch (AmbiguousMatchException)
        {
        }

        var props = type.GetProperties(flags);
        PropertyInfo best = null;

        for (var i = 0; i < props.Length; i++)
        {
            var p = props[i];

            if (!p.Name.InsensitiveEquals(name))
            {
                continue;
            }

            if (best == null || p.DeclaringType != null && p.DeclaringType.IsSubclassOf(best.DeclaringType))
            {
                best = p;
            }
        }

        return best;
    }
}

internal enum GuardFailure
{
    None,
    NotFound,
    NoAttribute,
    ReadLevel,
    WriteLevel,
    WriteOnly,
    ReadOnly
}
