namespace Server.Items
{
    public class FruitBasket : Food
    {
        [Constructible]
        public FruitBasket() : base(0x993)
        {
            Weight = 2.0;
            FillFactor = 5;
            Stackable = false;
        }

        public FruitBasket(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new Basket());
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

    [Flippable(0x171f, 0x1720)]
    public class Banana : Food
    {
        [Constructible]
        public Banana(int amount = 1) : base(0x171f, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Banana(Serial serial) : base(serial)
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

    [Flippable(0x1721, 0x1722)]
    public class Bananas : Food
    {
        [Constructible]
        public Bananas(int amount = 1) : base(0x1721, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Bananas(Serial serial) : base(serial)
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

    public class SplitCoconut : Food
    {
        [Constructible]
        public SplitCoconut(int amount = 1) : base(0x1725, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public SplitCoconut(Serial serial) : base(serial)
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

    public class Lemon : Food
    {
        [Constructible]
        public Lemon(int amount = 1) : base(0x1728, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Lemon(Serial serial) : base(serial)
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

    public class Lemons : Food
    {
        [Constructible]
        public Lemons(int amount = 1) : base(0x1729, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Lemons(Serial serial) : base(serial)
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

    public class Lime : Food
    {
        [Constructible]
        public Lime(int amount = 1) : base(0x172a, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Lime(Serial serial) : base(serial)
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

    public class Limes : Food
    {
        [Constructible]
        public Limes(int amount = 1) : base(0x172B, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Limes(Serial serial) : base(serial)
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

    public class Coconut : Food
    {
        [Constructible]
        public Coconut(int amount = 1) : base(0x1726, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Coconut(Serial serial) : base(serial)
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

    public class OpenCoconut : Food
    {
        [Constructible]
        public OpenCoconut(int amount = 1) : base(0x1723, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public OpenCoconut(Serial serial) : base(serial)
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

    public class Dates : Food
    {
        [Constructible]
        public Dates(int amount = 1) : base(0x1727, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Dates(Serial serial) : base(serial)
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

    public class Grapes : Food
    {
        [Constructible]
        public Grapes(int amount = 1) : base(0x9D1, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Grapes(Serial serial) : base(serial)
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

    public class Peach : Food
    {
        [Constructible]
        public Peach(int amount = 1) : base(0x9D2, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Peach(Serial serial) : base(serial)
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

    public class Pear : Food
    {
        [Constructible]
        public Pear(int amount = 1) : base(0x994, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Pear(Serial serial) : base(serial)
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

    public class Apple : Food
    {
        [Constructible]
        public Apple(int amount = 1) : base(0x9D0, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Apple(Serial serial) : base(serial)
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

    public class Watermelon : Food
    {
        [Constructible]
        public Watermelon(int amount = 1) : base(0xC5C, amount)
        {
            Weight = 5.0;
            FillFactor = 5;
        }

        public Watermelon(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1)
            {
                if (FillFactor == 2)
                {
                    FillFactor = 5;
                }

                if (Weight == 2.0)
                {
                    Weight = 5.0;
                }
            }
        }
    }

    public class SmallWatermelon : Food
    {
        [Constructible]
        public SmallWatermelon(int amount = 1) : base(0xC5D, amount)
        {
            Weight = 5.0;
            FillFactor = 5;
        }

        public SmallWatermelon(Serial serial) : base(serial)
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

    [Flippable(0xc72, 0xc73)]
    public class Squash : Food
    {
        [Constructible]
        public Squash(int amount = 1) : base(0xc72, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Squash(Serial serial) : base(serial)
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

    [Flippable(0xc79, 0xc7a)]
    public class Cantaloupe : Food
    {
        [Constructible]
        public Cantaloupe(int amount = 1) : base(0xc79, amount)
        {
            Weight = 1.0;
            FillFactor = 1;
        }

        public Cantaloupe(Serial serial) : base(serial)
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
