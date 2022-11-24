using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Forge]
[SerializationGenerator(0, false)]
public partial class LargeForgeWest : Item
{
    [SerializableField(0, getter: "private", setter: "private")]
    private InternalItem _item;

    [SerializableField(1, getter: "private", setter: "private")]
    private InternalItem2 _item2;

    [Constructible]
    public LargeForgeWest() : base(0x199A)
    {
        Movable = false;

        _item = new InternalItem(this);
        _item2 = new InternalItem2(this);
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        if (_item != null)
        {
            _item.Location = new Point3D(X, Y + 1, Z);
        }

        if (_item2 != null)
        {
            _item2.Location = new Point3D(X, Y + 2, Z);
        }
    }

    public override void OnMapChange()
    {
        if (_item != null)
        {
            _item.Map = Map;
        }

        if (_item2 != null)
        {
            _item2.Map = Map;
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _item?.Delete();
        _item2?.Delete();
    }

    [Forge]
    [SerializationGenerator(0, false)]
    private partial class InternalItem : Item
    {
        private LargeForgeWest _parent;

        public InternalItem(LargeForgeWest item) : base(0x1996)
        {
            Movable = false;

            _parent = item;
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (_parent != null)
            {
                _parent.Location = new Point3D(X, Y - 1, Z);
            }
        }

        public override void OnMapChange()
        {
            if (_parent != null)
            {
                _parent.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            _parent?.Delete();
        }
    }

    [Forge]
    [SerializationGenerator(0, false)]
    private partial class InternalItem2 : Item
    {
        private LargeForgeWest _parent;

        public InternalItem2(LargeForgeWest item) : base(0x1992)
        {
            Movable = false;

            _parent = item;
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (_parent != null)
            {
                _parent.Location = new Point3D(X, Y - 2, Z);
            }
        }

        public override void OnMapChange()
        {
            if (_parent != null)
            {
                _parent.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            _parent?.Delete();
        }
    }
}

[Forge]
[SerializationGenerator(0, false)]
public partial class LargeForgeEast : Item
{
    private InternalItem _item;
    private InternalItem2 _item2;

    [Constructible]
    public LargeForgeEast() : base(0x197A)
    {
        Movable = false;

        _item = new InternalItem(this);
        _item2 = new InternalItem2(this);
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        if (_item != null)
        {
            _item.Location = new Point3D(X + 1, Y, Z);
        }

        if (_item2 != null)
        {
            _item2.Location = new Point3D(X + 2, Y, Z);
        }
    }

    public override void OnMapChange()
    {
        if (_item != null)
        {
            _item.Map = Map;
        }

        if (_item2 != null)
        {
            _item2.Map = Map;
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _item?.Delete();
        _item2?.Delete();
    }

    [Forge]
    [SerializationGenerator(0, false)]
    private partial class InternalItem : Item
    {
        private LargeForgeEast _parent;

        public InternalItem(LargeForgeEast item) : base(0x197E)
        {
            Movable = false;

            _parent = item;
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (_parent != null)
            {
                _parent.Location = new Point3D(X - 1, Y, Z);
            }
        }

        public override void OnMapChange()
        {
            if (_parent != null)
            {
                _parent.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            _parent?.Delete();
        }
    }

    [Forge]
    [SerializationGenerator(0, false)]
    private partial class InternalItem2 : Item
    {
        private LargeForgeEast _parent;

        public InternalItem2(LargeForgeEast item) : base(0x1982)
        {
            Movable = false;

            _parent = item;
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (_parent != null)
            {
                _parent.Location = new Point3D(X - 2, Y, Z);
            }
        }

        public override void OnMapChange()
        {
            if (_parent != null)
            {
                _parent.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            _parent?.Delete();
        }
    }
}
