namespace Server.Items
{
    public abstract class BaseReagent : Item
    {
        public BaseReagent(int itemID, int amount = 1) : base(itemID)
        {
            Stackable = true;
            Amount = amount;
        }

        public BaseReagent(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 0.1;

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
