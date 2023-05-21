using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BookOfChivalry : Spellbook
{
    [Constructible]
    public BookOfChivalry(ulong content = 0x3FF) : base(content, 0x2252) =>
        Layer = Core.ML ? Layer.OneHanded : Layer.Invalid;

    public override SpellbookType SpellbookType => SpellbookType.Paladin;
    public override int BookOffset => 200;
    public override int BookCount => 10;
}
