using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MindBlastScroll : SpellScroll
{
    [Constructible]
    public MindBlastScroll(int amount = 1) : base(36, 0x1F51, amount)
    {
    }
}
