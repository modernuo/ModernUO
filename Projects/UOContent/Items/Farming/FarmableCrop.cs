using System;
using ModernUO.Serialization;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0)]
public abstract partial class FarmableCrop : Item
{
    [SerializableField(0)]
    private bool _picked;

    public FarmableCrop(int itemID) : base(itemID) => Movable = false;

    public abstract Item GetCropObject();
    public abstract int GetPickedID();

    public override void OnDoubleClick(Mobile from)
    {
        var map = Map;
        var loc = Location;

        if (Parent != null || Movable || IsLockedDown || IsSecure || map == null || map == Map.Internal)
        {
            return;
        }

        if (!from.InRange(loc, 2) || !from.InLOS(this))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else if (!_picked)
        {
            OnPicked(from, loc, map);
        }
    }

    public virtual void OnPicked(Mobile from, Point3D loc, Map map)
    {
        ItemID = GetPickedID();

        var spawn = GetCropObject();

        spawn?.MoveToWorld(loc, map);

        _picked = true;

        Unlink();

        Timer.StartTimer(TimeSpan.FromMinutes(5.0), Delete);
    }

    public void Unlink()
    {
        if (Spawner != null)
        {
            Spawner.Remove(this);
            Spawner = null;
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_picked)
        {
            Unlink();
            Delete();
        }
    }
}
