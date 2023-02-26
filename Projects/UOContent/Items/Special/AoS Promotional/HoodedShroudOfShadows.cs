using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x2684, 0x2683)]
[SerializationGenerator(0, false)]
public partial class HoodedShroudOfShadows : BaseOuterTorso
{
    [Constructible]
    public HoodedShroudOfShadows(int hue = 0x455) : base(0x2684, hue)
    {
        LootType = LootType.Blessed;
        Weight = 3.0;
    }

    public override bool Dye(Mobile from, DyeTub sender)
    {
        from.SendLocalizedMessage(sender.FailMessage);
        return false;
    }
}
