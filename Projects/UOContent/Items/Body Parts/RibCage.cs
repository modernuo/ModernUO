namespace Server.Items
{
    [Flippable(0x1B17, 0x1B18)]
    public class RibCage : Item, IScissorable
    {
        [Constructible]
        public RibCage() : base(0x1B17 + Utility.Random(2))
        {
            Stackable = false;
            Weight = 5.0;
        }

        public RibCage(Serial serial) : base(serial)
        {
        }

        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (Deleted || !from.CanSee(this))
            {
                return false;
            }

            ScissorHelper(from, new Bone(), Utility.RandomMinMax(3, 5));

            return true;
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
