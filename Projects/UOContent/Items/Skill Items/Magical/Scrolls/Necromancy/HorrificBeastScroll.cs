using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HorrificBeastScroll : SpellScroll
{
    [Constructible]
    public HorrificBeastScroll(int amount = 1) : base(105, 0x2265, amount)
    {
    }
}
