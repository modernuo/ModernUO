using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BlessScroll : SpellScroll
{
    [Constructible]
    public BlessScroll(int amount = 1) : base(16, 0x1F3D, amount)
    {
    }
}
