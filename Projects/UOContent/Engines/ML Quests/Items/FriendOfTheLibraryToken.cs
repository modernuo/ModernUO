using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FriendOfTheLibraryToken : Item
{
    [Constructible]
    public FriendOfTheLibraryToken() : base(0x2F58)
    {
        Layer = Layer.Talisman;
        Hue = 0x28A;
    }

    public override int LabelNumber => 1073136; // Friend of the Library Token (allows donations to be made)
}
