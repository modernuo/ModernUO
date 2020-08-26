namespace Server.Items
{
    public class BarrelLid : Item
    {
        [Constructible]
        public BarrelLid() : base(0x1DB8) => Weight = 2;

        public BarrelLid(Serial serial) : base(serial)
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

    [Flippable(0x1EB1, 0x1EB2, 0x1EB3, 0x1EB4)]
    public class BarrelStaves : Item
    {
        [Constructible]
        public BarrelStaves() : base(0x1EB1) => Weight = 1;

        public BarrelStaves(Serial serial) : base(serial)
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

    public class BarrelHoops : Item
    {
        [Constructible]
        public BarrelHoops() : base(0x1DB7) => Weight = 5;

        public BarrelHoops(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1011228; // Barrel hoops

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

    public class BarrelTap : Item
    {
        [Constructible]
        public BarrelTap() : base(0x1004) => Weight = 1;

        public BarrelTap(Serial serial) : base(serial)
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
}
