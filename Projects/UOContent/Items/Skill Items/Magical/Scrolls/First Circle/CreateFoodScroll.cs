using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CreateFoodScroll : SpellScroll
{
    [Constructible]
    public CreateFoodScroll(int amount = 1) : base(1, 0x1F2F, amount)
    {
    }
}
