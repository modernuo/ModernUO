using System;
using ModernUO.Serialization;
using Server.Collections;

namespace Server.Items;

[SerializationGenerator(1, false)]
public partial class WarningItem : Item
{
    private bool m_Broadcasting;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TextDefinition _warningMessage;

    // Field 1
    private int _range;

    [SerializableField(2)]
    private TimeSpan _resetDelay;

    private DateTime m_LastBroadcast;

    [Constructible]
    public WarningItem(int itemID, int range, int warning) : base(itemID)
    {
        Movable = false;

        _warningMessage = warning;
        _range = Math.Min(range, 18);
    }

    [Constructible]
    public WarningItem(int itemID, int range, string warning) : base(itemID)
    {
        Movable = false;

        _warningMessage = warning;
        _range = Math.Min(range, 18);
    }

    [CommandProperty(AccessLevel.GameMaster)]
    [SerializableProperty(1, useField: nameof(_range))]
    public int Range
    {
        get => _range;
        set
        {
            _range = Math.Min(value, 18);
            this.MarkDirty();
        }
    }

    public virtual bool OnlyToTriggerer => false;
    public virtual int NeighborRange => 5;

    public override bool HandlesOnMovement => true;

    public virtual void SendMessage(Mobile triggerer, bool onlyToTriggerer, TextDefinition warningMessage)
    {
        if (onlyToTriggerer)
        {
            warningMessage.SendMessageTo(triggerer);
        }
        else
        {
            warningMessage.PublicOverheadMessage(this, MessageType.Regular, 0x3B2);
        }
    }

    public virtual void Broadcast(Mobile triggerer)
    {
        if (m_Broadcasting || Core.Now < m_LastBroadcast + ResetDelay)
        {
            return;
        }

        m_LastBroadcast = Core.Now;

        m_Broadcasting = true;

        SendMessage(triggerer, OnlyToTriggerer, _warningMessage);

        if (NeighborRange >= 0)
        {
            using var queue = PooledRefQueue<WarningItem>.Create();
            foreach (var warningItem in GetItemsInRange<WarningItem>(NeighborRange))
            {
                if (warningItem != this)
                {
                    queue.Enqueue(warningItem);
                }
            }

            while (queue.Count > 0)
            {
                queue.Dequeue().Broadcast(triggerer);
            }
        }

        Timer.StartTimer(StopBroadcasting);
    }

    private void StopBroadcasting()
    {
        m_Broadcasting = false;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (m.Player && Utility.InRange(m.Location, Location, _range) &&
            !Utility.InRange(oldLocation, Location, _range))
        {
            Broadcast(m);
        }
    }

    public void Deserialize(IGenericReader reader, int version)
    {
        var warningMessageString = reader.ReadString();
        var warningMessageInt = reader.ReadInt();

        _range = reader.ReadInt();
        ResetDelay = reader.ReadTimeSpan();

        _warningMessage = warningMessageInt > 0 ? warningMessageInt : warningMessageString;
    }
}


[SerializationGenerator(1, false)]
public partial class HintItem : WarningItem
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TextDefinition _hintMessage;

    [Constructible]
    public HintItem(int itemID, int range, int warning, int hint) : base(itemID, range, warning) =>
        _hintMessage = hint;

    [Constructible]
    public HintItem(int itemID, int range, string warning, string hint) : base(itemID, range, warning) =>
        _hintMessage = hint;

    public override bool OnlyToTriggerer => true;

    public override void OnDoubleClick(Mobile from)
    {
        SendMessage(from, true, _hintMessage);
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        var hintMessageString = reader.ReadString();
        var hintMessageInt = reader.ReadInt();

        _hintMessage = hintMessageInt > 0 ? hintMessageInt : hintMessageString;
    }
}
