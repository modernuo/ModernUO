using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RegStone : Item
{
    [Constructible]
    public RegStone() : base(0xED4)
    {
        Movable = false;
        Hue = 0x2D1;
    }

    public override string DefaultName => "a reagent stone";

    public override void OnDoubleClick(Mobile from)
    {
        var regBag = new BagOfReagents();

        if (!from.AddToBackpack(regBag))
        {
            regBag.Delete();
        }
    }
}
