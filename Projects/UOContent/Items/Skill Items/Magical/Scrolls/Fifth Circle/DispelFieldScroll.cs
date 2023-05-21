using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DispelFieldScroll : SpellScroll
{
    [Constructible]
    public DispelFieldScroll(int amount = 1) : base(33, 0x1F4E, amount)
    {
    }
}
