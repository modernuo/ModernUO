using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RedLeatherBook : BlueBook
{
    [Constructible]
    public RedLeatherBook() => Hue = 0x485;
}
