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

    // The matcher also reads these through IEntity, which dispatches through the runtime type's interface map.
    private static readonly string[] _entityProperties = ["Map", "Location", "Deleted"];

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
            return AllProperties(typeof(Item), type, _itemProperties) && EntityProperties(type) &&
                   Method(typeof(Item), type, nameof(Item.GetWorldLocation)) && Extras(type);
        }

        if (typeof(Mobile).IsAssignableFrom(type))
        {
            return AllProperties(typeof(Mobile), type, _mobileProperties) && EntityProperties(type) && Extras(type);
        }

        return false;
    }

    private static bool Extras(Type type)
    {
        if (typeof(BaseCreature).IsAssignableFrom(type) &&
            !Property(typeof(BaseCreature), type, nameof(BaseCreature.IsStabled)))
        {
            return false;
        }

        if (typeof(PlayerVendor).IsAssignableFrom(type) &&
            !Property(typeof(PlayerVendor), type, nameof(PlayerVendor.House)))
        {
            return false;
        }

        return !typeof(IMount).IsAssignableFrom(type) || InterfaceProperty(typeof(IMount), type, nameof(IMount.Rider));
    }

    private static bool AllProperties(Type callSiteType, Type runtimeType, string[] names)
    {
        for (var i = 0; i < names.Length; i++)
        {
            if (!Property(callSiteType, runtimeType, names[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EntityProperties(Type runtimeType)
    {
        for (var i = 0; i < _entityProperties.Length; i++)
        {
            if (!InterfaceProperty(typeof(IEntity), runtimeType, _entityProperties[i]))
            {
                return false;
            }
        }

        return true;
    }

    // The getter is looked up on the type the matcher's call site is typed as, not the runtime type: a `new`
    // member on a subclass hides by name but is never what that call site runs. The classifier then follows
    // the override chain from there for the runtime type.
    private static bool Property(Type callSiteType, Type runtimeType, string name)
    {
        for (var t = callSiteType; t != null; t = t.BaseType)
        {
            var property = t.GetProperty(name, Declared);
            if (property != null)
            {
                var getter = property.GetGetMethod(true);
                return getter != null && PureGetterClassifier.IsPure(getter, runtimeType);
            }
        }

        return false;
    }

    private static bool Method(Type callSiteType, Type runtimeType, string name)
    {
        for (var t = callSiteType; t != null; t = t.BaseType)
        {
            var method = t.GetMethod(name, Declared, Type.EmptyTypes);
            if (method != null)
            {
                return PureGetterClassifier.IsPure(method, runtimeType);
            }
        }

        return false;
    }

    // Interface members are declared on the interface or one it extends; the classifier maps the getter to
    // the runtime type's implementation.
    private static bool InterfaceProperty(Type interfaceType, Type runtimeType, string name)
    {
        var property = interfaceType.GetProperty(name);
        if (property == null)
        {
            var inherited = interfaceType.GetInterfaces();
            for (var i = 0; i < inherited.Length && property == null; i++)
            {
                property = inherited[i].GetProperty(name);
            }
        }

        var getter = property?.GetGetMethod();
        return getter != null && PureGetterClassifier.IsPure(getter, runtimeType);
    }
}
