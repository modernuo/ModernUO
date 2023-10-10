using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ScribeBag : Bag
{
    [Constructible]
    public ScribeBag(int amount = 5000)
    {
        Hue = 0x105;
        DropItem(new BagOfReagents(amount));
        DropItem(new BlankScroll(amount));
    }

    public override string DefaultName => "a Scribe Kit";
}
