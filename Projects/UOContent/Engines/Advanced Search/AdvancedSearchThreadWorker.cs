using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Engines.AdvancedSearch;

public class AdvancedSearchThreadWorker
{
    private readonly Thread _thread;
    private readonly AutoResetEvent _startEvent; // Main thread tells the thread to start working
    private readonly AutoResetEvent _stopEvent; // Main thread waits for the worker finish draining
    private bool _pause;
    private bool _exit;
    private readonly ConcurrentQueue<IEntity> _entities;
    private ConcurrentQueue<AdvancedSearchResult> _results;
    private ConcurrentQueue<IEntity> _ignoreQueue;
    private WorldLocation _worldLocation;
    private AdvancedSearchFilter _filter;

    public AdvancedSearchThreadWorker()
    {
        _startEvent = new AutoResetEvent(false);
        _stopEvent = new AutoResetEvent(false);
        _entities = new ConcurrentQueue<IEntity>();
        _thread = new Thread(Execute);
        _thread.Start(this);
    }

    public void Wake(
        WorldLocation worldLocation,
        AdvancedSearchFilter filter,
        ConcurrentQueue<AdvancedSearchResult> results,
        ConcurrentQueue<IEntity> ignoreQueue
    )
    {
        _worldLocation = worldLocation;
        _filter = filter;
        _ignoreQueue = ignoreQueue;
        _results = results;
        _startEvent.Set();
    }

    public void Sleep()
    {
        Volatile.Write(ref _pause, true);
        _stopEvent.WaitOne();
    }

    public void Exit()
    {
        _exit = true;

        Wake(WorldLocation.Zero, null, null, null);
        Sleep();
    }

    public void Push(IEntity entity)
    {
        _entities.Enqueue(entity);
    }

    private static void Execute(object obj)
    {
        var worker = (AdvancedSearchThreadWorker)obj;

        var reader = worker._entities;

        while (worker._startEvent.WaitOne())
        {
            while (true)
            {
                var pauseRequested = Volatile.Read(ref worker._pause);
                if (reader.TryDequeue(out var entity))
                {
                    var result = worker.DoEntitySearch(entity);
                    if (result != null)
                    {
                        worker._results?.Enqueue(result);
                    }
                }
                else if (pauseRequested) // Break when finished
                {
                    worker._results = null;
                    worker._filter = null;
                    break;
                }
            }

            worker._stopEvent.Set(); // Allow the main thread to continue now that we are finished
            worker._pause = false;

            if (Core.Closing || worker._exit)
            {
                return;
            }
        }
    }

    private AdvancedSearchResult DoEntitySearch(IEntity entity)
    {
        if (_filter.HideValidInternalMap)
        {
            HandleValidInternal(entity);
        }

        // Check for valid map
        if (_filter.FilterFelucca && entity.Map != Map.Felucca ||
            _filter.FilterTrammel && entity.Map != Map.Trammel ||
            _filter.FilterIlshenar && entity.Map != Map.Ilshenar ||
            _filter.FilterMalas && entity.Map != Map.Malas ||
            _filter.FilterTokuno && entity.Map != Map.Tokuno ||
            _filter.FilterTerMur && entity.Map != Map.TerMur ||
            _filter.FilterInternalMap && entity.Map != Map.Internal ||
            _filter.FilterNullMap && entity.Map != null)
        {
            return null;
        }

        var location = (entity as Item)?.GetWorldLocation() ?? entity.Location;

        if (_filter.FilterRange &&
            (_filter.Range == null ||
             _filter.Range < 0 ||
             entity.Map != _worldLocation.Map ||
             !Utility.InRange(_worldLocation.Location, location, _filter.Range.Value)))
        {
            return null;
        }

        if (_filter.FilterRegion &&
            (string.IsNullOrWhiteSpace(_filter.RegionName) ||
             !Region.Find(location, entity.Map).IsPartOf(_filter.RegionName)))
        {
            return null;
        }

        if (entity is Mobile mobile)
        {
            return DoMobileSearch(mobile);
        }

        if (entity is Item item)
        {
            return DoItemSearch(item);
        }

        return null;
    }

    private static bool IsValidInternal(Item item)
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

    private AdvancedSearchResult DoItemSearch(Item item)
    {
        if (_filter.FilterName && !string.IsNullOrWhiteSpace(_filter.Name) && !(item.Name ?? item.ItemData.Name).InsensitiveContains(_filter.Name))
        {
            return null;
        }

        if (_filter.HideValidInternalMap && item.Map == Map.Internal && !IsValidInternal(item))
        {
            return null;
        }

        if (_filter.FilterPropertyTest &&
            (string.IsNullOrWhiteSpace(_filter.PropertyTest) || !EvaluateRecursive(item, _filter.PropertyTest)))
        {
            return null;
        }

        return new AdvancedSearchResult(item.Name ?? item.ItemData.Name, item.GetType(), item.Location, item.Map, item.RootParent)
        {
            Entity = item,
        };
    }

    private AdvancedSearchResult DoMobileSearch(Mobile mobile)
    {
        if (_filter.FilterName && !string.IsNullOrWhiteSpace(_filter.Name) && !mobile.Name.InsensitiveContains(_filter.Name))
        {
            return null;
        }

        if (_filter.HideValidInternalMap && mobile.Map == Map.Internal && !IsValidInternal(mobile))
        {
            return null;
        }

        if (_filter.FilterPropertyTest &&
            (string.IsNullOrWhiteSpace(_filter.PropertyTest) || !EvaluateRecursive(mobile, _filter.PropertyTest)))
        {
            return null;
        }

        return new AdvancedSearchResult(mobile.Name, mobile.GetType(), mobile.Location, mobile.Map, null)
        {
            Entity = mobile
        };
    }

    private static bool IsValidInternal(Mobile m)
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

    public void HandleValidInternal(IEntity entity)
    {
        if (entity is CommodityDeed deed && deed.Commodity != null && deed.Commodity.Map == Map.Internal)
        {
            _ignoreQueue.Enqueue(entity);
            return;
        }

        // Keys don't have a backreference, so we just ignore them for now
        if (entity is KeyRing keyring && keyring.Keys?.Count > 0)
        {
            foreach (var k in keyring.Keys)
            {
                _ignoreQueue.Enqueue(k);
            }

            return;
        }

        if (entity is BaseHouse house)
        {
            foreach (var relEntity in house.RelocatedEntities)
            {
                if (relEntity.Entity is Item)
                {
                    _ignoreQueue.Enqueue(relEntity.Entity);
                }
            }

            foreach (var inventory in house.VendorInventories)
            {
                foreach (var subItem in inventory.Items)
                {
                    _ignoreQueue.Enqueue(subItem);
                }
            }
        }
    }

    private static bool EvaluateRecursive(IEntity entity, ReadOnlySpan<char> span)
    {
        var atIndex = span.IndexOf('@');
        var orIndex = span.IndexOf('|');

        if (atIndex == -1 && orIndex == -1)
        {
            return EvaluateSingleExpression(entity, span);
        }

        var result = atIndex != -1;
        var splitIndex = result ? atIndex : orIndex;

        var left = EvaluateRecursive(entity, span.Slice(0, splitIndex));
        var right = EvaluateRecursive(entity, span.Slice(splitIndex + 1));

        return result ? left && right : left || right;
    }

    private static bool EvaluateSingleExpression(IEntity entity, ReadOnlySpan<char> expression)
    {
        var negate = false;
        if (expression[0] == '~')
        {
            negate = true;
            expression = expression[1..];
        }

        var operatorSpan = AdvancedSearchUtilities.FindOperatorIndex(expression, out var operatorIndex);
        if (operatorSpan.Length == 0)
        {
            return false;
        }

        var propertyName = expression[..operatorIndex].Trim();
        var valuePart = expression[(operatorIndex + operatorSpan.Length)..].Trim();

        if (valuePart.Length == 0)
        {
            return false;
        }

        var properties = entity.GetType().GetProperties();
        PropertyInfo property = null;
        for (var i = 0; i < properties.Length; ++i)
        {
            var p = properties[i];
            if (p.CanRead && p.Name.InsensitiveEquals(propertyName))
            {
                property = p;
                break;
            }
        }

        if (property == null)
        {
            return false;
        }

        var propertyValue = property.GetValue(entity);
        var result = AdvancedSearchUtilities.CompareValues(property.PropertyType, propertyValue, valuePart, operatorSpan);

        return negate ? !result : result;
    }
}
