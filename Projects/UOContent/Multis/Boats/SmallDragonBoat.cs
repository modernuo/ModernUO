namespace Server.Multis
{
    public class SmallDragonBoat : BaseBoat
    {
        [Constructible]
        public SmallDragonBoat()
        {
        }

        public SmallDragonBoat(Serial serial) : base(serial)
        {
        }

        public override int NorthID => 0x4;
        public override int EastID => 0x5;
        public override int SouthID => 0x6;
        public override int WestID => 0x7;

        public override int HoldDistance => 4;
        public override int TillerManDistance => -4;

        public override Point2D StarboardOffset => new(2, 0);
        public override Point2D PortOffset => new(-2, 0);

        public override Point3D MarkOffset => new(0, 1, 3);

        public override BaseDockedBoat DockedBoat => new SmallDockedDragonBoat(this);

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

    public class SmallDragonBoatDeed : BaseBoatDeed
    {
        [Constructible]
        public SmallDragonBoatDeed() : base(0x4, Point3D.Zero)
        {
        }

        public SmallDragonBoatDeed(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1041206; // small dragon ship deed
        public override BaseBoat Boat => new SmallDragonBoat();

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

    public class SmallDockedDragonBoat : BaseDockedBoat
    {
        public SmallDockedDragonBoat(BaseBoat boat) : base(0x4, Point3D.Zero, boat)
        {
        }

        public SmallDockedDragonBoat(Serial serial) : base(serial)
        {
        }

        public override BaseBoat Boat => new SmallDragonBoat();

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
