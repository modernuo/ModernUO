using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HarmScroll : SpellScroll
{
    [Constructible]
    public HarmScroll(int amount = 1) : base(11, 0x1F38, amount)
    {
    }
}
