namespace Server.Items
{
    public abstract class BaseMiddleTorso : BaseClothing
    {
        public BaseMiddleTorso(int itemID, int hue = 0) : base(itemID, Layer.MiddleTorso, hue)
        {
        }

        public BaseMiddleTorso(Serial serial) : base(serial)
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

    [Flippable(0x1541, 0x1542)]
    public class BodySash : BaseMiddleTorso
    {
        [Constructible]
        public BodySash(int hue = 0) : base(0x1541, hue) => Weight = 1.0;

        public BodySash(Serial serial) : base(serial)
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

    [Flippable(0x153d, 0x153e)]
    public class FullApron : BaseMiddleTorso
    {
        [Constructible]
        public FullApron(int hue = 0) : base(0x153d, hue) => Weight = 4.0;

        public FullApron(Serial serial) : base(serial)
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

    [Flippable(0x1f7b, 0x1f7c)]
    public class Doublet : BaseMiddleTorso
    {
        [Constructible]
        public Doublet(int hue = 0) : base(0x1F7B, hue) => Weight = 2.0;

        public Doublet(Serial serial) : base(serial)
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

    [Flippable(0x1ffd, 0x1ffe)]
    public class Surcoat : BaseMiddleTorso
    {
        [Constructible]
        public Surcoat(int hue = 0) : base(0x1FFD, hue) => Weight = 6.0;

        public Surcoat(Serial serial) : base(serial)
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

            if (Weight == 3.0)
            {
                Weight = 6.0;
            }
        }
    }

    [Flippable(0x1fa1, 0x1fa2)]
    public class Tunic : BaseMiddleTorso
    {
        [Constructible]
        public Tunic(int hue = 0) : base(0x1FA1, hue) => Weight = 5.0;

        public Tunic(Serial serial) : base(serial)
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

    [Flippable(0x2310, 0x230F)]
    public class FormalShirt : BaseMiddleTorso
    {
        [Constructible]
        public FormalShirt(int hue = 0) : base(0x2310, hue) => Weight = 1.0;

        public FormalShirt(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            if (Weight == 2.0)
            {
                Weight = 1.0;
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    [Flippable(0x1f9f, 0x1fa0)]
    public class JesterSuit : BaseMiddleTorso
    {
        [Constructible]
        public JesterSuit(int hue = 0) : base(0x1F9F, hue) => Weight = 4.0;

        public JesterSuit(Serial serial) : base(serial)
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

    [Flippable(0x27A1, 0x27EC)]
    public class JinBaori : BaseMiddleTorso
    {
        [Constructible]
        public JinBaori(int hue = 0) : base(0x27A1, hue) => Weight = 3.0;

        public JinBaori(Serial serial) : base(serial)
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
