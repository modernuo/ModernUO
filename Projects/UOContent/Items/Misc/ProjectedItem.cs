using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class ProjectedItem : Item
{
    private static readonly HashSet<ProjectedItem> _active = [];
    private static Timer _timer;

    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [SerializableField(0)]
    private int _effectItemId;

    [SerializedCommandProperty(AccessLevel.Developer)]
    [SerializableField(1)]
    private AccessLevel _minimumVisible;

    private bool _oldVisible;

    [Constructible]
    public ProjectedItem(int effectItemId) : base(1)
    {
        Movable = false;
        Visible = false;
        _effectItemId = effectItemId;
    }

    public override bool HandlesOnMovement => true;

    public override bool CanSeeStaffOnly(Mobile from) => from.AccessLevel >= _minimumVisible;

    public override void ProcessDelta()
    {
        if (_oldVisible != Visible)
        {
            Timer.DelayCall(
                TimeSpan.Zero,
                i =>
                {
                    i.ItemID = i.Visible ? i._effectItemId : 1;
                    i._oldVisible = i.Visible;
                    i.Activate();
                },
                this
            );
        }

        base.ProcessDelta();
    }

    public override void SendInfoTo(NetState ns, ReadOnlySpan<byte> world = default)
    {
        base.SendInfoTo(ns, world);

        var m = ns.Mobile;
        if (m != null && !Visible && m.AccessLevel >= _minimumVisible && !_active.Contains(this))
        {
            Activate();
        }
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);
        Activate();
    }

    public override void OnMapChange()
    {
        base.OnMapChange();
        Activate();
    }

    public override void OnDelete()
    {
        Deactivate();
        base.OnDelete();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _oldVisible = Visible;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (!Visible && m.AccessLevel >= _minimumVisible && !_active.Contains(this))
        {
            Activate();
        }
    }

    private void Activate()
    {
        if (!_active.Add(this))
        {
            return;
        }

        if (_timer == null)
        {
            _timer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(1.0), OnTick);
        }
        else if (!_timer.Running)
        {
            _timer.Interval = TimeSpan.FromSeconds(1.0);
            _timer.Start();
        }
    }

    private void Deactivate()
    {
        if (_active.Remove(this) && _active.Count == 0)
        {
            _timer?.Stop();
        }
    }

    private static void OnTick()
    {
        using var queue = PooledRefQueue<Item>.Create();
        foreach (var item in _active)
        {
            if (!item.SendEffect())
            {
                queue.Enqueue(item);
            }
        }

        while (queue.Count > 0)
        {
            _active.Remove(queue.Dequeue() as ProjectedItem);
        }

        if (_active.Count == 0)
        {
            _timer?.Stop();
        }
    }

    protected virtual bool SendEffect()
    {
        if (_oldVisible)
        {
            return false;
        }

        var found = false;
        Span<byte> buffer = new byte[OutgoingEffectPackets.HuedEffectLength].InitializePacket();
        OutgoingEffectPackets.CreateHuedEffect(
            buffer,
            EffectType.FixedXYZ,
            Serial,
            Serial,
            _effectItemId,
            Location,
            Location,
            1,
            20,
            true,
            false,
            Hue - 1,
            3
        );

        foreach (var ns in GetClientsInRange(GetMaxUpdateRange()))
        {
            found = true;
            var from = ns.Mobile;
            if (ns.CannotSendPackets() || from == null || from.AccessLevel < _minimumVisible)
            {
                continue;
            }

            from.ProcessDelta();
            ns.Send(buffer);
        }

        return found;
    }
}
