using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseWall : Item
    {
        public BaseWall(int itemID) : base(itemID) => Movable = false;
    }
}
