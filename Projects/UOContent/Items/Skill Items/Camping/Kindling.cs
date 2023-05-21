using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Regions;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Kindling : Item
{
    [Constructible]
    public Kindling(int amount = 1) : base(0xDE1)
    {
        Stackable = true;
        Weight = 5.0;
        Amount = amount;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!VerifyMove(from))
        {
            return;
        }

        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        var fireLocation = GetFireLocation(from);

        if (fireLocation == Point3D.Zero)
        {
            from.SendLocalizedMessage(501695); // There is not a spot nearby to place your campfire.
        }
        else if (!from.CheckSkill(SkillName.Camping, 0.0, 100.0))
        {
            from.SendLocalizedMessage(501696); // You fail to ignite the campfire.
        }
        else
        {
            Consume();

            if (!Deleted && Parent == null)
            {
                from.PlaceInBackpack(this);
            }

            new Campfire().MoveToWorld(fireLocation, from.Map);
        }
    }

    private Point3D GetFireLocation(Mobile from)
    {
        if (from.Region.IsPartOf<DungeonRegion>())
        {
            return Point3D.Zero;
        }

        if (Parent == null)
        {
            return Location;
        }

        var list = new List<Point3D>(4);

        AddOffsetLocation(from, 0, -1, list);
        AddOffsetLocation(from, -1, 0, list);
        AddOffsetLocation(from, 0, 1, list);
        AddOffsetLocation(from, 1, 0, list);

        if (list.Count == 0)
        {
            return Point3D.Zero;
        }

        return list.RandomElement();
    }

    private static void AddOffsetLocation(Mobile from, int offsetX, int offsetY, List<Point3D> list)
    {
        var map = from.Map;

        var x = from.X + offsetX;
        var y = from.Y + offsetY;

        var loc = new Point3D(x, y, from.Z);

        if (map.CanFit(loc, 1) && from.InLOS(loc))
        {
            list.Add(loc);
        }
        else
        {
            loc = new Point3D(x, y, map.GetAverageZ(x, y));

            if (map.CanFit(loc, 1) && from.InLOS(loc))
            {
                list.Add(loc);
            }
        }
    }
}
