using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SweatOfParoxysmus : Item
{
    [Constructible]
    public SweatOfParoxysmus() : base(0xF01)
    {
    }

    public override int LabelNumber => 1072081; // Sweat of Paroxysmus
}
