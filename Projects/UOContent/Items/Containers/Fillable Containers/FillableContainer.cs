using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(2, false)]
public abstract partial class FillableContainer : LockableContainer
{
    [TimerDrift]
    [SerializableField(1)]
    private Timer _respawnTimer;

    [DeserializeTimerField(1)]
    private void DeserializeRespawnTimer(TimeSpan delay)
    {
        if (delay > TimeSpan.MinValue)
        {
            _respawnTimer = Timer.DelayCall(delay, Respawn);
        }
    }

    public FillableContainer(int itemID) : base(itemID) => Movable = false;

    public virtual int MinRespawnMinutes => 60;
    public virtual int MaxRespawnMinutes => 90;

    public virtual bool IsLockable => true;
    public virtual bool IsTrappable => IsLockable;

    public virtual int SpawnThreshold => 2;

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime NextRespawnTime => _respawnTimer?.Next ?? DateTime.MinValue;

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public FillableContentType ContentType
    {
        get => _contentType;
        set
        {
            if (_contentType == value)
            {
                return;
            }

            ClearContents();
            _contentType = value;
            Respawn();
        }
    }

    protected void ClearContents()
    {
        for (var i = Items.Count - 1; i >= 0; --i)
        {
            if (i < Items.Count)
            {
                Items[i].Delete();
            }
        }
    }

    public override void OnMapChange()
    {
        base.OnMapChange();
        AcquireContent();
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);
        AcquireContent();
    }

    public virtual void AcquireContent()
    {
        if (_contentType != FillableContentType.None)
        {
            return;
        }

        // Don't trigger serialization code
        _contentType = FillableContent.Acquire(GetWorldLocation(), Map);

        if (_contentType != FillableContentType.None)
        {
            Respawn();
        }
    }

    public override void OnItemRemoved(Item item)
    {
        CheckRespawn();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _respawnTimer?.Stop();
        _respawnTimer = null;
    }

    public int GetItemsCount()
    {
        var count = 0;

        foreach (var item in Items)
        {
            count += item.Amount;
        }

        return count;
    }

    public void CheckRespawn()
    {
        var canSpawn =
            _contentType != FillableContentType.None &&
            !Deleted && !Movable && Parent == null && !IsLockedDown && !IsSecure &&
            (
                GetItemsCount() <= SpawnThreshold ||
                IsLockable && !Locked ||
                IsTrappable && TrapType == TrapType.None
            );

        if (canSpawn)
        {
            if (_respawnTimer?.Running != true)
            {
                var mins = Utility.RandomMinMax(MinRespawnMinutes, MaxRespawnMinutes);
                var delay = TimeSpan.FromMinutes(mins);
                _respawnTimer = Timer.DelayCall(delay, Respawn);
            }
        }
        else
        {
            _respawnTimer?.Stop();
            _respawnTimer = null;
        }
    }

    public void Respawn()
    {
        _respawnTimer?.Stop();
        _respawnTimer = null;

        if (_contentType == FillableContentType.None || Deleted)
        {
            return;
        }

        GenerateContent();

        var level = FillableContent.Lookup(_contentType).Level;

        if (IsLockable)
        {
            Locked = true;

            var difficulty = (level - 1) * 30;

            LockLevel = difficulty - 10;
            MaxLockLevel = difficulty + 30;
            RequiredSkill = difficulty;
        }

        if (IsTrappable && (level > 1 || Utility.Random(5) < 4))
        {
            TrapType = level > Utility.Random(5) ? TrapType.PoisonTrap : TrapType.ExplosionTrap;
            TrapPower = level * Utility.RandomMinMax(10, 30);
            TrapLevel = level;
        }
        else
        {
            TrapType = TrapType.None;
            TrapPower = 0;
            TrapLevel = 0;
        }

        CheckRespawn();
    }

    protected virtual int GetSpawnCount()
    {
        var itemsCount = GetItemsCount();

        if (itemsCount > SpawnThreshold)
        {
            return 0;
        }

        var maxSpawnCount = (1 + SpawnThreshold - itemsCount) * 2;

        return Utility.RandomMinMax(0, maxSpawnCount);
    }

    public virtual void GenerateContent()
    {
        if (_contentType == FillableContentType.None || Deleted)
        {
            return;
        }

        var content = FillableContent.Lookup(_contentType);

        var toSpawn = GetSpawnCount();

        for (var i = 0; i < toSpawn; ++i)
        {
            var item = content.Construct();

            if (item == null)
            {
                continue;
            }

            var list = Items;

            for (var j = 0; j < list.Count; ++j)
            {
                var subItem = list[j];

                if (subItem is not Container && subItem.StackWith(null, item, false))
                {
                    break;
                }
            }

            if (!item.Deleted)
            {
                DropItem(item);
            }
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _contentType = (FillableContentType)reader.ReadInt();
        var respawnTimerNext = reader.ReadDeltaTime();
        DeserializeRespawnTimer(respawnTimerNext == DateTime.MinValue ? TimeSpan.MinValue : respawnTimerNext - Core.Now);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_respawnTimer?.Running != true)
        {
            CheckRespawn();
        }
    }
}
