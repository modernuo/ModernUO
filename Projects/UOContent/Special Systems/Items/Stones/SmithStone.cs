using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SmithStone : Item
{
    [Constructible]
    public SmithStone() : base(0xED4)
    {
        Movable = false;
        Hue = 0x476;
    }

    public override string DefaultName => "a Blacksmith Supply Stone";

    public override void OnDoubleClick(Mobile from)
    {
        var SmithBag = new SmithBag();

        if (!from.AddToBackpack(SmithBag))
        {
            SmithBag.Delete();
        }
    }
}
