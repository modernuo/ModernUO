using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(1, false)]
public partial class PricedHealer : BaseHealer
{
    [Constructible]
    public PricedHealer(int price = 5000) : base(price)
    {
        if (!Core.AOS)
        {
            NameHue = 0x35;
        }
    }

    public override bool IsInvulnerable => true;

    public override bool HealsYoungPlayers => false;

    private void MigrateFrom(V0Content content)
    {
        Price = content.Price;
    }
}
