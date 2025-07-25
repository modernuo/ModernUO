using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;

namespace Server.Items;

[Flippable(0x234C, 0x234D)]
[SerializationGenerator(1)]
public partial class RoseOfTrinsic : Item, ISecurable
{
    private static readonly TimeSpan m_SpawnTime = TimeSpan.FromHours(4.0);

    private SpawnTimer _spawnTimer;

    [SerializableField(1, setter: "private")]
    private DateTime _nextSpawnTime;

    [SerializedIgnoreDupe]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [Constructible]
    public RoseOfTrinsic() : base(0x234D)
    {
        LootType = LootType.Blessed;
        _petals = 0;
        StartSpawnTimer(TimeSpan.FromMinutes(1.0));
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1062913; // Rose of Trinsic

    public override void OnAfterDuped(Item newItem)
    {
        if (newItem is not RoseOfTrinsic rose)
        {
            return;
        }

        rose.NextSpawnTime = NextSpawnTime;
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Petals
    {
        get => _petals;
        set
        {
            if (value >= 10)
            {
                _petals = 10;

                StopSpawnTimer();
            }
            else
            {
                _petals = value <= 0 ? 0 : value;

                StartSpawnTimer(m_SpawnTime);
            }

            InvalidateProperties();
            this.MarkDirty();
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1062925, Petals); // Petals:  ~1_COUNT~
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        SetSecureLevelEntry.AddTo(from, this, ref list);
    }

    private void StartSpawnTimer(TimeSpan delay)
    {
        StopSpawnTimer();

        _spawnTimer = new SpawnTimer(this, delay);
        _spawnTimer.Start();

        NextSpawnTime = Core.Now + delay;
    }

    private void StopSpawnTimer()
    {
        _spawnTimer?.Stop();
        _spawnTimer = null;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else if (_petals > 0)
        {
            from.AddToBackpack(new RoseOfTrinsicPetal(_petals));
            Petals = 0;
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _petals = reader.ReadEncodedInt();
        _nextSpawnTime = reader.ReadDeltaTime();
        _level = (SecureLevel)reader.ReadEncodedInt();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_petals < 10)
        {
            StartSpawnTimer(_nextSpawnTime - Core.Now);
        }
    }

    private class SpawnTimer : Timer
    {
        private readonly RoseOfTrinsic _rose;

        public SpawnTimer(RoseOfTrinsic rose, TimeSpan delay) : base(delay) => _rose = rose;

        protected override void OnTick()
        {
            if (_rose.Deleted)
            {
                return;
            }

            _rose._spawnTimer = null;
            _rose.Petals++;
        }
    }
}

[SerializationGenerator(0)]
public partial class RoseOfTrinsicPetal : Item
{
    [Constructible]
    public RoseOfTrinsicPetal(int amount = 1) : base(0x1021)
    {
        Stackable = true;
        Amount = amount;
        Hue = 0xE;
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1062926; // Petal of the Rose of Trinsic

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
        }
        else if (from.GetStatMod("RoseOfTrinsicPetal") != null)
        {
            // You have eaten one of these recently and eating another would provide no benefit.
            from.SendLocalizedMessage(1062927);
        }
        else
        {
            from.PlaySound(0x1EE);
            from.AddStatMod(new StatMod(StatType.Str, "RoseOfTrinsicPetal", 5, TimeSpan.FromMinutes(5.0)));

            Consume();
        }
    }
}
