using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BookOfNinjitsu : Spellbook
{
    [Constructible]
    public BookOfNinjitsu(ulong content = 0xFF) : base(content, 0x23A0) =>
        Layer = Core.ML ? Layer.OneHanded : Layer.Invalid;

    public override SpellbookType SpellbookType => SpellbookType.Ninja;
    public override int BookOffset => 500;
    public override int BookCount => 8;
}
