using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[Flippable(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
[SerializationGenerator(0, false)]
public partial class WayPoint : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private WayPoint _nextPoint;

    [Constructible]
    public WayPoint(WayPoint prev = null) : base(0x1f14)
    {
        Hue = 0x498;
        Visible = false;
        // this.Movable = false;

        if (prev != null)
        {
            prev.NextPoint = this;
        }
    }

    public override string DefaultName => "AI Way Point";

    public static void Initialize()
    {
        CommandSystem.Register("WayPointSeq", AccessLevel.GameMaster, WayPointSeq_OnCommand);
    }

    public static void WayPointSeq_OnCommand(CommandEventArgs arg)
    {
        arg.Mobile.SendMessage("Target the position of the first way point.");
        arg.Mobile.Target = new WayPointSeqTarget(null);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            from.SendMessage("Target the next way point in the sequence.");

            from.Target = new NextPointTarget(this);
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        if (_nextPoint == null)
        {
            LabelTo(from, "(Unlinked)");
        }
        else
        {
            LabelTo(from, "(Linked: {0})", _nextPoint.Location);
        }
    }
}

public class NextPointTarget : Target
{
    private WayPoint _point;

    public NextPointTarget(WayPoint pt) : base(-1, false, TargetFlags.None) => _point = pt;

    protected override void OnTarget(Mobile from, object target)
    {
        if (target is WayPoint point && _point != null)
        {
            _point.NextPoint = point;
        }
        else
        {
            from.SendMessage("Target a way point.");
        }
    }
}

public class WayPointSeqTarget : Target
{
    private WayPoint _last;

    public WayPointSeqTarget(WayPoint last) : base(-1, true, TargetFlags.None) => _last = last;

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (targeted is WayPoint wayPoint)
        {
            if (_last != null)
            {
                _last.NextPoint = wayPoint;
            }
        }
        else if (targeted is IPoint3D d)
        {
            var p = new Point3D(d);

            var point = new WayPoint(_last);
            point.MoveToWorld(p, from.Map);

            from.Target = new WayPointSeqTarget(point);
            from.SendMessage(
                "Target the position of the next way point in the sequence, or target a way point link the newest way point to."
            );
        }
        else
        {
            from.SendMessage("Target a position, or another way point.");
        }
    }
}
