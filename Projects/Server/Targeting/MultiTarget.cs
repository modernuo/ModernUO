using Server.Network;

namespace Server.Targeting;

public abstract class MultiTarget : Target
{
    protected MultiTarget(
        int multiID, Point3D offset, int range = 10, bool allowGround = true,
        TargetFlags flags = TargetFlags.None
    )
        : base(range, allowGround, flags)
    {
        MultiID = multiID;
        Offset = offset;
    }

    public int MultiID { get; set; }

    public Point3D Offset { get; set; }

    public override void SendTargetTo(NetState ns) => ns.SendMultiTargetReq(this);
}
