namespace Server.Items
{
    [Flippable(0x3158, 0x3159)]
    public class MountedDreadHorn : Item
    {
        [Constructible]
        public MountedDreadHorn() : base(0x3158) => Weight = 1.0;

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
    }
}
