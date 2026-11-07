using System;
using System.Collections.Concurrent;
using System.Reflection;
using Server.Mobiles;

namespace Server.Engines.AdvancedSearch;

/// <summary>
/// Decides per runtime entity type whether the members the search worker itself reads (to place, name and
/// filter an entity) are pure for that type. Overrides are what get classified, so a content subclass with an
/// impure override opts its instances out of off-loop evaluation instead of being read unsafely.
/// </summary>
public static class AdvancedSearchWorkerReads
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static readonly ConcurrentDictionary<Type, bool> _memo = new();

    private static readonly string[] _itemProperties =
        ["Name", "ItemData", "Map", "Location", "RootParent", "Parent", "HeldBy", "Deleted"];

    private static readonly string[] _mobileProperties = ["Name", "Map", "Location", "Deleted", "Account"];

    public static bool IsWorkerSafe(Type runtimeType)
    {
        if (runtimeType == null)
        {
            return false;
        }

        return _memo.GetOrAdd(runtimeType, Evaluate);
    }

    private static bool Evaluate(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
        {
            return false;
        }

        if (typeof(Item).IsAssignableFrom(type))
        {
            return AllProperties(type, _itemProperties) && Method(type, nameof(Item.GetWorldLocation)) &&
                   Extras(type);
        }

        if (typeof(Mobile).IsAssignableFrom(type))
        {
            return AllProperties(type, _mobileProperties) && Extras(type);
        }

        return false;
    }

    private static bool Extras(Type type)
    {
        if (typeof(BaseCreature).IsAssignableFrom(type) && !Property(type, nameof(BaseCreature.IsStabled)))
        {
            return false;
        }

        if (typeof(PlayerVendor).IsAssignableFrom(type) && !Property(type, nameof(PlayerVendor.House)))
        {
            return false;
        }

        return !typeof(IMount).IsAssignableFrom(type) || InterfaceGetter(type);
    }

    private static bool AllProperties(Type type, string[] names)
    {
        for (var i = 0; i < names.Length; i++)
        {
            if (!Property(type, names[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Property(Type type, string name)
    {
        for (var t = type; t != null; t = t.BaseType)
        {
            var property = t.GetProperty(name, Declared);
            if (property != null)
            {
                var getter = property.GetGetMethod(true);
                return getter != null && PureGetterClassifier.IsPure(getter, type);
            }
        }

        return false;
    }

    private static bool Method(Type type, string name)
    {
        for (var t = type; t != null; t = t.BaseType)
        {
            var method = t.GetMethod(name, Declared, Type.EmptyTypes);
            if (method != null)
            {
                return PureGetterClassifier.IsPure(method, type);
            }
        }

        return false;
    }

    private static bool InterfaceGetter(Type type)
    {
        var getter = typeof(IMount).GetProperty(nameof(IMount.Rider))?.GetGetMethod();
        if (getter == null)
        {
            return false;
        }

        var map = type.GetInterfaceMap(typeof(IMount));
        for (var i = 0; i < map.InterfaceMethods.Length; i++)
        {
            if (map.InterfaceMethods[i] == getter)
            {
                return PureGetterClassifier.IsPure(map.TargetMethods[i], type);
            }
        }

        return false;
    }
}
