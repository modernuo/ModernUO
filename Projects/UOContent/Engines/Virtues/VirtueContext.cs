using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Virtues;

[PropertyObject]
[SerializationGenerator(0)]
public partial class VirtueContext
{
    [DeltaDateTime]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private DateTime _lastSacrificeGain;

    [SerializableFieldSaveFlag(0)]
    private bool ShouldSerializeLastSacrificeGain() => !SacrificeVirtue.CanGain(this);

    [DeltaDateTime]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private DateTime _lastSacrificeLoss;

    [SerializableFieldSaveFlag(1)]
    private bool ShouldSerializeLastSacrificeLoss() => !SacrificeVirtue.CanAtrophy(this);

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _availableResurrects;

    [SerializableFieldSaveFlag(2)]
    private bool ShouldSerializeAvailableResurrects() => _availableResurrects > 0;

    [DeltaDateTime]
    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private DateTime _lastJusticeLoss;

    [SerializableFieldSaveFlag(3)]
    private bool ShouldSerializeLastJusticeLoss() => !JusticeVirtue.CanAtrophy(this);

    [DeltaDateTime]
    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private DateTime _lastCompassionLoss;

    [SerializableFieldSaveFlag(4)]
    private bool ShouldSerializeLastCompassionLoss() => !CompassionVirtue.CanAtrophy(this);

    [DeltaDateTime]
    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextCompassionDay;

    [SerializableFieldSaveFlag(5)]
    private bool ShouldSerializeNextCompassionDay() => _nextCompassionDay > Core.Now;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _compassionGains;

    [SerializableFieldSaveFlag(6)]
    private bool ShouldSerializeCompassionGains() => _compassionGains > 0;

    [DeltaDateTime]
    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private DateTime _lastValorLoss;

    [SerializableFieldSaveFlag(7)]
    private bool ShouldSerializeValorLoss() => !ValorVirtue.CanAtrophy(this);

    [DeltaDateTime]
    [SerializableField(8)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _lastHonorUse;

    [SerializableFieldSaveFlag(8)]
    private bool ShouldSerializeLastHonorUse() => !HonorVirtue.CanUse(this);

    [SerializableField(9)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private bool _honorActive;

    [SerializableFieldSaveFlag(9)]
    private bool ShouldSerializeHonorActive() => _honorActive;

    [SerializableField(10)]
    private PlayerMobile _justiceProtection;

    [SerializableFieldSaveFlag(10)]
    private bool ShouldSerializeJusticeProtection() => _justiceProtection != null && _justiceStatus != JusticeProtectorStatus.None;

    [SerializableField(11)]
    private JusticeProtectorStatus _justiceStatus;

    [SerializableFieldSaveFlag(11)]
    private bool ShouldSerializeJusticeStatus() => _justiceProtection != null && _justiceStatus != JusticeProtectorStatus.None;

    [SerializableField(12, setter: "private")]
    private int[] _values;

    [SerializableFieldSaveFlag(12)]
    private bool ShouldSerializeValues()
    {
        if (_values == null)
        {
            return false;
        }

        for (var i = 0; i < _values.Length; i++)
        {
            if (_values[i] > 0)
            {
                return true;
            }
        }

        return false;
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public int Humility
    {
        get => GetValue(0);
        set => SetValue(0, value);
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public int Sacrifice
    {
        get => GetValue(1);
        set => SetValue(1, value);
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public int Compassion
    {
        get => GetValue(2);
        set => SetValue(2, value);
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public int Spirituality
    {
        get => GetValue(3);
        set => SetValue(3, value);
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public int Valor
    {
        get => GetValue(4);
        set => SetValue(4, value);
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public int Honor
    {
        get => GetValue(5);
        set => SetValue(5, value);
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public int Justice
    {
        get => GetValue(6);
        set => SetValue(6, value);
    }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public int Honesty
    {
        get => GetValue(7);
        set => SetValue(7, value);
    }

    public int GetValue(int index) => _values?[index] ?? 0;

    public void SetValue(int index, int value)
    {
        _values ??= new int[8];
        _values[index] = value;
    }

    public override string ToString() => "...";

    // Used to invalidate and delete the VirtueContext, usually during world load
    public bool IsUsed() => ShouldSerializeLastSacrificeGain() || ShouldSerializeLastSacrificeLoss() ||
                            ShouldSerializeAvailableResurrects() || ShouldSerializeLastJusticeLoss() ||
                            ShouldSerializeJusticeStatus() || ShouldSerializeNextCompassionDay() ||
                            ShouldSerializeCompassionGains() || ShouldSerializeValorLoss() ||
                            ShouldSerializeLastHonorUse() || ShouldSerializeHonorActive() ||
                            ShouldSerializeValues() || ShouldSerializeLastCompassionLoss();
}
