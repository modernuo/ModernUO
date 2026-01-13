using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class RaiseSwitch : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private RaisableItem _raisableItem;

    private ResetTimer _resetTimer;

    [Constructible]
    public RaiseSwitch(int itemID = 0x1093) : base(itemID) => Movable = false;

    public override void OnDoubleClick(Mobile m)
    {
        if (!m.InRange(this, 2))
        {
            m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (RaisableItem?.Deleted == true)
        {
            RaisableItem = null;
        }

        Flip();

        if (RaisableItem == null)
        {
            return;
        }

        if (RaisableItem.IsRaisable)
        {
            RaisableItem.Raise();
            m.LocalOverheadMessage(
                MessageType.Regular,
                0x5A,
                true,
                "You hear a grinding noise echoing in the distance."
            );
        }
        else
        {
            m.LocalOverheadMessage(
                MessageType.Regular,
                0x5A,
                true,
                "You flip the switch again, but nothing happens."
            );
        }
    }

    protected virtual void Flip()
    {
        if (ItemID != 0x1093)
        {
            ItemID = 0x1093;

            StopResetTimer();
        }
        else
        {
            ItemID = 0x1095;

            StartResetTimer(
                RaisableItem?.CloseDelay >= TimeSpan.Zero ? RaisableItem.CloseDelay : TimeSpan.FromMinutes(2.0)
            );
        }

        Effects.PlaySound(Location, Map, 0x3E8);
    }

    protected void StartResetTimer(TimeSpan delay)
    {
        StopResetTimer();

        _resetTimer = new ResetTimer(this, delay);
        _resetTimer.Start();
    }

    protected void StopResetTimer()
    {
        if (_resetTimer != null)
        {
            _resetTimer.Stop();
            _resetTimer = null;
        }
    }

    protected virtual void Reset()
    {
        if (ItemID != 0x1093)
        {
            Flip();
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Reset();
    }

    private class ResetTimer : Timer
    {
        private readonly RaiseSwitch _raiseSwitch;

        public ResetTimer(RaiseSwitch raiseSwitch, TimeSpan delay) : base(delay) => _raiseSwitch = raiseSwitch;

        protected override void OnTick()
        {
            if (_raiseSwitch.Deleted)
            {
                return;
            }

            _raiseSwitch._resetTimer = null;
            _raiseSwitch.Reset();
        }
    }
}

[SerializationGenerator(0)]
public partial class DisappearingRaiseSwitch : RaiseSwitch
{
    [Constructible]
    public DisappearingRaiseSwitch() : base(0x108F)
    {
    }

    public int CurrentRange => Visible ? 3 : 2;

    public override bool HandlesOnMovement => true;

    protected override void Flip()
    {
    }

    protected override void Reset()
    {
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (Utility.InRange(m.Location, Location, CurrentRange) || Utility.InRange(oldLocation, Location, CurrentRange))
        {
            Refresh();
        }
    }

    public override void OnMapChange()
    {
        if (!Deleted)
        {
            Refresh();
        }
    }

    public override void OnLocationChange(Point3D oldLoc)
    {
        if (!Deleted)
        {
            Refresh();
        }
    }

    [AfterDeserialization(false)]
    public void Refresh()
    {
        foreach (var mob in GetMobilesInRange(CurrentRange))
        {
            if (!mob.Hidden || mob.AccessLevel <= AccessLevel.Player)
            {
                Visible = true;
                break;
            }
        }
    }
}
