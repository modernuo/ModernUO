using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HarrowerGate : Moongate
{
    [SerializableField(0)]
    private Mobile _harrower;

    public HarrowerGate(Mobile harrower, Point3D loc, Map map, Point3D targLoc, Map targMap) : base(targLoc, targMap)
    {
        _harrower = harrower;

        Dispellable = false;
        ItemID = 0x1FD4;
        Light = LightType.Circle300;

        MoveToWorld(loc, map);
    }

    public override int LabelNumber => 1049498; // dark moongate

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_harrower == null)
        {
            Delete();
        }
    }
}
