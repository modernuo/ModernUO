using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BookOfBushido : Spellbook
{
    [Constructible]
    public BookOfBushido(ulong content = 0x3F) : base(content, 0x238C) =>
        Layer = Core.ML ? Layer.OneHanded : Layer.Invalid;

    public override SpellbookType SpellbookType => SpellbookType.Samurai;
    public override int BookOffset => 400;
    public override int BookCount => 6;
}
