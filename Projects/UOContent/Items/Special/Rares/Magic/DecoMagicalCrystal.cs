using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoMagicalCrystal : Item
{
    [Constructible]
    public DecoMagicalCrystal() : base(0x1F19)
    {
        Movable = true;
        Stackable = false;
    }
}
