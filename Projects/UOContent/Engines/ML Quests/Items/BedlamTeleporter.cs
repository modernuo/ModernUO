using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Items;

[SerializationGenerator(0, false)]
public partial class BedlamTeleporter : Item
{
    private static readonly Point3D PointDest = new(120, 1682, 0);
    private static readonly Map MapDest = Map.Malas;

    public BedlamTeleporter() : base(0x124D) => Movable = false;

    public override int LabelNumber => 1074161; // Access to Bedlam by invitation only

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (from is PlayerMobile mobile && MLQuestSystem.GetContext(mobile)?.BedlamAccess == true)
        {
            BaseCreature.TeleportPets(mobile, PointDest, MapDest);
            mobile.MoveToWorld(PointDest, MapDest);
        }
        else
        {
            from.SendLocalizedMessage(1074276); // You press and push on the iron maiden, but nothing happens.
        }
    }
}
