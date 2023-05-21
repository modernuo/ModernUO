using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class SpellweavingBook : Spellbook
{
    [Constructible]
    public SpellweavingBook(ulong content = 0) : base(content, 0x2D50)
    {
        Hue = 0x8A2;
        Layer = Layer.OneHanded;
    }


    public override SpellbookType SpellbookType => SpellbookType.Arcanist;
    public override int BookOffset => 600;
    public override int BookCount => 16;
}
