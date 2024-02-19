using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Cauldron : Item
{
    [Constructible]
    public Cauldron() : base(0x9ED) => Weight = 1.0;

    public override string DefaultName => "a cauldron";
}
