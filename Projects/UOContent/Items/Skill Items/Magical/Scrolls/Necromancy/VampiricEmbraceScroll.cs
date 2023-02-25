using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class VampiricEmbraceScroll : SpellScroll
{
    [Constructible]
    public VampiricEmbraceScroll(int amount = 1) : base(112, 0x226C, amount)
    {
    }
}
