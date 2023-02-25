using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MeteorSwarmScroll : SpellScroll
{
    [Constructible]
    public MeteorSwarmScroll(int amount = 1) : base(54, 0x1F63, amount)
    {
    }
}
