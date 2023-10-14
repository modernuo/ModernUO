using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Necro;

[SerializationGenerator(0, false)]
public partial class DarkTidesHorn : HornOfRetreat
{
    [Constructible]
    public DarkTidesHorn()
    {
        DestLoc = new Point3D(2103, 1319, -68);
        DestMap = Map.Malas;
    }

    public override bool ValidateUse(Mobile from) => from is PlayerMobile { Quest: DarkTidesQuest };
}
