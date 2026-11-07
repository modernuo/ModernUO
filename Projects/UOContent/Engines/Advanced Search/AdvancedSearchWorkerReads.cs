using System;
using System.Collections.Concurrent;
using System.Reflection;
using Server.Mobiles;

namespace Server.Engines.AdvancedSearch;

/// <summary>
/// Decides per runtime type whether the members the worker itself reads (to place, name and filter an
/// entity) are pure. Overrides are classified, so an impure override sends that type to the loop.
/// </summary>
public static class AdvancedSearchWorkerReads
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private static readonly ConcurrentDictionary<Type, bool> _memo = [];

    // Every entity member AdvancedSearchMatcher reads off the loop must be listed here (or in Evaluate/Extras),
    // or it runs unvetted on the workers. Add one whenever the matcher's worker path reads a new member.
    private static readonly string[] _itemProperties =
    [
        nameof(Item.Name), nameof(Item.ItemData), nameof(Item.Map), nameof(Item.Location),
        nameof(Item.RootParent), nameof(Item.Parent), nameof(Item.HeldBy), nameof(Item.Deleted)
    ];

    private static readonly string[] _mobileProperties =
        [nameof(Mobile.Name), nameof(Mobile.Map), nameof(Mobile.Location), nameof(Mobile.Deleted), nameof(Mobile.Account)];

    // Also read through IEntity, which dispatches via the runtime type's interface map.
    private static readonly string[] _entityProperties = [nameof(IEntity.Map), nameof(IEntity.Location), nameof(IEntity.Deleted)];

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

    // Looked up on the call site's static type: a subclass `new` member hides by name but never runs there.
    // The classifier follows overrides from there for the runtime type.
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

    // Declared on the interface or one it extends; the classifier maps it to the runtime implementation.
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
