namespace Server.Multis
{
    public class SmallBoat : BaseBoat
    {
        [Constructible]
        public SmallBoat()
        {
        }

        public SmallBoat(Serial serial) : base(serial)
        {
        }

        public override int NorthID => 0x0;
        public override int EastID => 0x1;
        public override int SouthID => 0x2;
        public override int WestID => 0x3;

        public override int HoldDistance => 4;
        public override int TillerManDistance => -4;

        public override Point2D StarboardOffset => new(2, 0);
        public override Point2D PortOffset => new(-2, 0);

        public override Point3D MarkOffset => new(0, 1, 3);

        public override BaseDockedBoat DockedBoat => new SmallDockedBoat(this);

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

    public class SmallBoatDeed : BaseBoatDeed
    {
        [Constructible]
        public SmallBoatDeed() : base(0x0, Point3D.Zero)
        {
        }

        public SmallBoatDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041205; // small ship deed
        public override BaseBoat Boat => new SmallBoat();

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

    public class SmallDockedBoat : BaseDockedBoat
    {
        public SmallDockedBoat(BaseBoat boat) : base(0x0, Point3D.Zero, boat)
        {
        }

        public SmallDockedBoat(Serial serial) : base(serial)
        {
        }

        public override BaseBoat Boat => new SmallBoat();

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
