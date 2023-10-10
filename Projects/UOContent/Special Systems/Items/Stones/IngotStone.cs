using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class IngotStone : Item
{
    [Constructible]
    public IngotStone() : base(0xED4)
    {
        Movable = false;
        Hue = 0x480;
    }

    public override string DefaultName => "an Ingot stone";

    public override void OnDoubleClick(Mobile from)
    {
        var ingotBag = new BagOfingots();

        if (!from.AddToBackpack(ingotBag))
        {
            ingotBag.Delete();
        }
    }
}
