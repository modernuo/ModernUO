using System;
using ModernUO.Serialization;

namespace Server.Items;

public enum ECEffectType
{
    None,
    Moving,
    Location,
    Target,
    Lightning
}

public enum EffectTriggerType
{
    None,
    Sequenced,
    DoubleClick,
    InRange
}

[SerializationGenerator(0, false)]
public partial class EffectController : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _effectDelay;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _triggerDelay;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _soundDelay;

    [SerializableField(3)]
    private IEntity _source;

    [SerializableField(4)]
    private IEntity _target;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private EffectController _sequence;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _fixedDirection;

    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _explodes;

    [SerializableField(8)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _playSoundAtTrigger;

    [SerializableField(9)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ECEffectType _effectType;

    [SerializableField(10)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private EffectLayer _effectLayer;

    [SerializableField(11)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private EffectTriggerType _triggerType;

    [SerializableField(12)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _effectItemId;

    [SerializableField(13)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _effectHue;

    [SerializableField(14)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _renderMode;

    [SerializableField(15)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _speed;

    [SerializableField(16)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _duration;

    [SerializableField(17)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _particleEffect;

    [SerializableField(18)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _explodeParticleEffect;

    [SerializableField(19)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _explodeSound;

    [SerializableField(20)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _unknown;

    [SerializableField(21)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _soundId;

    [SerializableField(22)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _triggerRange;

    [Constructible]
    public EffectController() : base(0x1B72)
    {
        Movable = false;
        Visible = false;
        TriggerType = EffectTriggerType.Sequenced;
        EffectLayer = (EffectLayer)255;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Item SourceItem
    {
        get => _source as Item;
        set => _source = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile SourceMobile
    {
        get => _source as Mobile;
        set => _source = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool SourceNull
    {
        get => _source == null;
        set
        {
            if (value)
            {
                _source = null;
            }
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Item TargetItem
    {
        get => _target as Item;
        set => _target = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile TargetMobile
    {
        get => _target as Mobile;
        set => _target = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool TargetNull
    {
        get => _target == null;
        set
        {
            if (value)
            {
                _target = null;
            }
        }
    }

    public override string DefaultName => "Effect Controller";

    public override bool HandlesOnMovement => _triggerType == EffectTriggerType.InRange;

    public override void OnDoubleClick(Mobile from)
    {
        if (_triggerType == EffectTriggerType.DoubleClick)
        {
            DoEffect(from);
        }
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (m.Location == oldLocation || _triggerType != EffectTriggerType.InRange)
        {
            return;
        }

        var worldLocation = GetWorldLocation();

        if (Utility.InRange(worldLocation, m.Location, _triggerRange) &&
            !Utility.InRange(worldLocation, oldLocation, _triggerRange))
        {
            DoEffect(m);
        }
    }

    public void PlaySound(IEntity trigger)
    {
        var ent = PlaySoundAtTrigger ? trigger : this;

        Effects.PlaySound((ent as Item)?.GetWorldLocation() ?? ent.Location, ent.Map, _soundId);
    }

    public void DoEffect(IEntity trigger)
    {
        if (Deleted || TriggerType == EffectTriggerType.None)
        {
            return;
        }

        if (trigger is Mobile { Hidden: true, AccessLevel: > AccessLevel.Player })
        {
            return;
        }

        if (_soundId > 0)
        {
            Timer.StartTimer(SoundDelay, () => PlaySound(trigger));
        }

        if (Sequence != null)
        {
            var sequence = Sequence;
            Timer.StartTimer(TriggerDelay, () => sequence.DoEffect(trigger));
        }

        if (EffectType != ECEffectType.None)
        {
            Timer.StartTimer(EffectDelay, () => InternalDoEffect(trigger));
        }
    }

    public void InternalDoEffect(IEntity trigger)
    {
        var from = _source ?? trigger;
        var to = _target ?? trigger;

        switch (EffectType)
        {
            case ECEffectType.Lightning:
                {
                    Effects.SendBoltEffect(from, false, EffectHue);
                    break;
                }
            case ECEffectType.Location:
                {
                    Effects.SendLocationParticles(
                        EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration),
                        _effectItemId,
                        _speed,
                        _duration,
                        _effectHue,
                        _renderMode,
                        _particleEffect,
                        _unknown
                    );
                    break;
                }
            case ECEffectType.Moving:
                {
                    if (from == this)
                    {
                        from = EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration);
                    }

                    if (to == this)
                    {
                        to = EffectItem.Create(to.Location, to.Map, EffectItem.DefaultDuration);
                    }

                    Effects.SendMovingParticles(
                        from,
                        to,
                        _effectItemId,
                        _speed,
                        _duration,
                        _fixedDirection,
                        _explodes,
                        _effectHue,
                        _renderMode,
                        _particleEffect,
                        _explodeParticleEffect,
                        _explodeSound,
                        _effectLayer,
                        _unknown
                    );
                    break;
                }
            case ECEffectType.Target:
                {
                    Effects.SendTargetParticles(
                        from,
                        _effectItemId,
                        _speed,
                        _duration,
                        _effectHue,
                        _renderMode,
                        _particleEffect,
                        _effectLayer,
                        _unknown
                    );
                    break;
                }
        }
    }
}
