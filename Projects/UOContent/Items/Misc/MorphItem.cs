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

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int OutsideRange
    {
        get => _outsideRange;
        set => _outsideRange = Math.Clamp(value, 0, 18);
    }

    [SerializableProperty(3)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int InsideRange
    {
        get => _insideRange;
        set => _insideRange = Math.Clamp(value, 0, 18);
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
