namespace Server.Items
{
    public class EmptyWoodenBowl : Item
    {
        [Constructible]
        public EmptyWoodenBowl() : base(0x15F8) => Weight = 1.0;

        public EmptyWoodenBowl(Serial serial) : base(serial)
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

    public class EmptyPewterBowl : Item
    {
        [Constructible]
        public EmptyPewterBowl() : base(0x15FD) => Weight = 1.0;

        public EmptyPewterBowl(Serial serial) : base(serial)
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

    public class WoodenBowlOfCarrots : Food
    {
        [Constructible]
        public WoodenBowlOfCarrots() : base(0x15F9)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 2;
        }

        public WoodenBowlOfCarrots(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new EmptyWoodenBowl());
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

    public class WoodenBowlOfCorn : Food
    {
        [Constructible]
        public WoodenBowlOfCorn() : base(0x15FA)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 2;
        }

        public WoodenBowlOfCorn(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new EmptyWoodenBowl());
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

    public class WoodenBowlOfLettuce : Food
    {
        [Constructible]
        public WoodenBowlOfLettuce() : base(0x15FB)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 2;
        }

        public WoodenBowlOfLettuce(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new EmptyWoodenBowl());
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

    public class WoodenBowlOfPeas : Food
    {
        [Constructible]
        public WoodenBowlOfPeas() : base(0x15FC)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 2;
        }

        public WoodenBowlOfPeas(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new EmptyWoodenBowl());
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

    public class PewterBowlOfCarrots : Food
    {
        [Constructible]
        public PewterBowlOfCarrots() : base(0x15FE)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 2;
        }

        public PewterBowlOfCarrots(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new EmptyPewterBowl());
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

    public class PewterBowlOfCorn : Food
    {
        [Constructible]
        public PewterBowlOfCorn() : base(0x15FF)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 2;
        }

        public PewterBowlOfCorn(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new EmptyPewterBowl());
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

    public class PewterBowlOfLettuce : Food
    {
        [Constructible]
        public PewterBowlOfLettuce() : base(0x1600)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 2;
        }

        public PewterBowlOfLettuce(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new EmptyPewterBowl());
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

    public class PewterBowlOfPeas : Food
    {
        [Constructible]
        public PewterBowlOfPeas() : base(0x1601)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 2;
        }

        public PewterBowlOfPeas(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new EmptyPewterBowl());
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

    public class PewterBowlOfPotatos : Food
    {
        [Constructible]
        public PewterBowlOfPotatos() : base(0x1602)
        {
            Stackable = false;
            Weight = 1.0;
            FillFactor = 2;
        }

        public PewterBowlOfPotatos(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new EmptyPewterBowl());
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

    [TypeAlias("Server.Items.EmptyLargeWoodenBowl")]
    public class EmptyWoodenTub : Item
    {
        [Constructible]
        public EmptyWoodenTub() : base(0x1605) => Weight = 2.0;

        public EmptyWoodenTub(Serial serial) : base(serial)
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

    [TypeAlias("Server.Items.EmptyLargePewterBowl")]
    public class EmptyPewterTub : Item
    {
        [Constructible]
        public EmptyPewterTub() : base(0x1603) => Weight = 2.0;

        public EmptyPewterTub(Serial serial) : base(serial)
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

    public class WoodenBowlOfStew : Food
    {
        [Constructible]
        public WoodenBowlOfStew() : base(0x1604)
        {
            Stackable = false;
            Weight = 2.0;
            FillFactor = 2;
        }

        public WoodenBowlOfStew(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new EmptyWoodenTub());
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

    public class WoodenBowlOfTomatoSoup : Food
    {
        [Constructible]
        public WoodenBowlOfTomatoSoup() : base(0x1606)
        {
            Stackable = false;
            Weight = 2.0;
            FillFactor = 2;
        }

        public WoodenBowlOfTomatoSoup(Serial serial) : base(serial)
        {
        }

        public override bool Eat(Mobile from)
        {
            if (!base.Eat(from))
            {
                return false;
            }

            from.AddToBackpack(new EmptyWoodenTub());
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
}
