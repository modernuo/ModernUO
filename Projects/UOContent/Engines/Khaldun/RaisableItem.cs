using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class RaisableItem : Item
{
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _moveSound;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _stopSound;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _closeDelay;

    [SerializableField(4, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _elevation;

    private RaiseTimer _raiseTimer;

    [Constructible]
    public RaisableItem(int itemID) : this(itemID, 20, -1, -1, TimeSpan.FromMinutes(1.0))
    {
    }

    [Constructible]
    public RaisableItem(int itemID, int maxElevation, TimeSpan closeDelay) : this(
        itemID,
        maxElevation,
        -1,
        -1,
        closeDelay
    )
    {
    }

    [Constructible]
    public RaisableItem(int itemID, int maxElevation, int moveSound, int stopSound, TimeSpan closeDelay) : base(itemID)
    {
        Movable = false;

        _maxElevation = maxElevation;
        MoveSound = moveSound;
        StopSound = stopSound;
        CloseDelay = closeDelay;
    }

    [EncodedInt]
    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int MaxElevation
    {
        get => _maxElevation;
        set
        {
            _maxElevation = value switch
            {
                <= 0  => 0,
                >= 60 => 60,
                _     => value
            };

            this.MarkDirty();
        }
    }

    public bool IsRaisable => _raiseTimer == null;

    public void Raise()
    {
        if (!IsRaisable)
        {
            return;
        }

        _raiseTimer = new RaiseTimer(this);
        _raiseTimer.Start();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Z -= _elevation;
    }

    private class RaiseTimer : Timer
    {
        private readonly DateTime _closeTime;
        private readonly RaisableItem _item;
        private int _step;
        private bool _up;

        public RaiseTimer(RaisableItem item) : base(TimeSpan.Zero, TimeSpan.FromSeconds(0.5))
        {
            _item = item;
            _closeTime = Core.Now + item.CloseDelay;
            _up = true;
        }

        protected override void OnTick()
        {
            if (_item.Deleted)
            {
                Stop();
                return;
            }

            if (_step++ % 3 == 0)
            {
                if (_up)
                {
                    _item.Z++;

                    if (++_item._elevation >= _item.MaxElevation)
                    {
                        Stop();

                        if (_item.StopSound >= 0)
                        {
                            Effects.PlaySound(_item.Location, _item.Map, _item.StopSound);
                        }

                        _up = false;
                        _step = 0;

                        var delay = _closeTime - Core.Now;

                        StartTimer(delay, () => Start());

                        return;
                    }
                }
                else
                {
                    _item.Z--;

                    if (--_item._elevation <= 0)
                    {
                        Stop();

                        if (_item.StopSound >= 0)
                        {
                            Effects.PlaySound(_item.Location, _item.Map, _item.StopSound);
                        }

                        _item._raiseTimer = null;

                        return;
                    }
                }
            }

            if (_item.MoveSound >= 0)
            {
                Effects.PlaySound(_item.Location, _item.Map, _item.MoveSound);
            }
        }
    }
}
