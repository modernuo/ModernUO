using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Multis;

namespace Server.Engines.AdvancedSearch;

internal enum MatchOutcome
{
    Reject,
    Match,
    Defer
}

/// <summary>
/// The search filter, shared by the search workers and the loop phase. Off the loop
/// (<c>onLoop: false</c>) it runs only reads proven pure for the entity's runtime type and returns
/// <see cref="MatchOutcome.Defer" /> for anything else: a type whose fixed reads are not worker-safe, a
/// property test that needs its loop predicate, and the region filter (<c>Region.Find</c> lazily
/// allocates map sectors). On the loop it runs the whole filter and never defers.
/// </summary>
internal static class AdvancedSearchMatcher
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(AdvancedSearchMatcher));

    internal static MatchOutcome Evaluate(
        IEntity entity,
        AdvancedSearchFilter filter,
        WorldLocation origin,
        AdvancedSearchConditions.Cache predicates,
        bool onLoop,
        out AdvancedSearchResult result
    )
    {
        result = null;

        if (entity == null || entity.Deleted || filter == null)
        {
            return MatchOutcome.Reject;
        }

        try
        {
            return EvaluateCore(entity, filter, origin, predicates, onLoop, out result);
        }
        catch (Exception ex)
        {
            result = null;
            _logger.Warning(ex, "AdvancedSearch: filter threw for {Entity}; skipping", entity);
            return MatchOutcome.Reject;
        }
    }

    private static MatchOutcome EvaluateCore(
        IEntity entity,
        AdvancedSearchFilter filter,
        WorldLocation origin,
        AdvancedSearchConditions.Cache predicates,
        bool onLoop,
        out AdvancedSearchResult result
    )
    {
        result = null;

        // The map, location, name and internal-validity reads below are the fixed reads vetted per type.
        if (!onLoop && !AdvancedSearchWorkerReads.IsWorkerSafe(entity.GetType()))
        {
            WarmPredicate(entity.GetType(), filter, predicates);
            return MatchOutcome.Defer;
        }

        var item = entity as Item;
        var mobile = entity as Mobile;

        if (item == null && mobile == null)
        {
            return MatchOutcome.Reject;
        }

        if (!PassesMapAndRange(entity, filter, origin, out var location))
        {
            return MatchOutcome.Reject;
        }

        var defer = false;

        if (filter.FilterRegion)
        {
            if (string.IsNullOrWhiteSpace(filter.RegionName))
            {
                return MatchOutcome.Reject;
            }

            if (!onLoop)
            {
                defer = true;
            }
            else if (!Region.Find(location, entity.Map).IsPartOf(filter.RegionName))
            {
                return MatchOutcome.Reject;
            }
        }

        if (filter.FilterName && !string.IsNullOrWhiteSpace(filter.Name))
        {
            var name = item != null ? item.Name ?? item.ItemData.Name : mobile.Name;
            if (!name.InsensitiveContains(filter.Name))
            {
                return MatchOutcome.Reject;
            }
        }

        if (filter.HideValidInternalMap && entity.Map == Map.Internal &&
            (item != null ? !IsValidInternal(item) : !IsValidInternal(mobile)))
        {
            return MatchOutcome.Reject;
        }

        if (filter.FilterPropertyTest)
        {
            var test = filter.PropertyTest;

            if (string.IsNullOrWhiteSpace(test))
            {
                return MatchOutcome.Reject;
            }

            // Compiled once per runtime type for the search and memoized; after that each entity costs a
            // dictionary lookup and a delegate call.
            var predicate = AdvancedSearchConditions.GetPredicate(predicates, entity.GetType(), test);

            if (onLoop)
            {
                if (!(predicate.Loop ?? predicate.Worker)(entity))
                {
                    return MatchOutcome.Reject;
                }
            }
            else if (!predicate.Worker(entity))
            {
                // Worker is a necessary condition, so a false here is a true rejection.
                return MatchOutcome.Reject;
            }
            else if (predicate.NeedsLoop)
            {
                defer = true;
            }
        }

        if (defer)
        {
            return MatchOutcome.Defer;
        }

        result = item != null
            ? new AdvancedSearchResult(item.Name ?? item.ItemData.Name, item.GetType(), item.Location, item.Map, item.RootParent)
            {
                Entity = item
            }
            : new AdvancedSearchResult(mobile.Name, mobile.GetType(), mobile.Location, mobile.Map, null)
            {
                Entity = mobile
            };

        return MatchOutcome.Match;
    }

    // Compiling a predicate is reflection and Expression.Compile, with no game state, so it is done here on the
    // worker rather than in the loop's drain. A failure only means the loop compiles it instead.
    private static void WarmPredicate(Type runtimeType, AdvancedSearchFilter filter, AdvancedSearchConditions.Cache predicates)
    {
        if (!filter.FilterPropertyTest || string.IsNullOrWhiteSpace(filter.PropertyTest) || predicates == null)
        {
            return;
        }

        try
        {
            AdvancedSearchConditions.GetPredicate(predicates, runtimeType, filter.PropertyTest);
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "AdvancedSearch: precompiling the predicate for {Type} failed", runtimeType);
        }
    }

    /// <summary>
    /// The placement filters an ignore source must pass before it hides anything: map, range and, on
    /// the loop, region. Loop only.
    /// </summary>
    internal static bool PassesPlacement(IEntity entity, AdvancedSearchFilter filter, WorldLocation origin)
    {
        if (!PassesMapAndRange(entity, filter, origin, out var location))
        {
            return false;
        }

        return !filter.FilterRegion ||
               !string.IsNullOrWhiteSpace(filter.RegionName) &&
               Region.Find(location, entity.Map).IsPartOf(filter.RegionName);
    }

    private static bool PassesMapAndRange(
        IEntity entity, AdvancedSearchFilter filter, WorldLocation origin, out Point3D location
    )
    {
        location = default;

        if (!OnASelectedMap(filter, entity.Map))
        {
            return false;
        }

        location = (entity as Item)?.GetWorldLocation() ?? entity.Location;

        return !filter.FilterRange ||
               filter.Range != null &&
               filter.Range >= 0 &&
               entity.Map == origin.Map &&
               Utility.InRange(origin.Location, location, filter.Range.Value);
    }

    // The map boxes are independent checks, so several can be ticked at once: an entity passes when
    // its map is any of them. With none ticked there is no map constraint.
    internal static bool OnASelectedMap(AdvancedSearchFilter f, Map map)
    {
        var anySelected = f.FilterFelucca || f.FilterTrammel || f.FilterIlshenar || f.FilterMalas ||
                          f.FilterTokuno || f.FilterTerMur || f.FilterInternalMap || f.FilterNullMap;

        if (!anySelected)
        {
            return true;
        }

        if (map == null)
        {
            return f.FilterNullMap;
        }

        if (map == Map.Internal)
        {
            return f.FilterInternalMap;
        }

        return map == Map.Felucca && f.FilterFelucca ||
               map == Map.Trammel && f.FilterTrammel ||
               map == Map.Ilshenar && f.FilterIlshenar ||
               map == Map.Malas && f.FilterMalas ||
               map == Map.Tokuno && f.FilterTokuno ||
               map == Map.TerMur && f.FilterTerMur;
    }

    internal static bool IsValidInternal(Item item)
    {
        if (item.Parent != null || item.HeldBy != null)
        {
            return true;
        }

        if (item is Fists
            or MountItem
            or EffectItem
            or MovingCrate
            or BaseDockedBoat
            or BaseBoat
            or Plank
            or TillerMan
            or Hold)
        {
            return true;
        }

        // DisplayCache container
        if (item.GetType().DeclaringType == typeof(GenericBuyInfo))
        {
            return true;
        }

        return false;
    }

    internal static bool IsValidInternal(Mobile m)
    {
        // Logged out players
        if (m.Account != null)
        {
            return true;
        }

        // Stabled pets
        if (m is BaseCreature creature && creature.IsStabled)
        {
            return true;
        }

        // Internalized vendors
        if (m is PlayerVendor playerVendor && playerVendor.House != null)
        {
            return true;
        }

        // Currently mounted creatures
        if (m is IMount mount && mount.Rider != null)
        {
            return true;
        }

        return false;
    }

    /// <summary>Entities whose contents can sit on the internal map legitimately and must be hidden.</summary>
    internal static bool IsIgnoreSource(IEntity entity) => entity is CommodityDeed or KeyRing or BaseHouse;

    /// <summary>
    /// Adds the entities <paramref name="source" /> legitimately keeps on the internal map. Enumerates live
    /// lists, so loop only.
    /// </summary>
    internal static void CollectIgnored(IEntity source, HashSet<IEntity> ignored)
    {
        if (source is CommodityDeed deed && deed.Commodity != null && deed.Commodity.Map == Map.Internal)
        {
            ignored.Add(source);
            return;
        }

        // Keys don't have a backreference, so we just ignore them for now
        if (source is KeyRing keyring && keyring.Keys?.Count > 0)
        {
            foreach (var k in keyring.Keys)
            {
                ignored.Add(k);
            }

            return;
        }

        if (source is BaseHouse house)
        {
            foreach (var relEntity in house.RelocatedEntities)
            {
                if (relEntity.Entity is Item)
                {
                    ignored.Add(relEntity.Entity);
                }
            }

            foreach (var inventory in house.VendorInventories)
            {
                foreach (var subItem in inventory.Items)
                {
                    ignored.Add(subItem);
                }
            }
        }
    }
}
