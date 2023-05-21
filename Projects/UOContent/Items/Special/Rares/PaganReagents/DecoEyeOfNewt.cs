using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoEyeOfNewt : Item
{
    [Constructible]
    public DecoEyeOfNewt() : base(0xF87)
    {
        Movable = true;
        Stackable = false;
    }
}
