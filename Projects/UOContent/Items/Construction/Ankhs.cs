using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Items
{
    public static class Ankhs
    {
        public const int ResurrectRange = 2;
        public const int TitheRange = 2;
        public const int LockRange = 2;

        public static void GetContextMenuEntries(Mobile from, Item item, ref PooledRefList<ContextMenuEntry> list)
        {
            if (from is PlayerMobile mobile)
            {
                list.Add(new LockKarmaEntry(mobile.KarmaLocked));
            }

            list.Add(new ResurrectEntry(from.Alive));

            if (Core.AOS)
            {
                list.Add(new TitheEntry(from.Alive));
            }
        }

        public static void Resurrect(Mobile m, Item item)
        {
            if (m.Alive)
            {
                return;
            }

            if (!m.InRange(item.GetWorldLocation(), ResurrectRange))
            {
                m.SendLocalizedMessage(500446); // That is too far away.
            }
            else if (m.Map?.CanFit(m.Location, 16, false, false) == true)
            {
                m.SendGump(new ResurrectGump(m, ResurrectMessage.VirtueShrine));
            }
            else
            {
                m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
            }
        }

        private class ResurrectEntry : ContextMenuEntry
        {
            public ResurrectEntry(bool enabled) : base(6195, ResurrectRange) => Enabled = enabled;

            public override void OnClick(Mobile from, IEntity target)
            {
                if (target is Item item)
                {
                    Resurrect(from, item);
                }
            }
        }

        public class LockKarmaEntry : ContextMenuEntry
        {
            public LockKarmaEntry(bool karmaLocked) : base(karmaLocked ? 6197 : 6196, LockRange)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (from is not PlayerMobile pm || target is not Item item)
                {
                    return;
                }

                if (!from.InRange(item.GetWorldLocation(), 2))
                {
                    from.SendLocalizedMessage(500446); // That is too far away.
                }

                pm.KarmaLocked = !pm.KarmaLocked;

                if (pm.KarmaLocked)
                {
                    // Your karma has been locked. Your karma can no longer be raised.
                    pm.SendLocalizedMessage(1060192);
                }
                else
                {
                    pm.SendLocalizedMessage(1060191); // Your karma has been unlocked. Your karma can be raised again.
                }
            }
        }

        private class TitheEntry : ContextMenuEntry
        {
            public TitheEntry(bool enabled) : base(6198, TitheRange) => Enabled = enabled;

            public override void OnClick(Mobile from, IEntity target)
            {
                if (from.CheckAlive())
                {
                    from.SendGump(new TithingGump(from));
                }
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class AnkhWest : Item
    {
        [SerializableField(0, getter: "private", setter: "private")]
        private InternalItem _item;

        [Constructible]
        public AnkhWest(bool bloodied = false) : base(bloodied ? 0x1D98 : 0x3)
        {
            Movable = false;
            _item = new InternalItem(bloodied, this);
        }

        public override bool HandlesOnMovement => true; // Tell the core that we implement OnMovement

        [Hue]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get => base.Hue;
            set
            {
                base.Hue = value;
                if (_item.Hue != value)
                {
                    _item.Hue = value;
                }
            }
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (Parent == null && Utility.InRange(Location, m.Location, 1) && !Utility.InRange(Location, oldLocation, 1))
            {
                Ankhs.Resurrect(m, this);
            }
        }

        public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, ref list);
            Ankhs.GetContextMenuEntries(from, this, ref list);
        }

        public override void OnDoubleClickDead(Mobile m)
        {
            Ankhs.Resurrect(m, this);
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

        [SerializationGenerator(0, false)]
        private partial class InternalItem : Item
        {
            [SerializableField(0)]
            private AnkhWest _item;

            public InternalItem(bool bloodied, AnkhWest item) : base(bloodied ? 0x1D97 : 0x2)
            {
                Movable = false;
                _item = item;
            }

            public override bool HandlesOnMovement => true; // Tell the core that we implement OnMovement

            [Hue]
            [CommandProperty(AccessLevel.GameMaster)]
            public override int Hue
            {
                get => base.Hue;
                set
                {
                    base.Hue = value;
                    if (_item.Hue != value)
                    {
                        _item.Hue = value;
                    }
                }
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

            public override void OnMovement(Mobile m, Point3D oldLocation)
            {
                if (Parent == null && Utility.InRange(Location, m.Location, 1) && !Utility.InRange(Location, oldLocation, 1))
                {
                    Ankhs.Resurrect(m, this);
                }
            }

            public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
            {
                base.GetContextMenuEntries(from, ref list);
                Ankhs.GetContextMenuEntries(from, this, ref list);
            }

            public override void OnDoubleClickDead(Mobile m)
            {
                Ankhs.Resurrect(m, this);
            }
        }
    }

    [TypeAlias("Server.Items.AnkhEast")]
    [SerializationGenerator(0, false)]
    public partial class AnkhNorth : Item
    {
        [SerializableField(0, getter: "private", setter: "private")]
        private InternalItem _item;

        [Constructible]
        public AnkhNorth(bool bloodied = false) : base(bloodied ? 0x1E5D : 0x4)
        {
            Movable = false;

            _item = new InternalItem(bloodied, this);
        }

        public override bool HandlesOnMovement => true; // Tell the core that we implement OnMovement

        [Hue]
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get => base.Hue;
            set
            {
                base.Hue = value;
                if (_item.Hue != value)
                {
                    _item.Hue = value;
                }
            }
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (Parent == null && Utility.InRange(Location, m.Location, 1) && !Utility.InRange(Location, oldLocation, 1))
            {
                Ankhs.Resurrect(m, this);
            }
        }

        public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, ref list);
            Ankhs.GetContextMenuEntries(from, this, ref list);
        }

        public override void OnDoubleClickDead(Mobile m)
        {
            Ankhs.Resurrect(m, this);
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

        [TypeAlias("Server.Items.AnkhEast+InternalItem")]
        [SerializationGenerator(0, false)]
        private partial class InternalItem : Item
        {
            [SerializableField(0)]
            private AnkhNorth _item;

            public InternalItem(bool bloodied, AnkhNorth item)
                : base(bloodied ? 0x1E5C : 0x5)
            {
                Movable = false;
                _item = item;
            }

            public override bool HandlesOnMovement => true; // Tell the core that we implement OnMovement

            [Hue]
            [CommandProperty(AccessLevel.GameMaster)]
            public override int Hue
            {
                get => base.Hue;
                set
                {
                    base.Hue = value;
                    if (_item.Hue != value)
                    {
                        _item.Hue = value;
                    }
                }
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

            public override void OnMovement(Mobile m, Point3D oldLocation)
            {
                if (Parent == null && Utility.InRange(Location, m.Location, 1) && !Utility.InRange(Location, oldLocation, 1))
                {
                    Ankhs.Resurrect(m, this);
                }
            }

            public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
            {
                base.GetContextMenuEntries(from, ref list);
                Ankhs.GetContextMenuEntries(from, this, ref list);
            }

            public override void OnDoubleClickDead(Mobile m)
            {
                Ankhs.Resurrect(m, this);
            }
        }
    }
}
