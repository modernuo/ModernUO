using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MagicReflectScroll : SpellScroll
{
    [Constructible]
    public MagicReflectScroll(int amount = 1) : base(35, 0x1F50, amount)
    {
    }
}
