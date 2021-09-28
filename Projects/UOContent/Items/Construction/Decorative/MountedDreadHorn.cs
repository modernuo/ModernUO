namespace Server.Items
{
    [Flippable(0x3158, 0x3159)]
<<<<<<< HEAD
    public class MountedDreadHorn : Item
=======
    [Serializable(0)]
    public partial class MountedDreadHorn : Item
>>>>>>> 990d151ef302b70bb21d4b3e94b8df73ad7c9ef8
    {
        [Constructible]
        public MountedDreadHorn() : base(0x3158) => Weight = 1.0;

<<<<<<< HEAD
        public MountedDreadHorn(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074464; // mounted Dread Horn
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
        public override int LabelNumber => 1074464; // mounted Dread Horn
>>>>>>> 990d151ef302b70bb21d4b3e94b8df73ad7c9ef8
    }
}
