using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Tapestry1N : Item
    {
        [SerializableField(0, "private", "private")]
        private InternalItem _item;

        [Constructible]
        public Tapestry1N() : base(0xEAA)
        {
            Movable = false;
            _item = new InternalItem(this);
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

        [SerializationGenerator(0)]
        private partial class InternalItem : Item
        {
            [SerializableField(0, "private", "private")]
            private Tapestry1N _item;

            public InternalItem(Tapestry1N item) : base(0xEAB)
            {
                Movable = true;
                _item = item;
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
        }
    }

    [SerializationGenerator(0, false)]
    public partial class Tapestry2N : Item
    {
        [SerializableField(0, "private", "private")]
        private InternalItem _item;

        [Constructible]
        public Tapestry2N() : base(0xEAC)
        {
            Movable = false;
            _item = new InternalItem(this);
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

        [SerializationGenerator(0, false)]
        private partial class InternalItem : Item
        {
            [SerializableField(0, "private", "private")]
            private Tapestry2N _item;

            public InternalItem(Tapestry2N item) : base(0xEAD)
            {
                Movable = true;
                _item = item;
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
        }
    }

    [SerializationGenerator(0, false)]
    public partial class Tapestry2W : Item
    {
        [SerializableField(0, "private", "private")]
        private InternalItem _item;

        [Constructible]
        public Tapestry2W() : base(0xEAE)
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

        [SerializationGenerator(0, false)]
        private partial class InternalItem : Item
        {
            [SerializableField(0, "private", "private")]
            private Tapestry2W _item;

            public InternalItem(Tapestry2W item) : base(0xEAF)
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

    [SerializationGenerator(0, false)]
    public partial class Tapestry3N : Item
    {
        [SerializableField(0, "private", "private")]
        private InternalItem _item;

        [Constructible]
        public Tapestry3N() : base(0xFD6)
        {
            Movable = false;
            _item = new InternalItem(this);
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (_item != null)
            {
                _item.Location = new Point3D(X - 2, Y, Z);
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
            [SerializableField(0, "private", "private")]
            private Tapestry3N _item;

            public InternalItem(Tapestry3N item) : base(0xFD5)
            {
                Movable = true;
                _item = item;
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (_item != null)
                {
                    _item.Location = new Point3D(X + 2, Y, Z);
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

    [SerializationGenerator(0, false)]
    public partial class Tapestry3W : Item
    {
        [SerializableField(0, "private", "private")]
        private InternalItem _item;

        [Constructible]
        public Tapestry3W() : base(0xFD7)
        {
            Movable = false;
            _item = new InternalItem(this);
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (_item != null)
            {
                _item.Location = new Point3D(X, Y - 2, Z);
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
            [SerializableField(0, "private", "private")]
            private Tapestry3W _item;

            public InternalItem(Tapestry3W item) : base(0xFD8)
            {
                Movable = true;
                _item = item;
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (_item != null)
                {
                    _item.Location = new Point3D(X, Y + 2, Z);
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

    [SerializationGenerator(0, false)]
    public partial class Tapestry4N : Item
    {
        [SerializableField(0, "private", "private")]
        private InternalItem _item;

        [Constructible]
        public Tapestry4N() : base(0xFDA)
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
            [SerializableField(0, "private", "private")]
            private Tapestry4N _item;

            public InternalItem(Tapestry4N item) : base(0xFD9)
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

    [SerializationGenerator(0, false)]
    public partial class Tapestry4W : Item
    {
        [SerializableField(0, "private", "private")]
        private InternalItem _item;

        [Constructible]
        public Tapestry4W() : base(0xFDB)
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

        [SerializationGenerator(0, false)]
        private partial class InternalItem : Item
        {
            [SerializableField(0, "private", "private")]
            private Tapestry4W _item;

            public InternalItem(Tapestry4W item) : base(0xFDC)
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

    [SerializationGenerator(0, false)]
    public partial class Tapestry5N : Item
    {
        [SerializableField(0, "private", "private")]
        private InternalItem _item;

        [Constructible]
        public Tapestry5N() : base(0xFDE)
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
            [SerializableField(0, "private", "private")]
            private Tapestry5N _item;

            public InternalItem(Tapestry5N item) : base(0xFDD)
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

    [SerializationGenerator(0, false)]
    public partial class Tapestry5W : Item
    {
        [SerializableField(0, "private", "private")]
        private InternalItem _item;

        [Constructible]
        public Tapestry5W() : base(0xFDF)
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

        [SerializationGenerator(0, false)]
        private partial class InternalItem : Item
        {
            [SerializableField(0, "private", "private")]
            private Tapestry5W _item;

            public InternalItem(Tapestry5W item) : base(0xFE0)
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

    [SerializationGenerator(0, false)]
    public partial class Tapestry6N : Item
    {
        [SerializableField(0, "private", "private")]
        private InternalItem _item;

        [Constructible]
        public Tapestry6N() : base(0xFE2)
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
            [SerializableField(0, "private", "private")]
            private Tapestry6N _item;

            public InternalItem(Tapestry6N item) : base(0xFE1)
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

    [SerializationGenerator(0, false)]
    public partial class Tapestry6W : Item
    {
        [SerializableField(0, "private", "private")]
        private InternalItem _item;

        [Constructible]
        public Tapestry6W() : base(0xFE3)
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

        [SerializationGenerator(0, false)]
        private partial class InternalItem : Item
        {
            [SerializableField(0, "private", "private")]
            private Tapestry6W _item;

            public InternalItem(Tapestry6W item) : base(0xFE4)
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
