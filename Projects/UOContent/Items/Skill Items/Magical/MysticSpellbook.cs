using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MysticSpellbook : Spellbook
{
    [Constructible]
    public MysticSpellbook(ulong content = 0) : base(content, 0x2D9D) => Layer = Layer.OneHanded;

    public override SpellbookType SpellbookType => SpellbookType.Mystic;

    public override int BookOffset => 677;
    public override int BookCount => 16;
}
