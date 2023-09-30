using ModernUO.Serialization;

namespace Server.Items;

public enum SignFacing
{
    North,
    West
}

public enum SignType
{
    Library,
    DarkWoodenPost,
    LightWoodenPost,
    MetalPostC,
    MetalPostB,
    MetalPostA,
    MetalPost,
    Bakery,
    Tailor,
    Tinker,
    Butcher,
    Healer,
    Mage,
    Woodworker,
    Customs,
    Inn,
    Shipwright,
    Stables,
    BarberShop,
    Bard,
    Fletcher,
    Armourer,
    Jeweler,
    Tavern,
    ReagentShop,
    Blacksmith,
    Painter,
    Provisioner,
    Bowyer,
    WoodenSign,
    BrassSign,
    ArmamentsGuild,
    ArmourersGuild,
    BlacksmithsGuild,
    WeaponsGuild,
    BardicGuild,
    BartersGuild,
    ProvisionersGuild,
    TradersGuild,
    CooksGuild,
    HealersGuild,
    MagesGuild,
    SorcerersGuild,
    IllusionistGuild,
    MinersGuild,
    ArchersGuild,
    SeamensGuild,
    FishermensGuild,
    SailorsGuild,
    ShipwrightsGuild,
    TailorsGuild,
    ThievesGuild,
    RoguesGuild,
    AssassinsGuild,
    TinkersGuild,
    WarriorsGuild,
    CavalryGuild,
    FightersGuild,
    MerchantsGuild,
    Bank,
    Theatre
}

[SerializationGenerator(0, false)]
public partial class Sign : BaseSign
{
    [Constructible]
    public Sign(SignType type, SignFacing facing) : base(0xB95 + 2 * (int)type + (int)facing)
    {
    }

    [Constructible]
    public Sign(int itemID) : base(itemID)
    {
    }
}
