namespace Server.Items
{
    public class NetherBoltScroll : SpellScroll
    {
        [Constructible]
        public NetherBoltScroll(int amount = 1)
            : base(677, 0x2D9E, amount)
        {
        }

        public NetherBoltScroll(Serial serial)
            : base(serial)
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

            /*int version = */
            reader.ReadInt();
        }
    }
}
