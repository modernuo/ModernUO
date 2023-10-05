using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseSign : Item
{
    public BaseSign(int dispID) : base(dispID) => Movable = false;
}
