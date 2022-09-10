using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CandelabraOfSouls : Item
{
    [Constructible]
    public CandelabraOfSouls() : base(0xB26)
    {
    }
}
