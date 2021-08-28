namespace Server.Items
{
    public abstract class BaseShirt : BaseClothing
    {
        public BaseShirt(int itemID, int hue = 0) : base(itemID, Layer.Shirt, hue)
        {
        }

        public BaseShirt(Serial serial) : base(serial)
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

    [Flippable(0x1efd, 0x1efe)]
    public class FancyShirt : BaseShirt
    {
        [Constructible]
        public FancyShirt(int hue = 0) : base(0x1EFD, hue) => Weight = 2.0;

        public FancyShirt(Serial serial) : base(serial)
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

    [Flippable(0x1517, 0x1518)]
    public class Shirt : BaseShirt
    {
        [Constructible]
        public Shirt(int hue = 0) : base(0x1517, hue) => Weight = 1.0;

        public Shirt(Serial serial) : base(serial)
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

            if (Weight == 2.0)
            {
                Weight = 1.0;
            }
        }
    }

    [Flippable(0x2794, 0x27DF)]
    public class ClothNinjaJacket : BaseShirt
    {
        [Constructible]
        public ClothNinjaJacket(int hue = 0) : base(0x2794, hue)
        {
            Weight = 5.0;
            Layer = Layer.InnerTorso;
        }

        public ClothNinjaJacket(Serial serial) : base(serial)
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

    public class ElvenShirt : BaseShirt
    {
        [Constructible]
        public ElvenShirt(int hue = 0) : base(0x3175, hue) => Weight = 2.0;

        public ElvenShirt(Serial serial)
            : base(serial)
        {
        }

        public override Race RequiredRace => Race.Elf;

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

    public class ElvenDarkShirt : BaseShirt
    {
        [Constructible]
        public ElvenDarkShirt(int hue = 0) : base(0x3176, hue) => Weight = 2.0;

        public ElvenDarkShirt(Serial serial) : base(serial)
        {
        }

        public override Race RequiredRace => Race.Elf;

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
