namespace Server.Items
{
    public abstract class BaseSign : Item
    {
        public BaseSign(int dispID) : base(dispID) => Movable = false;

        public BaseSign(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
