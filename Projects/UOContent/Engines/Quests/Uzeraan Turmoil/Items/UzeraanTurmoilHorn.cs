using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven;

[SerializationGenerator(0, false)]
public partial class UzeraanTurmoilHorn : HornOfRetreat
{
    [Constructible]
    public UzeraanTurmoilHorn()
    {
        DestLoc = new Point3D(3597, 2582, 0);
        DestMap = Map.Trammel;
    }

    public override bool ValidateUse(Mobile from) => from is PlayerMobile pm && pm.Quest is UzeraanTurmoilQuest;
}
