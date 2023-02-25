using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NecromancerSpellbook : Spellbook
{
    [Constructible]
    public NecromancerSpellbook(ulong content = 0) : base(content, 0x2253) =>
        Layer = Core.ML ? Layer.OneHanded : Layer.Invalid;

    public override SpellbookType SpellbookType => SpellbookType.Necromancer;
    public override int BookOffset => 100;
    public override int BookCount => Core.SE ? 17 : 16;
}
