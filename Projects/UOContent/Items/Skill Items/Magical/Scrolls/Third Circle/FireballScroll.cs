using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FireballScroll : SpellScroll
{
    [Constructible]
    public FireballScroll(int amount = 1) : base(17, 0x1F3E, amount)
    {
    }
}
