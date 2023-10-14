using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AlchemyStone : Item
{
    [Constructible]
    public AlchemyStone() : base(0xED4)
    {
        Movable = false;
        Hue = 0x250;
    }

    public override string DefaultName => "an Alchemist Supply Stone";

    public override void OnDoubleClick(Mobile from)
    {
        var alcBag = new AlchemyBag();

        if (!from.AddToBackpack(alcBag))
        {
            alcBag.Delete();
        }
    }
}
