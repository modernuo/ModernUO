namespace Server.Multis
{
    public class LargeBoat : BaseBoat
    {
        [Constructible]
        public LargeBoat()
        {
        }

        public LargeBoat(Serial serial) : base(serial)
        {
        }

        public override int NorthID => 0x10;
        public override int EastID => 0x11;
        public override int SouthID => 0x12;
        public override int WestID => 0x13;

        public override int HoldDistance => 5;
        public override int TillerManDistance => -5;

        public override Point2D StarboardOffset => new(2, -1);
        public override Point2D PortOffset => new(-2, -1);

        public override Point3D MarkOffset => new(0, 0, 3);

        public override BaseDockedBoat DockedBoat => new LargeDockedBoat(this);

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }
    }

    public class LargeBoatDeed : BaseBoatDeed
    {
        [Constructible]
        public LargeBoatDeed() : base(0x10, new Point3D(0, -1, 0))
        {
        }

        public LargeBoatDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041209; // large ship deed
        public override BaseBoat Boat => new LargeBoat();

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }
    }

    public class LargeDockedBoat : BaseDockedBoat
    {
        public LargeDockedBoat(BaseBoat boat) : base(0x10, new Point3D(0, -1, 0), boat)
        {
        }

        public LargeDockedBoat(Serial serial) : base(serial)
        {
        }

        public override BaseBoat Boat => new LargeBoat();

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }
    }
}
