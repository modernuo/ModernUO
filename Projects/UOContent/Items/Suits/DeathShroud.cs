namespace Server.Items
{
    public class DeathShroud : BaseSuit
    {
        [Constructible]
        public DeathShroud() : base(AccessLevel.GameMaster, 0x0, 0x204E)
        {
        }

        public DeathShroud(Serial serial) : base(serial)
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
