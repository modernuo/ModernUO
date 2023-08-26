using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class PricedHealer : BaseHealer
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _price;

    [Constructible]
    public PricedHealer(int price = 5000)
    {
        Price = price;

        if (!Core.AOS)
        {
            NameHue = 0x35;
        }
    }

    public override bool IsInvulnerable => true;

    public override bool HealsYoungPlayers => false;

    public override void InitSBInfo()
    {
    }

    public override void OfferResurrection(Mobile m)
    {
        Direction = GetDirectionTo(m);

        m.PlaySound(0x214);
        m.FixedEffect(0x376A, 10, 16);

        m.CloseGump<ResurrectGump>();
        m.SendGump(new ResurrectGump(m, this, Price));
    }

    public override bool CheckResurrect(Mobile m) => true;
}
