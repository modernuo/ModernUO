namespace Server.Items
{
    [Flippable(0x156C, 0x156D)]
    public class DecorativeShield1 : Item
    {
        [Constructible]
        public DecorativeShield1() : base(0x156C) => Movable = false;

        public DecorativeShield1(Serial serial) : base(serial)
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

    [Flippable(0x156E, 0x156F)]
    public class DecorativeShield2 : Item
    {
        [Constructible]
        public DecorativeShield2() : base(0x156E) => Movable = false;

        public DecorativeShield2(Serial serial) : base(serial)
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

    [Flippable(0x1570, 0x1571)]
    public class DecorativeShield3 : Item
    {
        [Constructible]
        public DecorativeShield3() : base(0x1570) => Movable = false;

        public DecorativeShield3(Serial serial) : base(serial)
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

    [Flippable(0x1572, 0x1573)]
    public class DecorativeShield4 : Item
    {
        [Constructible]
        public DecorativeShield4() : base(0x1572) => Movable = false;

        public DecorativeShield4(Serial serial) : base(serial)
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

    [Flippable(0x1574, 0x1575)]
    public class DecorativeShield5 : Item
    {
        [Constructible]
        public DecorativeShield5() : base(0x1574) => Movable = false;

        public DecorativeShield5(Serial serial) : base(serial)
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

    [Flippable(0x1576, 0x1577)]
    public class DecorativeShield6 : Item
    {
        [Constructible]
        public DecorativeShield6() : base(0x1576) => Movable = false;

        public DecorativeShield6(Serial serial) : base(serial)
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

    [Flippable(0x1578, 0x1579)]
    public class DecorativeShield7 : Item
    {
        [Constructible]
        public DecorativeShield7() : base(0x1578) => Movable = false;

        public DecorativeShield7(Serial serial) : base(serial)
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

    [Flippable(0x157A, 0x157B)]
    public class DecorativeShield8 : Item
    {
        [Constructible]
        public DecorativeShield8() : base(0x157A) => Movable = false;

        public DecorativeShield8(Serial serial) : base(serial)
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

    [Flippable(0x157C, 0x157D)]
    public class DecorativeShield9 : Item
    {
        [Constructible]
        public DecorativeShield9() : base(0x157C) => Movable = false;

        public DecorativeShield9(Serial serial) : base(serial)
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

    [Flippable(0x157E, 0x157F)]
    public class DecorativeShield10 : Item
    {
        [Constructible]
        public DecorativeShield10() : base(0x157E) => Movable = false;

        public DecorativeShield10(Serial serial) : base(serial)
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

    [Flippable(0x1580, 0x1581)]
    public class DecorativeShield11 : Item
    {
        [Constructible]
        public DecorativeShield11() : base(0x1580) => Movable = false;

        public DecorativeShield11(Serial serial) : base(serial)
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

    [Flippable(0x1582, 0x1583, 0x1634, 0x1635)]
    public class DecorativeShieldSword1North : Item
    {
        [Constructible]
        public DecorativeShieldSword1North() : base(Utility.Random(0x1582, 2)) => Movable = false;

        public DecorativeShieldSword1North(Serial serial) : base(serial)
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

    [Flippable(0x1634, 0x1635, 0x1582, 0x1583)]
    public class DecorativeShieldSword1West : Item
    {
        [Constructible]
        public DecorativeShieldSword1West() : base(Utility.Random(0x1634, 2)) => Movable = false;

        public DecorativeShieldSword1West(Serial serial) : base(serial)
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

    [Flippable(0x1584, 0x1585, 0x1636, 0x1637)]
    public class DecorativeShieldSword2North : Item
    {
        [Constructible]
        public DecorativeShieldSword2North() : base(Utility.Random(0x1584, 2)) => Movable = false;

        public DecorativeShieldSword2North(Serial serial) : base(serial)
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

    [Flippable(0x1636, 0x1637, 0x1584, 0x1585)]
    public class DecorativeShieldSword2West : Item
    {
        [Constructible]
        public DecorativeShieldSword2West() : base(Utility.Random(0x1636, 2)) => Movable = false;

        public DecorativeShieldSword2West(Serial serial) : base(serial)
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
