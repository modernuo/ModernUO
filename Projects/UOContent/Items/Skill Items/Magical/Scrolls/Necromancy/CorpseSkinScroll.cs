using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CorpseSkinScroll : SpellScroll
{
    [Constructible]
    public CorpseSkinScroll(int amount = 1) : base(102, 0x2262, amount)
    {
    }
}
