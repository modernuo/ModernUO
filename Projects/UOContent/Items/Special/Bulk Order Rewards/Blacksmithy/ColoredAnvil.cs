using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Anvil]
[SerializationGenerator(0, false)]
[Flippable(0xFAF, 0xFB0)]
public partial class ColoredAnvil : Item
{
    [Constructible]
    public ColoredAnvil() : base(0xFAF)
    {
        // TODO: Color weighted by rarity?
        Hue = CraftResources.GetRandomResource(CraftResource.DullCopper, CraftResource.Valorite)?.Hue ?? 0;
        Weight = 20;
    }
}
