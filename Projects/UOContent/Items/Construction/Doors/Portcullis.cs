namespace Server.Items;

public class PortcullisNS : BaseDoor
{
    [Constructible]
    public PortcullisNS() : base(0x6F5, 0x6F5, 0xF0, 0xEF, new Point3D(0, 0, 20))
    {
    }
}

public class PortcullisEW : BaseDoor
{
    [Constructible]
    public PortcullisEW() : base(0x6F6, 0x6F6, 0xF0, 0xEF, new Point3D(0, 0, 20))
    {
    }
}
