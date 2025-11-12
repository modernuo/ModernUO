using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Multis;

[SerializationGenerator(1, false)]
public abstract partial class BaseCamp : BaseMulti
{
    [Tidy]
    [SerializableField(0, setter: "private")]
    private List<Item> _items;

    [Tidy]
    [SerializableField(1, setter: "private")]
    private List<Mobile> _mobiles;

    [DeltaDateTime]
    [SerializableField(2, setter: "private")]
    private DateTime _decayTime;

    private TimeSpan _decayDelay;
    private Timer _decayTimer;
    private Timer _initTimer;

    public BaseCamp(int multiID) : base(multiID)
    {
        _items = new List<Item>();
        _mobiles = new List<Mobile>();
        _decayDelay = TimeSpan.FromMinutes(30.0);
        RefreshDecay(true);

        _initTimer = Timer.DelayCall(TimeSpan.Zero, CheckAddComponents);
    }

    public virtual int EventRange => 10;

    public TimeSpan DecayDelay
    {
        get => _decayDelay;
        set
        {
            _decayDelay = value;
            RefreshDecay(true);
        }
    }

    public override bool HandlesOnMovement => true;

    public void CheckAddComponents()
    {
        _initTimer = null;
        
        if (Deleted)
        {
            return;
        }

        AddComponents();
    }

    public virtual void AddComponents()
    {
    }

    public virtual void RefreshDecay(bool setDecayTime)
    {
        if (Deleted)
        {
            return;
        }

        if (setDecayTime)
        {
            _decayTime = Core.Now + _decayDelay;
        }

        _decayTimer?.Stop();
        _decayTimer = Timer.DelayCall(_decayDelay, Delete);
    }

    public virtual void AddItem(Item item, int xOffset, int yOffset, int zOffset)
    {
        AddToItems(item);

        var zavg = Map.GetAverageZ(X + xOffset, Y + yOffset);
        item.MoveToWorld(new Point3D(X + xOffset, Y + yOffset, zavg + zOffset), Map);
    }

    public virtual void AddMobile(Mobile m, int wanderRange, int xOffset, int yOffset, int zOffset)
    {
        AddToMobiles(m);

        var zavg = Map.GetAverageZ(X + xOffset, Y + yOffset);
        var loc = new Point3D(X + xOffset, Y + yOffset, zavg + zOffset);

        if (m is BaseCreature bc)
        {
            bc.RangeHome = wanderRange;
            bc.Home = loc;
        }

        if (m is BaseVendor)
        {
            m.Direction = Direction.South;
        }

        m.MoveToWorld(loc, Map);
    }

    public virtual void OnEnter(Mobile m)
    {
        RefreshDecay(true);
    }

    public virtual void OnExit(Mobile m)
    {
        RefreshDecay(true);
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        var inOldRange = Utility.InRange(oldLocation, Location, EventRange);
        var inNewRange = Utility.InRange(m.Location, Location, EventRange);

        if (inNewRange && !inOldRange)
        {
            OnEnter(m);
        }
        else if (inOldRange && !inNewRange)
        {
            OnExit(m);
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        for (var i = 0; i < _items.Count; ++i)
        {
            _items[i]?.Delete();
        }

        for (var i = 0; i < _mobiles.Count; ++i)
        {
            var mob = _mobiles[i];

            if (mob != null && (mob.CantWalk || (mob as BaseCreature)?.IsPrisoner == false))
            {
                mob.Delete();
            }
        }

        ClearItems();
        ClearMobiles();

        _decayTimer?.Stop();
        _decayTimer = null;
        
        _initTimer?.Stop();
        _initTimer = null;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _items = reader.ReadEntityList<Item>();
        _mobiles = reader.ReadEntityList<Mobile>();
        _decayTime = reader.ReadDeltaTime();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        var remaining = _decayTime - Core.Now;
        
        if (remaining > TimeSpan.Zero)
        {
            _decayDelay = remaining;
            RefreshDecay(false);
        }
        else
        {
            Timer.DelayCall(TimeSpan.Zero, Delete);
            return;
        }
        
        _initTimer = Timer.DelayCall(TimeSpan.Zero, CheckAddComponents);
    }
}

[SerializationGenerator(0, false)]
public partial class LockableBarrel : LockableContainer
{
    [Constructible]
    public LockableBarrel() : base(0xE77)
    {
    }

    public override double DefaultWeight => 1.0;
}
