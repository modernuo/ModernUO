namespace Server.Items
{
    public class BombardScroll : SpellScroll
    {
        [Constructible]
        public BombardScroll(int amount = 1)
            : base(688, 0x2DA9, amount)
        {
        }

        public BombardScroll(Serial serial)
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
