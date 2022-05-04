using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(1, false)]
public partial class BaseTreasureChest : LockableContainer
{
    public enum TreasureLevel
    {
        Level1,
        Level2,
        Level3,
        Level4,
        Level5,
        Level6
    }

    private TimerExecutionToken _resetTimer;

    public BaseTreasureChest(int itemID, TreasureLevel level = TreasureLevel.Level2) : base(itemID)
    {
        _level = level;
        _minSpawnTime = TimeSpan.FromMinutes(10);
        _maxSpawnTime = TimeSpan.FromMinutes(60);

        Locked = true;
        Movable = false;

        SetLockLevel();
        GenerateTreasure();
    }

    [SerializableField(0)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private TreasureLevel _level;

    [SerializableField(1)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private TimeSpan _minSpawnTime;

    [SerializableField(2)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private TimeSpan _maxSpawnTime;

    [CommandProperty(AccessLevel.GameMaster)]
    public override bool Locked
    {
        get => base.Locked;
        set
        {
            if (base.Locked != value)
            {
                base.Locked = value;

                if (!value)
                {
                    StartResetTimer();
                }

                InvalidateProperties();
            }
        }
    }

    public override bool IsDecoContainer => false;

    public override string DefaultName => Locked ? "a locked treasure chest" : "a treasure chest";

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (!Locked)
        {
            StartResetTimer();
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _level = (TreasureLevel)reader.ReadByte();
        _minSpawnTime = TimeSpan.FromMinutes(reader.ReadShort());
        _maxSpawnTime = TimeSpan.FromMinutes(reader.ReadShort());
    }

    protected virtual void SetLockLevel()
    {
        RequiredSkill = _level switch
        {
            TreasureLevel.Level1 => LockLevel = 5,
            TreasureLevel.Level2 => LockLevel = 20,
            TreasureLevel.Level3 => LockLevel = 50,
            TreasureLevel.Level4 => LockLevel = 70,
            TreasureLevel.Level5 => LockLevel = 90,
            TreasureLevel.Level6 => LockLevel = 100,
            _                    => LockLevel = 120
        };
    }

    private void StartResetTimer()
    {
        _resetTimer.Cancel();

        var randomDuration = Utility.RandomMinMax(_minSpawnTime.Ticks, _maxSpawnTime.Ticks);
        Timer.StartTimer(TimeSpan.FromTicks(randomDuration), Reset, out _resetTimer);
    }

    protected virtual void GenerateTreasure()
    {
        var gold = _level switch
        {
            TreasureLevel.Level1 => Utility.RandomMinMax(100, 300),
            TreasureLevel.Level2 => Utility.RandomMinMax(300, 600),
            TreasureLevel.Level3 => Utility.RandomMinMax(600, 900),
            TreasureLevel.Level4 => Utility.RandomMinMax(900, 1200),
            TreasureLevel.Level5 => Utility.RandomMinMax(1200, 5000),
            _                    => Utility.RandomMinMax(5000, 9000),
        };

        DropItem(new Gold(gold));
    }

    public void ClearContents()
    {
        for (var i = Items.Count - 1; i >= 0; --i)
        {
            if (i < Items.Count)
            {
                Items[i].Delete();
            }
        }
    }

    public void Reset()
    {
        _resetTimer.Cancel();
        Locked = true;
        ClearContents();
        GenerateTreasure();
    }
}
