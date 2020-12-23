using System.Collections.Generic;
using Server.Multis;

namespace Server.Items
{
    public enum HolidayTreeType
    {
        Classic,
        Modern
    }

    public class HolidayTree : Item, IAddon
    {
        private List<Item> m_Components;

        public HolidayTree(Mobile from, HolidayTreeType type, Point3D loc) : base(1)
        {
            Movable = false;
            MoveToWorld(loc, from.Map);

            Placer = from;
            m_Components = new List<Item>();

            switch (type)
            {
                case HolidayTreeType.Classic:
                    {
                        ItemID = 0xCD7;

                        AddItem(0, 0, 0, new TreeTrunk(this, 0xCD6));

                        AddOrnament(0, 0, 2, 0xF22);
                        AddOrnament(0, 0, 9, 0xF18);
                        AddOrnament(0, 0, 15, 0xF20);
                        AddOrnament(0, 0, 19, 0xF17);
                        AddOrnament(0, 0, 20, 0xF24);
                        AddOrnament(0, 0, 20, 0xF1F);
                        AddOrnament(0, 0, 20, 0xF19);
                        AddOrnament(0, 0, 21, 0xF1B);
                        AddOrnament(0, 0, 28, 0xF2F);
                        AddOrnament(0, 0, 30, 0xF23);
                        AddOrnament(0, 0, 32, 0xF2A);
                        AddOrnament(0, 0, 33, 0xF30);
                        AddOrnament(0, 0, 34, 0xF29);
                        AddOrnament(0, 1, 7, 0xF16);
                        AddOrnament(0, 1, 7, 0xF1E);
                        AddOrnament(0, 1, 12, 0xF0F);
                        AddOrnament(0, 1, 13, 0xF13);
                        AddOrnament(0, 1, 18, 0xF12);
                        AddOrnament(0, 1, 19, 0xF15);
                        AddOrnament(0, 1, 25, 0xF28);
                        AddOrnament(0, 1, 29, 0xF1A);
                        AddOrnament(0, 1, 37, 0xF2B);
                        AddOrnament(1, 0, 13, 0xF10);
                        AddOrnament(1, 0, 14, 0xF1C);
                        AddOrnament(1, 0, 16, 0xF14);
                        AddOrnament(1, 0, 17, 0xF26);
                        AddOrnament(1, 0, 22, 0xF27);

                        break;
                    }
                case HolidayTreeType.Modern:
                    {
                        ItemID = 0x1B7E;

                        AddOrnament(0, 0, 2, 0xF2F);
                        AddOrnament(0, 0, 2, 0xF20);
                        AddOrnament(0, 0, 2, 0xF22);
                        AddOrnament(0, 0, 5, 0xF30);
                        AddOrnament(0, 0, 5, 0xF15);
                        AddOrnament(0, 0, 5, 0xF1F);
                        AddOrnament(0, 0, 5, 0xF2B);
                        AddOrnament(0, 0, 6, 0xF0F);
                        AddOrnament(0, 0, 7, 0xF1E);
                        AddOrnament(0, 0, 7, 0xF24);
                        AddOrnament(0, 0, 8, 0xF29);
                        AddOrnament(0, 0, 9, 0xF18);
                        AddOrnament(0, 0, 14, 0xF1C);
                        AddOrnament(0, 0, 15, 0xF13);
                        AddOrnament(0, 0, 15, 0xF20);
                        AddOrnament(0, 0, 16, 0xF26);
                        AddOrnament(0, 0, 17, 0xF12);
                        AddOrnament(0, 0, 18, 0xF17);
                        AddOrnament(0, 0, 20, 0xF1B);
                        AddOrnament(0, 0, 23, 0xF28);
                        AddOrnament(0, 0, 25, 0xF18);
                        AddOrnament(0, 0, 25, 0xF2A);
                        AddOrnament(0, 1, 7, 0xF16);

                        break;
                    }
            }
        }

        public HolidayTree(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Placer { get; set; }

        public override int LabelNumber => 1041117; // a tree for the holidays

        public bool CouldFit(IPoint3D p, Map map) => map.CanFit((Point3D)p, 20);

        Item IAddon.Deed => new HolidayTreeDeed();

        public override void OnAfterDelete()
        {
            for (var i = 0; i < m_Components.Count; ++i)
            {
                m_Components[i].Delete();
            }
        }

        private void AddOrnament(int x, int y, int z, int itemID)
        {
            AddItem(x + 1, y + 1, z + 11, new Ornament(itemID));
        }

        private void AddItem(int x, int y, int z, Item item)
        {
            item.MoveToWorld(new Point3D(Location.X + x, Location.Y + y, Location.Z + z), Map);

            m_Components.Add(item);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(Placer);

            writer.Write(m_Components.Count);

            for (var i = 0; i < m_Components.Count; ++i)
            {
                writer.Write(m_Components[i]);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        Placer = reader.ReadEntity<Mobile>();

                        goto case 0;
                    }
                case 0:
                    {
                        var count = reader.ReadInt();

                        m_Components = new List<Item>(count);

                        for (var i = 0; i < count; ++i)
                        {
                            var item = reader.ReadEntity<Item>();

                            if (item != null)
                            {
                                m_Components.Add(item);
                            }
                        }

                        break;
                    }
            }

            Timer.DelayCall(ValidatePlacement);
        }

        public void ValidatePlacement()
        {
            if (BaseHouse.FindHouseAt(this) == null)
            {
                var deed = new HolidayTreeDeed();
                deed.MoveToWorld(Location, Map);
                Delete();
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(GetWorldLocation(), 1))
            {
                if (Placer == null || from == Placer || from.AccessLevel >= AccessLevel.GameMaster)
                {
                    from.AddToBackpack(new HolidayTreeDeed());

                    Delete();

                    var house = BaseHouse.FindHouseAt(this);

                    if (house?.Addons.Contains(this) == true)
                    {
                        house.Addons.Remove(this);
                    }

                    from.SendLocalizedMessage(503393); // A deed for the tree has been placed in your backpack.
                }
                else
                {
                    from.SendLocalizedMessage(503396); // You cannot take this tree down.
                }
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        private class Ornament : Item
        {
            public Ornament(int itemID) : base(itemID) => Movable = false;

            public Ornament(Serial serial) : base(serial)
            {
            }

            public override int LabelNumber => 1041118; // a tree ornament

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

        private class TreeTrunk : Item
        {
            private HolidayTree m_Tree;

            public TreeTrunk(HolidayTree tree, int itemID) : base(itemID)
            {
                Movable = false;
                MoveToWorld(tree.Location, tree.Map);

                m_Tree = tree;
            }

            public TreeTrunk(Serial serial) : base(serial)
            {
            }

            public override int LabelNumber => 1041117; // a tree for the holidays

            public override void OnDoubleClick(Mobile from)
            {
                if (m_Tree?.Deleted == false)
                {
                    m_Tree.OnDoubleClick(from);
                }
            }

            public override void Serialize(IGenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write(0); // version

                writer.Write(m_Tree);
            }

            public override void Deserialize(IGenericReader reader)
            {
                base.Deserialize(reader);

                var version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            m_Tree = reader.ReadEntity<HolidayTree>();

                            if (m_Tree == null)
                            {
                                Delete();
                            }

                            break;
                        }
                }
            }
        }
    }
}
