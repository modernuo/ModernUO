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
    [SaveFlag(nameof(ShouldSerializeLastSacrificeGain))]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private DateTime _lastSacrificeGain;

    private bool ShouldSerializeLastSacrificeGain() => !SacrificeVirtue.CanGain(this);

    [DeltaDateTime]
    [SerializableField(1)]
    [SaveFlag(nameof(ShouldSerializeLastSacrificeLoss))]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private DateTime _lastSacrificeLoss;

    private bool ShouldSerializeLastSacrificeLoss() => !SacrificeVirtue.CanAtrophy(this);

    [SerializableField(2)]
    [SaveFlag(nameof(ShouldSerializeAvailableResurrects))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _availableResurrects;

    private bool ShouldSerializeAvailableResurrects() => _availableResurrects > 0;

    [DeltaDateTime]
    [SerializableField(3)]
    [SaveFlag(nameof(ShouldSerializeLastJusticeLoss))]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private DateTime _lastJusticeLoss;

    private bool ShouldSerializeLastJusticeLoss() => !JusticeVirtue.CanAtrophy(this);

    [DeltaDateTime]
    [SerializableField(4)]
    [SaveFlag(nameof(ShouldSerializeLastCompassionLoss))]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private DateTime _lastCompassionLoss;

    private bool ShouldSerializeLastCompassionLoss() => !CompassionVirtue.CanAtrophy(this);

    [DeltaDateTime]
    [SerializableField(5)]
    [SaveFlag(nameof(ShouldSerializeNextCompassionDay))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextCompassionDay;

    private bool ShouldSerializeNextCompassionDay() => _nextCompassionDay > Core.Now;

    [SerializableField(6)]
    [SaveFlag(nameof(ShouldSerializeCompassionGains))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _compassionGains;

    private bool ShouldSerializeCompassionGains() => _compassionGains > 0;

    [DeltaDateTime]
    [SerializableField(7)]
    [SaveFlag(nameof(ShouldSerializeValorLoss))]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private DateTime _lastValorLoss;

    private bool ShouldSerializeValorLoss() => !ValorVirtue.CanAtrophy(this);

    [DeltaDateTime]
    [SerializableField(8)]
    [SaveFlag(nameof(ShouldSerializeLastHonorUse))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _lastHonorUse;

    private bool ShouldSerializeLastHonorUse() => !HonorVirtue.CanUse(this);

    [SerializableField(9)]
    [SaveFlag(nameof(ShouldSerializeHonorActive))]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private bool _honorActive;

    private bool ShouldSerializeHonorActive() => _honorActive;

    [SerializableField(10)]
    [SaveFlag(nameof(ShouldSerializeJusticeProtection))]
    private PlayerMobile _justiceProtection;

    private bool ShouldSerializeJusticeProtection() => _justiceProtection != null && _justiceStatus != JusticeProtectorStatus.None;

    [SerializableField(11)]
    [SaveFlag(nameof(ShouldSerializeJusticeStatus))]
    private JusticeProtectorStatus _justiceStatus;

    private bool ShouldSerializeJusticeStatus() => _justiceProtection != null && _justiceStatus != JusticeProtectorStatus.None;

    [SerializableField(12, setter: "private")]
    [SaveFlag(nameof(ShouldSerializeValues))]
    private int[] _values;

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
