namespace Server.Items
{
    public abstract class BaseOuterLegs : BaseClothing
    {
        public BaseOuterLegs(int itemID, int hue = 0) : base(itemID, Layer.OuterLegs, hue)
        {
        }

        public BaseOuterLegs(Serial serial) : base(serial)
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

    [Flippable(0x230C, 0x230B)]
    public class FurSarong : BaseOuterLegs
    {
        [Constructible]
        public FurSarong(int hue = 0) : base(0x230C, hue) => Weight = 3.0;

        public FurSarong(Serial serial) : base(serial)
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

            if (Weight == 4.0)
            {
                Weight = 3.0;
            }
        }
    }

    [Flippable(0x1516, 0x1531)]
    public class Skirt : BaseOuterLegs
    {
        [Constructible]
        public Skirt(int hue = 0) : base(0x1516, hue) => Weight = 4.0;

        public Skirt(Serial serial) : base(serial)
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

    [Flippable(0x1537, 0x1538)]
    public class Kilt : BaseOuterLegs
    {
        [Constructible]
        public Kilt(int hue = 0) : base(0x1537, hue) => Weight = 2.0;

        public Kilt(Serial serial) : base(serial)
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

    [Flippable(0x279A, 0x27E5)]
    public class Hakama : BaseOuterLegs
    {
        [Constructible]
        public Hakama(int hue = 0) : base(0x279A, hue) => Weight = 2.0;

        public Hakama(Serial serial) : base(serial)
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
