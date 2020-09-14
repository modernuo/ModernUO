namespace Server.Items
{
    public class Rope : Item
    {
        [Constructible]
        public Rope(int amount = 1) : base(0x14F8)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public Rope(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class IronWire : Item
    {
        [Constructible]
        public IronWire(int amount = 1) : base(0x1876)
        {
            Stackable = true;
            Weight = 5.0;
            Amount = amount;
        }

        public IronWire(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1 && Weight == 2.0)
            {
                Weight = 5.0;
            }
        }
    }

    public class SilverWire : Item
    {
        [Constructible]
        public SilverWire(int amount = 1) : base(0x1877)
        {
            Stackable = true;
            Weight = 5.0;
            Amount = amount;
        }

        public SilverWire(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1 && Weight == 2.0)
            {
                Weight = 5.0;
            }
        }
    }

    public class GoldWire : Item
    {
        [Constructible]
        public GoldWire(int amount = 1) : base(0x1878)
        {
            Stackable = true;
            Weight = 5.0;
            Amount = amount;
        }

        public GoldWire(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1 && Weight == 2.0)
            {
                Weight = 5.0;
            }
        }
    }

    public class CopperWire : Item
    {
        [Constructible]
        public CopperWire(int amount = 1) : base(0x1879)
        {
            Stackable = true;
            Weight = 5.0;
            Amount = amount;
        }

        public CopperWire(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1 && Weight == 2.0)
            {
                Weight = 5.0;
            }
        }
    }

    public class WhiteDriedFlowers : Item
    {
        [Constructible]
        public WhiteDriedFlowers(int amount = 1) : base(0xC3C)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public WhiteDriedFlowers(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class GreenDriedFlowers : Item
    {
        [Constructible]
        public GreenDriedFlowers(int amount = 1) : base(0xC3E)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public GreenDriedFlowers(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class DriedOnions : Item
    {
        [Constructible]
        public DriedOnions(int amount = 1) : base(0xC40)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public DriedOnions(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class DriedHerbs : Item
    {
        [Constructible]
        public DriedHerbs(int amount = 1) : base(0xC42)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public DriedHerbs(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class HorseShoes : Item
    {
        [Constructible]
        public HorseShoes() : base(0xFB6) => Weight = 3.0;

        public HorseShoes(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class ForgedMetal : Item
    {
        [Constructible]
        public ForgedMetal() : base(0xFB8) => Weight = 5.0;

        public ForgedMetal(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class Whip : Item
    {
        [Constructible]
        public Whip() : base(0x166E) => Weight = 1.0;

        public Whip(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class PaintsAndBrush : Item
    {
        [Constructible]
        public PaintsAndBrush() : base(0xFC1) => Weight = 1.0;

        public PaintsAndBrush(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class PenAndInk : Item
    {
        [Constructible]
        public PenAndInk() : base(0xFBF) => Weight = 1.0;

        public PenAndInk(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class ChiselsNorth : Item
    {
        [Constructible]
        public ChiselsNorth() : base(0x1026) => Weight = 1.0;

        public ChiselsNorth(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class ChiselsWest : Item
    {
        [Constructible]
        public ChiselsWest() : base(0x1027) => Weight = 1.0;

        public ChiselsWest(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class DirtyPan : Item
    {
        [Constructible]
        public DirtyPan() : base(0x9E8) => Weight = 1.0;

        public DirtyPan(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class DirtySmallRoundPot : Item
    {
        [Constructible]
        public DirtySmallRoundPot() : base(0x9E7) => Weight = 1.0;

        public DirtySmallRoundPot(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class DirtyPot : Item
    {
        [Constructible]
        public DirtyPot() : base(0x9E6) => Weight = 1.0;

        public DirtyPot(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class DirtyRoundPot : Item
    {
        [Constructible]
        public DirtyRoundPot() : base(0x9DF) => Weight = 1.0;

        public DirtyRoundPot(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class DirtyFrypan : Item
    {
        [Constructible]
        public DirtyFrypan() : base(0x9DE) => Weight = 1.0;

        public DirtyFrypan(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class DirtySmallPot : Item
    {
        [Constructible]
        public DirtySmallPot() : base(0x9DD) => Weight = 1.0;

        public DirtySmallPot(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class DirtyKettle : Item
    {
        [Constructible]
        public DirtyKettle() : base(0x9DC) => Weight = 1.0;

        public DirtyKettle(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
