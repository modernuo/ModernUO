using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PortcullisNS : BaseDoor
{
    [Constructible]
    public PortcullisNS() : base(0x6F5, 0x6F5, 0xF0, 0xEF, new Point3D(0, 0, 20))
    {
    }
}

[SerializationGenerator(0, false)]
public partial class PortcullisEW : BaseDoor
{
    [Constructible]
    public PortcullisEW() : base(0x6F6, 0x6F6, 0xF0, 0xEF, new Point3D(0, 0, 20))
    {
    }
}
