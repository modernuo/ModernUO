using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Anvil]
[Flippable(0xFAF, 0xFB0)]
[SerializationGenerator(0, false)]
public partial class Anvil : Item
{
    [Constructible]
    public Anvil() : base(0xFAF) => Movable = false;
}

[Forge]
[SerializationGenerator(0, false)]
public partial class Forge : Item
{
    [Constructible]
    public Forge() : base(0xFB1) => Movable = false;
}
