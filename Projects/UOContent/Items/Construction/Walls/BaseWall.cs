namespace Server.Items
{
    [Serializable(0, false)]
    public abstract partial class BaseWall : Item
    {
        public BaseWall(int itemID) : base(itemID) => Movable = false;
    }
}
