namespace Server.Items
{
    [Furniture]
    public class GiantReplicaAcorn : Item
    {
        [Constructible]
        public GiantReplicaAcorn() : base(0x2D4A) => Weight = 1.0;

        public GiantReplicaAcorn(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072889; // giant replica acorn

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
