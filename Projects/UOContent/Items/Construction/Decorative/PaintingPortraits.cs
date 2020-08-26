namespace Server.Items
{
    public class LargePainting : Item
    {
        [Constructible]
        public LargePainting() : base(0x0EA0) => Movable = false;

        public LargePainting(Serial serial) : base(serial)
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

    [Flippable(0x0E9F, 0x0EC8)]
    public class WomanPortrait1 : Item
    {
        [Constructible]
        public WomanPortrait1() : base(0x0E9F) => Movable = false;

        public WomanPortrait1(Serial serial) : base(serial)
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

    [Flippable(0x0EE7, 0x0EC9)]
    public class WomanPortrait2 : Item
    {
        [Constructible]
        public WomanPortrait2() : base(0x0EE7) => Movable = false;

        public WomanPortrait2(Serial serial) : base(serial)
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

    [Flippable(0x0EA2, 0x0EA1)]
    public class ManPortrait1 : Item
    {
        [Constructible]
        public ManPortrait1() : base(0x0EA2) => Movable = false;

        public ManPortrait1(Serial serial) : base(serial)
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

    [Flippable(0x0EA3, 0x0EA4)]
    public class ManPortrait2 : Item
    {
        [Constructible]
        public ManPortrait2() : base(0x0EA3) => Movable = false;

        public ManPortrait2(Serial serial) : base(serial)
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

    [Flippable(0x0EA6, 0x0EA5)]
    public class LadyPortrait1 : Item
    {
        [Constructible]
        public LadyPortrait1() : base(0x0EA6) => Movable = false;

        public LadyPortrait1(Serial serial) : base(serial)
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

    [Flippable(0x0EA7, 0x0EA8)]
    public class LadyPortrait2 : Item
    {
        [Constructible]
        public LadyPortrait2() : base(0x0EA7) => Movable = false;

        public LadyPortrait2(Serial serial) : base(serial)
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
