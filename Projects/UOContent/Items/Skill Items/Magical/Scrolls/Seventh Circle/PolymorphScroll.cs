using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PolymorphScroll : SpellScroll
{
    [Constructible]
    public PolymorphScroll(int amount = 1) : base(55, 0x1F64, amount)
    {
    }
}
