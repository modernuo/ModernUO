namespace Server.Items
{
    [Flippable(0x155E, 0x155F, 0x155C, 0x155D)]
    [Serializable(0, false)]
    public partial class DecorativeBowWest : Item
    {
        [Constructible]
        public DecorativeBowWest() : base(Utility.Random(0x155E, 2)) => Movable = false;
    }

    [Flippable(0x155C, 0x155D, 0x155E, 0x155F)]
    [Serializable(0, false)]
    public partial class DecorativeBowNorth : Item
    {
        [Constructible]
        public DecorativeBowNorth() : base(Utility.Random(0x155C, 2)) => Movable = false;
    }

    [Flippable(0x1560, 0x1561, 0x1562, 0x1563)]
    [Serializable(0, false)]
    public partial class DecorativeAxeNorth : Item
    {
        [Constructible]
        public DecorativeAxeNorth() : base(Utility.Random(0x1560, 2)) => Movable = false;
    }

    [Flippable(0x1562, 0x1563, 0x1560, 0x1561)]
    [Serializable(0, false)]
    public partial class DecorativeAxeWest : Item
    {
        [Constructible]
        public DecorativeAxeWest() : base(Utility.Random(0x1562, 2)) => Movable = false;
    }

    [Serializable(0, false)]
    public partial class DecorativeSwordNorth : Item
    {
        [SerializableField(0)]
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
                Item.Location = new Point3D(X - 1, Y, Z);
            }
        }

        public override void OnMapChange()
        {
            if (_item != null)
            {
                Item.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            Item?.Delete();
        }

        [Serializable(0, false)]
        private partial class InternalItem : Item
        {
            [SerializableField(0)]
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
                    Item.Location = new Point3D(X + 1, Y, Z);
                }
            }

            public override void OnMapChange()
            {
                if (_item != null)
                {
                    Item.Map = Map;
                }
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                Item?.Delete();
            }
        }
    }

    public class DecorativeSwordWest : Item
    {
        private InternalItem m_Item;

        [Constructible]
        public DecorativeSwordWest() : base(0x1566)
        {
            Movable = false;

            m_Item = new InternalItem(this);
        }

        public DecorativeSwordWest(Serial serial) : base(serial)
        {
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (m_Item != null)
            {
                m_Item.Location = new Point3D(X, Y - 1, Z);
            }
        }

        public override void OnMapChange()
        {
            if (m_Item != null)
            {
                m_Item.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            m_Item?.Delete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Item);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_Item = reader.ReadEntity<InternalItem>();
        }

        private class InternalItem : Item
        {
            private DecorativeSwordWest m_Item;

            public InternalItem(DecorativeSwordWest item) : base(0x1567)
            {
                Movable = true;

                m_Item = item;
            }

            public InternalItem(Serial serial) : base(serial)
            {
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (m_Item != null)
                {
                    m_Item.Location = new Point3D(X, Y + 1, Z);
                }
            }

            public override void OnMapChange()
            {
                if (m_Item != null)
                {
                    m_Item.Map = Map;
                }
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                m_Item?.Delete();
            }

            public override void Serialize(IGenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write(0); // version

                writer.Write(m_Item);
            }

            public override void Deserialize(IGenericReader reader)
            {
                base.Deserialize(reader);

                var version = reader.ReadInt();

                m_Item = reader.ReadEntity<DecorativeSwordWest>();
            }
        }
    }

    public class DecorativeDAxeNorth : Item
    {
        private InternalItem m_Item;

        [Constructible]
        public DecorativeDAxeNorth() : base(0x1569)
        {
            Movable = false;

            m_Item = new InternalItem(this);
        }

        public DecorativeDAxeNorth(Serial serial) : base(serial)
        {
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (m_Item != null)
            {
                m_Item.Location = new Point3D(X - 1, Y, Z);
            }
        }

        public override void OnMapChange()
        {
            if (m_Item != null)
            {
                m_Item.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            m_Item?.Delete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Item);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_Item = reader.ReadEntity<InternalItem>();
        }

        private class InternalItem : Item
        {
            private DecorativeDAxeNorth m_Item;

            public InternalItem(DecorativeDAxeNorth item) : base(0x1568)
            {
                Movable = true;

                m_Item = item;
            }

            public InternalItem(Serial serial) : base(serial)
            {
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (m_Item != null)
                {
                    m_Item.Location = new Point3D(X + 1, Y, Z);
                }
            }

            public override void OnMapChange()
            {
                if (m_Item != null)
                {
                    m_Item.Map = Map;
                }
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                m_Item?.Delete();
            }

            public override void Serialize(IGenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write(0); // version

                writer.Write(m_Item);
            }

            public override void Deserialize(IGenericReader reader)
            {
                base.Deserialize(reader);

                var version = reader.ReadInt();

                m_Item = reader.ReadEntity<DecorativeDAxeNorth>();
            }
        }
    }

    public class DecorativeDAxeWest : Item
    {
        private InternalItem m_Item;

        [Constructible]
        public DecorativeDAxeWest() : base(0x156A)
        {
            Movable = false;

            m_Item = new InternalItem(this);
        }

        public DecorativeDAxeWest(Serial serial) : base(serial)
        {
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (m_Item != null)
            {
                m_Item.Location = new Point3D(X, Y - 1, Z);
            }
        }

        public override void OnMapChange()
        {
            if (m_Item != null)
            {
                m_Item.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            m_Item?.Delete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Item);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_Item = reader.ReadEntity<InternalItem>();
        }

        private class InternalItem : Item
        {
            private DecorativeDAxeWest m_Item;

            public InternalItem(DecorativeDAxeWest item) : base(0x156B)
            {
                Movable = true;

                m_Item = item;
            }

            public InternalItem(Serial serial) : base(serial)
            {
            }

            public override void OnLocationChange(Point3D oldLocation)
            {
                if (m_Item != null)
                {
                    m_Item.Location = new Point3D(X, Y + 1, Z);
                }
            }

            public override void OnMapChange()
            {
                if (m_Item != null)
                {
                    m_Item.Map = Map;
                }
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                m_Item?.Delete();
            }

            public override void Serialize(IGenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write(0); // version

                writer.Write(m_Item);
            }

            public override void Deserialize(IGenericReader reader)
            {
                base.Deserialize(reader);

                var version = reader.ReadInt();

                m_Item = reader.ReadEntity<DecorativeDAxeWest>();
            }
        }
    }
}
