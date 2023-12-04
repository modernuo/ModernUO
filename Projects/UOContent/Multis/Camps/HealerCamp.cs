using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Multis;

[SerializationGenerator(0, false)]
public partial class HealerCamp : BaseCamp
{
    [Constructible]
    public HealerCamp() : base(0x1F4)
    {
    }

    public override void AddComponents()
    {
        BaseDoor west, east;

        AddItem(west = new LightWoodGate(DoorFacing.WestCW), -4, 4, 7);
        AddItem(east = new LightWoodGate(DoorFacing.EastCCW), -3, 4, 7);

        west.Link = east;
        east.Link = west;

        AddItem(new Sign(SignType.Healer, SignFacing.West), -5, 5, -4);

        AddMobile(new Healer(), 4, -4, 3, 7);
        AddMobile(new Healer(), 5, 4, -2, 0);
    }
}
