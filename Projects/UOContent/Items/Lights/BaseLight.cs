using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(1, false)]
public abstract partial class BaseLight : Item
{
    public static readonly bool Burnout = false;

    [SerializableField(0)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private bool _burntOut;

    // Field 1
    private bool _burning;

    // Field 2
    private TimeSpan _duration = TimeSpan.Zero;

    [SerializableField(3)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private bool _protected;

    [TimerDrift]
    [SerializableField(4, getter: "private", setter: "private")]
    private Timer _burnTimer;

    [DeserializeTimerField(4)]
    private void DeserializeTimer(TimeSpan delay)
    {
        if (_burning && _duration != TimeSpan.Zero)
        {
            DoTimer(delay);
        }
    }

    [Constructible]
    public BaseLight(int itemID) : base(itemID)
    {
    }

    public abstract int LitItemID { get; }

    public virtual int UnlitItemID => 0;
    public virtual int BurntOutItemID => 0;

    public virtual int LitSound => 0x47;
    public virtual int UnlitSound => 0x3be;
    public virtual int BurntOutSound => 0x4b8;

    [SerializableField(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Burning
    {
        get => _burning;
        set
        {
            if (_burning != value)
            {
                _burning = true;
                DoTimer(_duration);
                this.MarkDirty();
            }
        }
    }

    [SerializableField(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public TimeSpan Duration
    {
        get => _duration != TimeSpan.Zero && _burning && _burnTimer != null ? _burnTimer.Next - Core.Now : _duration;
        set
        {
            _duration = value;
            this.MarkDirty();
        }
    }

    public virtual void PlayLitSound()
    {
        if (LitSound != 0)
        {
            var loc = GetWorldLocation();
            Effects.PlaySound(loc, Map, LitSound);
        }
    }

    public virtual void PlayUnlitSound()
    {
        var sound = UnlitSound;

        if (BurntOut && BurntOutSound != 0)
        {
            sound = BurntOutSound;
        }

        if (sound != 0)
        {
            var loc = GetWorldLocation();
            Effects.PlaySound(loc, Map, sound);
        }
    }

    public virtual void Ignite()
    {
        if (!BurntOut)
        {
            PlayLitSound();

            _burning = true;
            ItemID = LitItemID;
            DoTimer(_duration);
        }
    }

    public virtual void Douse()
    {
        _burning = false;

        ItemID = BurntOut && BurntOutItemID != 0 ? BurntOutItemID : UnlitItemID;

        if (BurntOut)
        {
            _duration = TimeSpan.Zero;
        }
        else if (_duration != TimeSpan.Zero)
        {
            _duration = _burnTimer.Next - Core.Now;
        }

        _burnTimer?.Stop();
        this.MarkDirty();

        PlayUnlitSound();
    }

    public virtual void Burn()
    {
        BurntOut = true;
        Douse();
    }

    private void DoTimer(TimeSpan delay)
    {
        _duration = delay;
        _burnTimer?.Stop();
        this.MarkDirty();

        if (delay == TimeSpan.Zero)
        {
            return;
        }

        _burnTimer = new InternalTimer(this, delay);
        _burnTimer.Start();
        this.MarkDirty();
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_burntOut)
        {
            return;
        }

        if (_protected && from.AccessLevel == AccessLevel.Player)
        {
            return;
        }

        if (!from.InRange(GetWorldLocation(), 2))
        {
            return;
        }

        if (!_burning)
        {
            Ignite();
        }
        else if (UnlitItemID != 0)
        {
            Douse();
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _burntOut = reader.ReadBool();
        _burning = reader.ReadBool();
        _duration = reader.ReadTimeSpan();
        _protected = reader.ReadBool();

        if (_burning && _duration != TimeSpan.Zero)
        {
            DoTimer(reader.ReadDeltaTime() - Core.Now);
        }
    }

    private class InternalTimer : Timer
    {
        private readonly BaseLight m_Light;

        public InternalTimer(BaseLight light, TimeSpan delay) : base(delay) => m_Light = light;

        protected override void OnTick()
        {
            if (m_Light?.Deleted == false)
            {
                m_Light.Burn();
            }
        }
    }
}
