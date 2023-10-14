using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SmithBag : Bag
{
    [Constructible]
    public SmithBag(int amount = 5000)
    {
        DropItem(new DullCopperIngot(amount));
        DropItem(new ShadowIronIngot(amount));
        DropItem(new CopperIngot(amount));
        DropItem(new BronzeIngot(amount));
        DropItem(new GoldIngot(amount));
        DropItem(new AgapiteIngot(amount));
        DropItem(new VeriteIngot(amount));
        DropItem(new ValoriteIngot(amount));
        DropItem(new IronIngot(amount));
        DropItem(new Tongs(amount));
        DropItem(new TinkerTools(amount));
    }
}
