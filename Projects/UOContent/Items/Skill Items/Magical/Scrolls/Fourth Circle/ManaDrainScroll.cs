using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ManaDrainScroll : SpellScroll
{
    [Constructible]
    public ManaDrainScroll(int amount = 1) : base(30, 0x1F4B, amount)
    {
    }
}
