using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator( 0 )]
public partial class CitadelAltar : PeerlessAltar
{
    private readonly Rectangle2D[] m_Bounds =
    [
        new Rectangle2D( 66, 1936, 51, 39 )
    ];

    public override BasePeerless Boss => new Travesty();

    [Constructible]
    public CitadelAltar() : base( 0x207E )
    {
        BossLocation = new Point3D( 86, 1955, 0 );
        TeleportDest = new Point3D( 111, 1955, 0 );
        ExitDest = new Point3D( 1355, 779, 17 );
    }

    public override MasterKey MasterKey => new CitadelKey();

    //TODO
    public override Type[] Keys => new[]
    {
        typeof( TigerClawKey ), typeof( SerpentFangKey ), typeof( DragonFlameKey )
    };

    public override int KeyCount => 3;

    public override Rectangle2D[] BossBounds => m_Bounds;
}
