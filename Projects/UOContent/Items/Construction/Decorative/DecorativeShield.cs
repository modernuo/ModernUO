using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x156C, 0x156D)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShield1 : Item
    {
        [Constructible]
        public DecorativeShield1() : base(0x156C) => Movable = false;
    }

    [Flippable(0x156E, 0x156F)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShield2 : Item
    {
        [Constructible]
        public DecorativeShield2() : base(0x156E) => Movable = false;
    }

    [Flippable(0x1570, 0x1571)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShield3 : Item
    {
        [Constructible]
        public DecorativeShield3() : base(0x1570) => Movable = false;
    }

    [Flippable(0x1572, 0x1573)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShield4 : Item
    {
        [Constructible]
        public DecorativeShield4() : base(0x1572) => Movable = false;
    }

    [Flippable(0x1574, 0x1575)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShield5 : Item
    {
        [Constructible]
        public DecorativeShield5() : base(0x1574) => Movable = false;
    }

    [Flippable(0x1576, 0x1577)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShield6 : Item
    {
        [Constructible]
        public DecorativeShield6() : base(0x1576) => Movable = false;
    }

    [Flippable(0x1578, 0x1579)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShield7 : Item
    {
        [Constructible]
        public DecorativeShield7() : base(0x1578) => Movable = false;
    }

    [Flippable(0x157A, 0x157B)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShield8 : Item
    {
        [Constructible]
        public DecorativeShield8() : base(0x157A) => Movable = false;
    }

    [Flippable(0x157C, 0x157D)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShield9 : Item
    {
        [Constructible]
        public DecorativeShield9() : base(0x157C) => Movable = false;
    }

    [Flippable(0x157E, 0x157F)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShield10 : Item
    {
        [Constructible]
        public DecorativeShield10() : base(0x157E) => Movable = false;
    }

    [Flippable(0x1580, 0x1581)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShield11 : Item
    {
        [Constructible]
        public DecorativeShield11() : base(0x1580) => Movable = false;
    }

    [Flippable(0x1582, 0x1583, 0x1634, 0x1635)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShieldSword1North : Item
    {
        [Constructible]
        public DecorativeShieldSword1North() : base(Utility.Random(0x1582, 2)) => Movable = false;
    }

    [Flippable(0x1634, 0x1635, 0x1582, 0x1583)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShieldSword1West : Item
    {
        [Constructible]
        public DecorativeShieldSword1West() : base(Utility.Random(0x1634, 2)) => Movable = false;
    }

    [Flippable(0x1584, 0x1585, 0x1636, 0x1637)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShieldSword2North : Item
    {
        [Constructible]
        public DecorativeShieldSword2North() : base(Utility.Random(0x1584, 2)) => Movable = false;
    }

    [Flippable(0x1636, 0x1637, 0x1584, 0x1585)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeShieldSword2West : Item
    {
        [Constructible]
        public DecorativeShieldSword2West() : base(Utility.Random(0x1636, 2)) => Movable = false;
    }
}
