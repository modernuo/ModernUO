using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0 )]
public partial class LibraryApplication : Item
{
    [Constructible]
    public LibraryApplication()
        : base( 0xEC0 )
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override int LabelNumber => 1073131; // Friends of the Library Application
}
