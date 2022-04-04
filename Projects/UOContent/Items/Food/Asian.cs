namespace Server.Items
{
    public class Wasabi : Item
    {
        [Constructible]
        public Wasabi() : base(0x24E8) => Weight = 1.0;

        public Wasabi(Serial serial) : base(serial)
        {
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

    public class WasabiClumps : Food
    {
        [Constructible]
        public WasabiClumps() : base(0x24EB)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 2;
        }

        public WasabiClumps(Serial serial) : base(serial)
        {
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

    public class EmptyBentoBox : Item
    {
        [Constructible]
        public EmptyBentoBox() : base(0x2834) => Weight = 5.0;

        public EmptyBentoBox(Serial serial) : base(serial)
        {
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

    public class BentoBox : Food
    {
        [Constructible]
        public BentoBox() : base(0x2836)
        {
            Stackable = false;
            Weight = 5.0;
            FillFactor = 2;
        }

        public BentoBox(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new EmptyBentoBox());
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

    public class SushiRolls : Food
    {
        [Constructible]
        public SushiRolls() : base(0x283E)
        {
            Stackable = false;
            Weight = 3.0;
            FillFactor = 2;
        }

        public SushiRolls(Serial serial) : base(serial)
        {
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

    public class SushiPlatter : Food
    {
        [Constructible]
        public SushiPlatter() : base(0x2840)
        {
            Stackable = Core.ML;
            Weight = 3.0;
            FillFactor = 2;
        }

        public SushiPlatter(Serial serial) : base(serial)
        {
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

    public class GreenTeaBasket : Item
    {
        [Constructible]
        public GreenTeaBasket() : base(0x284B) => Weight = 10.0;

        public GreenTeaBasket(Serial serial) : base(serial)
        {
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

    public class GreenTea : Food
    {
        [Constructible]
        public GreenTea() : base(0x284C)
        {
            Stackable = false;
            Weight = 4.0;
            FillFactor = 2;
        }

        public GreenTea(Serial serial) : base(serial)
        {
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

    public class MisoSoup : Food
    {
        [Constructible]
        public MisoSoup() : base(0x284D)
        {
            Stackable = false;
            Weight = 4.0;
            FillFactor = 2;
        }

        public MisoSoup(Serial serial) : base(serial)
        {
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

    public class WhiteMisoSoup : Food
    {
        [Constructible]
        public WhiteMisoSoup() : base(0x284E)
        {
            Stackable = false;
            Weight = 4.0;
            FillFactor = 2;
        }

        public WhiteMisoSoup(Serial serial) : base(serial)
        {
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

    public class RedMisoSoup : Food
    {
        [Constructible]
        public RedMisoSoup() : base(0x284F)
        {
            Stackable = false;
            Weight = 4.0;
            FillFactor = 2;
        }

        public RedMisoSoup(Serial serial) : base(serial)
        {
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

    public class AwaseMisoSoup : Food
    {
        [Constructible]
        public AwaseMisoSoup() : base(0x2850)
        {
            Stackable = false;
            Weight = 4.0;
            FillFactor = 2;
        }

        public AwaseMisoSoup(Serial serial) : base(serial)
        {
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
