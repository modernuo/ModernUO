using System;
using Server;
using Server.Items;

namespace Server.Multis
{
    public class PirateShip_Boat : BaseBoat
    {
        public override int NorthID { get { return 0x4014; } }
        public override int EastID { get { return 0x4015; } }
        public override int SouthID { get { return 0x4016; } }
        public override int WestID { get { return 0x4017; } }

        public override int HoldDistance { get { return 5; } }
        public override int TillerManDistance { get { return -5; } }

        public override Point2D StarboardOffset { get { return new Point2D(2, -1); } }
        public override Point2D PortOffset { get { return new Point2D(-2, -1); } }

        public override Point3D MarkOffset { get { return new Point3D(0, 0, 3); } }

        public override BaseDockedBoat DockedBoat { get { return new LargeDockedDragonBoat(this); } }

        [Constructible]
        public PirateShip_Boat()
        {
            Name = "A Pirate Ship";
            
        }

        public PirateShip_Boat(Serial serial): base(serial)
        {
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }
    }

    public class PirateShip_Boat_Deed : BaseBoatDeed
    {
        public override int LabelNumber { get { return 1041210; } }
        public override BaseBoat Boat { get { return new PirateShip_Boat(); } }

        [Constructible]
        public PirateShip_Boat_Deed(): base(0x4014, new Point3D(0, -1, 0))
        {
            Name = "A Pirate Ship";
            Hue = 1157;
        }

        public PirateShip_Boat_Deed(Serial serial): base(serial)
        {
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }
    }

    public class PirateShip_Boat_Docked : BaseDockedBoat
    {
        public override BaseBoat Boat { get { return new LargeDragonBoat(); } }

        public PirateShip_Boat_Docked(BaseBoat boat): base(0x4014, new Point3D(0, -1, 0), boat)
        {
        }

        public PirateShip_Boat_Docked(Serial serial): base(serial)
        {
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }
    }
}
