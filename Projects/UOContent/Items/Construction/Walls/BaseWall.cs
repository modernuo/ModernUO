namespace Server.Items
{
    public abstract class BaseWall : Item
    {
        public BaseWall(int itemID) : base(itemID) => Movable = false;

        public BaseWall(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
