using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class KhaldunPitTeleporter : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _active;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _pointDest;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Map _mapDest;

    [Constructible]
    public KhaldunPitTeleporter() : this(new Point3D(5451, 1374, 0), Map.Felucca)
    {
    }

    [Constructible]
    public KhaldunPitTeleporter(Point3D pointDest, Map mapDest) : base(0x053B)
    {
        Movable = false;
        Hue = 1;

        _active = true;
        _pointDest = pointDest;
        _mapDest = mapDest;
    }

    // the floor of the cavern seems to have collapsed here - a faint light is visible at the bottom of the pit
    public override int LabelNumber => 1016511;

    public override void OnDoubleClick(Mobile m)
    {
        if (!Active)
        {
            return;
        }

        var map = MapDest;

        if (map != null && map != Map.Internal && m.InRange(this, 3))
        {
            BaseCreature.TeleportPets(m, PointDest, MapDest);
            m.MoveToWorld(PointDest, MapDest);
        }
        else
        {
            m.SendLocalizedMessage(1019045); // I can't reach that.
        }
    }

    public override void OnDoubleClickDead(Mobile m)
    {
        OnDoubleClick(m);
    }
}
