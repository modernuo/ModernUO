using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ClumsyScroll : SpellScroll
{
    [Constructible]
    public ClumsyScroll(int amount = 1) : base(0, 0x1F2E, amount)
    {
    }
}
