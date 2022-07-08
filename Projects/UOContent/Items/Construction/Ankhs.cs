using System.Collections.Generic;
using ModernUO.Serialization;
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

        public static void GetContextMenuEntries(Mobile from, Item item, List<ContextMenuEntry> list)
        {
            if (from is PlayerMobile mobile)
            {
                list.Add(new LockKarmaEntry(mobile));
            }

            list.Add(new ResurrectEntry(from, item));

            if (Core.AOS)
            {
                list.Add(new TitheEntry(from));
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
                m.CloseGump<ResurrectGump>();
                m.SendGump(new ResurrectGump(m, ResurrectMessage.VirtueShrine));
            }
            else
            {
                m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
            }
        }

        private class ResurrectEntry : ContextMenuEntry
        {
            private readonly Item m_Item;
            private readonly Mobile m_Mobile;

            public ResurrectEntry(Mobile mobile, Item item) : base(6195, ResurrectRange)
            {
                m_Mobile = mobile;
                m_Item = item;

                Enabled = !m_Mobile.Alive;
            }

            public override void OnClick()
            {
                Resurrect(m_Mobile, m_Item);
            }
        }

        private class LockKarmaEntry : ContextMenuEntry
        {
            private readonly PlayerMobile m_Mobile;

            public LockKarmaEntry(PlayerMobile mobile) : base(mobile.KarmaLocked ? 6197 : 6196, LockRange) =>
                m_Mobile = mobile;

            public override void OnClick()
            {
                m_Mobile.KarmaLocked = !m_Mobile.KarmaLocked;

                if (m_Mobile.KarmaLocked)
                {
                    // Your karma has been locked. Your karma can no longer be raised.
                    m_Mobile.SendLocalizedMessage(1060192);
                }
                else
                {
                    m_Mobile.SendLocalizedMessage(1060191); // Your karma has been unlocked. Your karma can be raised again.
                }
            }
        }

        private class TitheEntry : ContextMenuEntry
        {
            private readonly Mobile m_Mobile;

            public TitheEntry(Mobile mobile) : base(6198, TitheRange)
            {
                m_Mobile = mobile;

                Enabled = m_Mobile.Alive;
            }

            public override void OnClick()
            {
                if (m_Mobile.CheckAlive())
                {
                    m_Mobile.SendGump(new TithingGump(m_Mobile, 0));
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

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            Ankhs.GetContextMenuEntries(from, this, list);
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

            public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
            {
                base.GetContextMenuEntries(from, list);
                Ankhs.GetContextMenuEntries(from, this, list);
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

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            Ankhs.GetContextMenuEntries(from, this, list);
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

            public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
            {
                base.GetContextMenuEntries(from, list);
                Ankhs.GetContextMenuEntries(from, this, list);
            }

            public override void OnDoubleClickDead(Mobile m)
            {
                Ankhs.Resurrect(m, this);
            }
        }
    }
}
