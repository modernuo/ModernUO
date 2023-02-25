using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CurseWeaponScroll : SpellScroll
{
    [Constructible]
    public CurseWeaponScroll(int amount = 1) : base(103, 0x2263, amount)
    {
    }
}
