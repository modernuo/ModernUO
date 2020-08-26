namespace Server.Items
{
    public abstract class BaseWaist : BaseClothing
    {
        public BaseWaist(int itemID, int hue = 0) : base(itemID, Layer.Waist, hue)
        {
        }

        public BaseWaist(Serial serial) : base(serial)
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

    [Flippable(0x153b, 0x153c)]
    public class HalfApron : BaseWaist
    {
        [Constructible]
        public HalfApron(int hue = 0) : base(0x153b, hue) => Weight = 2.0;

        public HalfApron(Serial serial) : base(serial)
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

    [Flippable(0x27A0, 0x27EB)]
    public class Obi : BaseWaist
    {
        [Constructible]
        public Obi(int hue = 0) : base(0x27A0, hue) => Weight = 1.0;

        public Obi(Serial serial) : base(serial)
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

    [Flippable(0x2B68, 0x315F)]
    public class WoodlandBelt : BaseWaist
    {
        [Constructible]
        public WoodlandBelt(int hue = 0) : base(0x2B68, hue) => Weight = 4.0;

        public WoodlandBelt(Serial serial) : base(serial)
        {
        }

        public override Race RequiredRace => Race.Elf;

        public override bool Dye(Mobile from, DyeTub sender)
        {
            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }

        public override bool Scissor(Mobile from, Scissors scissors)
        {
            from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
            return false;
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
