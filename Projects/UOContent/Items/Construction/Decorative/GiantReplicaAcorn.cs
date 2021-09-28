namespace Server.Items
{
    [Furniture]
<<<<<<< HEAD
    public class GiantReplicaAcorn : Item
=======
    [Serializable(0)]
    public partial class GiantReplicaAcorn : Item
>>>>>>> 990d151ef302b70bb21d4b3e94b8df73ad7c9ef8
    {
        [Constructible]
        public GiantReplicaAcorn() : base(0x2D4A) => Weight = 1.0;

<<<<<<< HEAD
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
=======
        public override int LabelNumber => 1072889; // giant replica acorn
>>>>>>> 990d151ef302b70bb21d4b3e94b8df73ad7c9ef8
    }
}
