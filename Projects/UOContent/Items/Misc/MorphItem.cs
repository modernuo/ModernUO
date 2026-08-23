using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MorphItem : Item
{
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _inactiveItemId;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _activeItemId;

    [Constructible]
    public MorphItem(int inactiveItemID, int activeItemID, int range) : this(inactiveItemID, activeItemID, range, range)
    {
    }

    [Constructible]
    public MorphItem(int inactiveItemID, int activeItemID, int inRange, int outRange) : base(inactiveItemID)
    {
        Movable = false;

        _inactiveItemId = inactiveItemID;
        _activeItemId = activeItemID;
        _insideRange = inRange;
        _outsideRange = outRange;
    }

    [SerializableField(0, allowFieldChange: nameof(AllowOutsideRangeChange))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _outsideRange;

    private bool AllowOutsideRangeChange(ref int value)
    {
        value = Math.Clamp(value, 0, 18);
        return true;
    }

    [SerializableField(3, allowFieldChange: nameof(AllowInsideRangeChange))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _insideRange;

    private bool AllowInsideRangeChange(ref int value)
    {
        value = Math.Clamp(value, 0, 18);
        return true;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int CurrentRange => ItemID == _inactiveItemId ? _insideRange : _outsideRange;

    public override bool HandlesOnMovement => true;

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

    public void Refresh()
    {
        var found = false;
        foreach (var mob in GetMobilesInRange(CurrentRange))
        {
            if (!mob.Hidden || mob.AccessLevel <= AccessLevel.Player)
            {
                found = true;
                break;
            }
        }

        ItemID = found ? _activeItemId : _inactiveItemId;

        Visible = ItemID != 0x1;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Refresh();
    }
}
