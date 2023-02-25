using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class IncognitoScroll : SpellScroll
{
    [Constructible]
    public IncognitoScroll(int amount = 1) : base(34, 0x1F4F, amount)
    {
    }
}
