using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AnimatedWeaponScroll : SpellScroll
{
    [Constructible]
    public AnimatedWeaponScroll(int amount = 1) : base(683, 0x2DA4, amount)
    {
    }
}
