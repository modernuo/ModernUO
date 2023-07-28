using System;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.CannedEvil;

[PropertyObject]
[SerializationGenerator(1)]
public partial class ChampionTitleContext
{
    private const int LossAmount = 90;
    private static readonly TimeSpan LossDelay = TimeSpan.FromDays(1.0);

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _harrower;

    private PlayerMobile _player;

    public PlayerMobile Player => _player;

    public ChampionTitleContext(PlayerMobile player) => _player = player;

    // Visible to PublicMobile for migrations
    internal void Deserialize(IGenericReader reader, int version)
    {
        _harrower = reader.ReadEncodedInt();

        var length = reader.ReadEncodedInt();

        if (length == 0)
        {
            return;
        }

        // If the list/enum for ChampionSpawnInfo changes, then this has to change for migrations
        for (var i = 0; i < length; i++)
        {
            var type = (ChampionSpawnType)i;
            ref var title = ref GetTitleValueRef(type);

            if (Unsafe.IsNullRef(ref title))
            {
                throw new NotImplementedException($"Cannot find ChampionSpawnType value {type}.");
            }

            title = new ChampionTitle();
            title.Deserialize(reader);
        }
    }

    [SerializableField(1)]
    private ChampionTitle _abyss;

    [SerializableFieldSaveFlag(1)]
    private bool ShouldSerializeAbyss() => _abyss != null;

    [CommandProperty(AccessLevel.GameMaster)]
    public int AbyssValue
    {
        get => GetTitle(ChampionSpawnType.Abyss)?.Value ?? 0;
        set => SetValue(ChampionSpawnType.Abyss, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime AbyssLastDecay
    {
        get => GetTitle(ChampionSpawnType.Abyss)?.LastDecay ?? DateTime.MinValue;
        set => SetLastDecay(ChampionSpawnType.Abyss, value);
    }

    [SerializableField(2)]
    private ChampionTitle _arachnid;

    [SerializableFieldSaveFlag(2)]
    private bool ShouldSerializeArachnid() => _arachnid != null;

    [CommandProperty(AccessLevel.GameMaster)]
    public int ArachnidValue
    {
        get => GetTitle(ChampionSpawnType.Arachnid)?.Value ?? 0;
        set => SetValue(ChampionSpawnType.Arachnid, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime ArachnidLastDecay
    {
        get => GetTitle(ChampionSpawnType.Arachnid)?.LastDecay ?? DateTime.MinValue;
        set => SetLastDecay(ChampionSpawnType.Arachnid, value);
    }

    [SerializableField(3)]
    private ChampionTitle _coldBlood;

    [SerializableFieldSaveFlag(3)]
    private bool ShouldSerializeColdBlood() => _coldBlood != null;

    [CommandProperty(AccessLevel.GameMaster)]
    public int ColdBloodValue
    {
        get => GetTitle(ChampionSpawnType.ColdBlood)?.Value ?? 0;
        set => SetValue(ChampionSpawnType.ColdBlood, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime ColdBloodLastDecay
    {
        get => GetTitle(ChampionSpawnType.ColdBlood)?.LastDecay ?? DateTime.MinValue;
        set => SetLastDecay(ChampionSpawnType.ColdBlood, value);
    }

    [SerializableField(4)]
    private ChampionTitle _forestLord;

    [SerializableFieldSaveFlag(4)]
    private bool ShouldSerializeForestLord() => _forestLord != null;

    [CommandProperty(AccessLevel.GameMaster)]
    public int ForestLordValue
    {
        get => GetTitle(ChampionSpawnType.ForestLord)?.Value ?? 0;
        set => SetValue(ChampionSpawnType.ForestLord, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime ForestLordLastDecay
    {
        get => GetTitle(ChampionSpawnType.ForestLord)?.LastDecay ?? DateTime.MinValue;
        set => SetLastDecay(ChampionSpawnType.ForestLord, value);
    }

    [SerializableField(5)]
    private ChampionTitle _verminHorde;

    [SerializableFieldSaveFlag(5)]
    private bool ShouldSerializeVerminHorde() => _verminHorde != null;

    [CommandProperty(AccessLevel.GameMaster)]
    public int VerminHordeValue
    {
        get => GetTitle(ChampionSpawnType.VerminHorde)?.Value ?? 0;
        set => SetValue(ChampionSpawnType.VerminHorde, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime VerminHordeLastDecay
    {
        get => GetTitle(ChampionSpawnType.VerminHorde)?.LastDecay ?? DateTime.MinValue;
        set => SetLastDecay(ChampionSpawnType.VerminHorde, value);
    }

    [SerializableField(6)]
    private ChampionTitle _unholyTerror;

    [SerializableFieldSaveFlag(6)]
    private bool ShouldSerializeUnholyTerror() => _unholyTerror != null;

    [CommandProperty(AccessLevel.GameMaster)]
    public int UnholyTerrorValue
    {
        get => GetTitle(ChampionSpawnType.UnholyTerror)?.Value ?? 0;
        set => SetValue(ChampionSpawnType.UnholyTerror, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime UnholyTerrorLastDecay
    {
        get => GetTitle(ChampionSpawnType.UnholyTerror)?.LastDecay ?? DateTime.MinValue;
        set => SetLastDecay(ChampionSpawnType.UnholyTerror, value);
    }

    [SerializableField(7)]
    private ChampionTitle _sleepingDragon;

    [SerializableFieldSaveFlag(7)]
    private bool ShouldSerializeSleepingDragon() => _sleepingDragon != null;

    [CommandProperty(AccessLevel.GameMaster)]
    public int SleepingDragonValue
    {
        get => GetTitle(ChampionSpawnType.SleepingDragon)?.Value ?? 0;
        set => SetValue(ChampionSpawnType.SleepingDragon, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime SleepingDragonLastDecay
    {
        get => GetTitle(ChampionSpawnType.SleepingDragon)?.LastDecay ?? DateTime.MinValue;
        set => SetLastDecay(ChampionSpawnType.SleepingDragon, value);
    }

    [SerializableField(8)]
    private ChampionTitle _corrupt;

    [SerializableFieldSaveFlag(8)]
    private bool ShouldSerializeCorrupt() => _corrupt != null;

    [CommandProperty(AccessLevel.GameMaster)]
    public int CorruptValue
    {
        get => GetTitle(ChampionSpawnType.Corrupt)?.Value ?? 0;
        set => SetValue(ChampionSpawnType.Corrupt, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime CorruptLastDecay
    {
        get => GetTitle(ChampionSpawnType.Corrupt)?.LastDecay ?? DateTime.MinValue;
        set => SetLastDecay(ChampionSpawnType.Corrupt, value);
    }

    [SerializableField(9)]
    private ChampionTitle _glade;

    [SerializableFieldSaveFlag(9)]
    private bool ShouldSerializeGlade() => _glade != null;

    [CommandProperty(AccessLevel.GameMaster)]
    public int GladeValue
    {
        get => GetTitle(ChampionSpawnType.Glade)?.Value ?? 0;
        set => SetValue(ChampionSpawnType.Glade, value);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime GladeLastDecay
    {
        get => GetTitle(ChampionSpawnType.Glade)?.LastDecay ?? DateTime.MinValue;
        set => SetLastDecay(ChampionSpawnType.Glade, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ChampionTitle GetTitle(ChampionSpawnType type) => GetTitleValueRef(type);

    // Get a pointer to the title so we can manipulate it internally such as nullifying it.
    // Using this technique instead of an array decouples the context from changes to ChampionSpawnInfo.Table indexes
    private ref ChampionTitle GetTitleValueRef(ChampionSpawnType type)
    {
        switch (type)
        {
            case ChampionSpawnType.Abyss:
                {
                    return ref _abyss;
                }
            case ChampionSpawnType.Arachnid:
                {
                    return ref _arachnid;
                }
            case ChampionSpawnType.ColdBlood:
                {
                    return ref _coldBlood;
                }
            case ChampionSpawnType.ForestLord:
                {
                    return ref _forestLord;
                }
            case ChampionSpawnType.VerminHorde:
                {
                    return ref _verminHorde;
                }
            case ChampionSpawnType.UnholyTerror:
                {
                    return ref _unholyTerror;
                }
            case ChampionSpawnType.SleepingDragon:
                {
                    return ref _sleepingDragon;
                }
            case ChampionSpawnType.Glade:
                {
                    return ref _glade;
                }
            case ChampionSpawnType.Corrupt:
                {
                    return ref _corrupt;
                }
            default:
                {
                    return ref Unsafe.NullRef<ChampionTitle>();
                }
        }
    }

    public ChampionTitle TryGetOrCreateTitle(ChampionSpawnType type)
    {
        ref var title = ref GetTitleValueRef(type);
        if (Unsafe.IsNullRef(ref title))
        {
            return null;
        }

        return title ??= new ChampionTitle();
    }

    public void SetValue(ChampionSpawnType type, int value)
    {
        ref var title = ref GetTitleValueRef(type);
        if (Unsafe.IsNullRef(ref title))
        {
            return;
        }

        if (title != null)
        {
            if (value <= 0)
            {
                title = null;

                _player.InvalidateProperties();
                return;
            }
        }
        else
        {
            title = new ChampionTitle();
        }

        title.Value = value;
        title.LastDecay = Core.Now;
        _player.InvalidateProperties();
    }

    public void SetLastDecay(ChampionSpawnType type, DateTime value)
    {
        var title = TryGetOrCreateTitle(type);
        if (title != null)
        {
            title.LastDecay = value;
        }
    }

    public void Award(ChampionSpawnType type, int value)
    {
        var title = TryGetOrCreateTitle(type);
        if (title == null)
        {
            return;
        }

        title.Value += value;
        if (title.LastDecay == DateTime.MinValue)
        {
            title.LastDecay = Core.Now;
        }
    }

    public bool CheckAtrophy()
    {
        var valuesUsed = _harrower > 0;

        for (var i = 0; i < ChampionSpawnInfo.Table.Length; i++)
        {
            ref var title = ref GetTitleValueRef(ChampionSpawnInfo.Table[i].Type);
            if (Unsafe.IsNullRef(ref title) || title == null)
            {
                continue;
            }

            var decay = title.LastDecay;
            if (decay > DateTime.MinValue && decay + LossDelay < Core.Now)
            {
                // Use bitwise-or to force-execute the Atrophy function
                var isUsed = title.Atrophy(LossAmount);

                if (!isUsed)
                {
                    title = null;
                }

                valuesUsed |= isUsed;
            }
        }

        return valuesUsed;
    }

    public override string ToString() => "...";
}
