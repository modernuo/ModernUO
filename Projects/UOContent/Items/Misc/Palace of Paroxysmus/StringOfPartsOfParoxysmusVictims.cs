using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class StringOfPartsOfParoxysmusVictims : Item
{
    [Constructible]
    public StringOfPartsOfParoxysmusVictims() : base(0xFD2)
    {
    }

    public override int LabelNumber => 1072082; // String of Parts of Paroxysmus' Victims
}
