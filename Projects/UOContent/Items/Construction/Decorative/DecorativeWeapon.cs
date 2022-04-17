using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x155E, 0x155F, 0x155C, 0x155D)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeBowWest : Item
    {
        [Constructible]
        public DecorativeBowWest() : base(Utility.Random(0x155E, 2)) => Movable = false;
    }

    [Flippable(0x155C, 0x155D, 0x155E, 0x155F)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeBowNorth : Item
    {
        [Constructible]
        public DecorativeBowNorth() : base(Utility.Random(0x155C, 2)) => Movable = false;
    }

    [Flippable(0x1560, 0x1561, 0x1562, 0x1563)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeAxeNorth : Item
    {
        [Constructible]
        public DecorativeAxeNorth() : base(Utility.Random(0x1560, 2)) => Movable = false;
    }

    [Flippable(0x1562, 0x1563, 0x1560, 0x1561)]
    [SerializationGenerator(0, false)]
    public partial class DecorativeAxeWest : Item
    {
        [Constructible]
        public DecorativeAxeWest() : base(Utility.Random(0x1562, 2)) => Movable = false;
    }

    [SerializationGenerator(0, false)]
    public partial class DecorativeSwordNorth : Item
    {
        [SerializableField(0, getter: "private", setter: "private")]
        private InternalItem _item;

        [Constructible]
        public DecorativeSwordNorth() : base(0x1565)
        {
            Movable = false;
            _item = new InternalItem(this);
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (_item != null)
            {
                _item.Location = new Point3D(X - 1, Y, Z);
            }
        }

        public override void OnMapChange()
        {
            if (_item != null)
            {
                _item.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            _item?.Delete();
        }

        [SerializationGenerator(0, false)]
        private partial class InternalItem : Item
        {
            [SerializableField(0, getter: "private", setter: "private")]
            private DecorativeSwordNorth _item;

            public InternalItem(DecorativeSwordNorth item) : base(0x1564)
            {
                Movable = true;

                _item = item;
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (_item != null)
                {
                    _item.Location = new Point3D(X + 1, Y, Z);
                }
            }

            public override void OnMapChange()
            {
                if (_item != null)
                {
                    _item.Map = Map;
                }
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                _item?.Delete();
            }
        }
    }

    [SerializationGenerator(0)]
    public partial class DecorativeSwordWest : Item
    {
        [SerializableField(0, getter: "private", setter: "private")]
        private InternalItem _item;

        [Constructible]
        public DecorativeSwordWest() : base(0x1566)
        {
            Movable = false;
            _item = new InternalItem(this);
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (_item != null)
            {
                _item.Location = new Point3D(X, Y - 1, Z);
            }
        }

        public override void OnMapChange()
        {
            if (_item != null)
            {
                _item.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            _item?.Delete();
        }

        [SerializationGenerator(0)]
        private partial class InternalItem : Item
        {
            [SerializableField(0, getter: "private", setter: "private")]
            private DecorativeSwordWest _item;

            public InternalItem(DecorativeSwordWest item) : base(0x1567)
            {
                Movable = true;
                _item = item;
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (_item != null)
                {
                    _item.Location = new Point3D(X, Y + 1, Z);
                }
            }

            public override void OnMapChange()
            {
                if (_item != null)
                {
                    _item.Map = Map;
                }
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                _item?.Delete();
            }
        }
    }

    [SerializationGenerator(0)]
    public partial class DecorativeDAxeNorth : Item
    {
        [SerializableField(0, getter: "private", setter: "private")]
        private InternalItem _item;

        [Constructible]
        public DecorativeDAxeNorth() : base(0x1569)
        {
            Movable = false;
            _item = new InternalItem(this);
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (_item != null)
            {
                _item.Location = new Point3D(X - 1, Y, Z);
            }
        }

        public override void OnMapChange()
        {
            if (_item != null)
            {
                _item.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            _item?.Delete();
        }

        [SerializationGenerator(0)]
        private partial class InternalItem : Item
        {
            [SerializableField(0, getter: "private", setter: "private")]
            private DecorativeDAxeNorth _item;

            public InternalItem(DecorativeDAxeNorth item) : base(0x1568)
            {
                Movable = true;

                _item = item;
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (_item != null)
                {
                    _item.Location = new Point3D(X + 1, Y, Z);
                }
            }

            public override void OnMapChange()
            {
                if (_item != null)
                {
                    _item.Map = Map;
                }
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                _item?.Delete();
            }
        }
    }

    [SerializationGenerator(0)]
    public partial class DecorativeDAxeWest : Item
    {
        [SerializableField(0, getter: "private", setter: "private")]
        private InternalItem _item;

        [Constructible]
        public DecorativeDAxeWest() : base(0x156A)
        {
            Movable = false;

            _item = new InternalItem(this);
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (_item != null)
            {
                _item.Location = new Point3D(X, Y - 1, Z);
            }
        }

        public override void OnMapChange()
        {
            if (_item != null)
            {
                _item.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            _item?.Delete();
        }

        [SerializationGenerator(0)]
        private partial class InternalItem : Item
        {
            [SerializableField(0, getter: "private", setter: "private")]
            private DecorativeDAxeWest _item;

            public InternalItem(DecorativeDAxeWest item) : base(0x156B)
            {
                Movable = true;

                _item = item;
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (_item != null)
                {
                    _item.Location = new Point3D(X, Y + 1, Z);
                }
            }

            public override void OnMapChange()
            {
                if (_item != null)
                {
                    _item.Map = Map;
                }
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                _item?.Delete();
            }
        }
    }
}
