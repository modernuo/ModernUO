using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Multis;

namespace Server.Items;

public enum HolidayTreeType
{
    Classic,
    Modern
}

[SerializationGenerator(2, false)]
public partial class HolidayTree : Item, IAddon
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _placer;

    [SerializableField(1, setter: "private")]
    private List<Item> _components;

    public HolidayTree(Mobile from, HolidayTreeType type, Point3D loc) : base(1)
    {
        Movable = false;
        MoveToWorld(loc, from.Map);

        Placer = from;

        switch (type)
        {
            case HolidayTreeType.Classic:
                {
                    ItemID = 0xCD7;

                    _components = new List<Item>(28);
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

                    _components = new List<Item>(23);
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

    public override int LabelNumber => 1041117; // a tree for the holidays

    public bool CouldFit(IPoint3D p, Map map) => map.CanFit((Point3D)p, 20);

    Item IAddon.Deed => new HolidayTreeDeed();

    public override void OnAfterDelete()
    {
        foreach (var c in _components)
        {
            c?.Delete();
        }

        _components = null;
    }

    private void AddOrnament(int x, int y, int z, int itemID)
    {
        AddItem(x + 1, y + 1, z + 11, new Ornament(this, itemID));
    }

    private void AddItem(int x, int y, int z, Item item)
    {
        item.MoveToWorld(new Point3D(Location.X + x, Location.Y + y, Location.Z + z), Map);

        _components.Add(item);
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _placer = reader.ReadEntity<Mobile>();

        var count = reader.ReadInt();

        for (var i = 0; i < count; ++i)
        {
            var item = reader.ReadEntity<Item>();

            if (item != null)
            {
                _components[i] = item;
            }
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        // Bug with older trees, or trees that belong to a house that doesn't exist should be redeeded.
        if (_components == null || _components.Count == 0 || BaseHouse.FindHouseAt(this) == null)
        {
            var deed = new HolidayTreeDeed();
            deed.MoveToWorld(Location, Map);
            Delete();
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 1))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
            return;
        }

        if (_placer != null && from != _placer && from.AccessLevel < AccessLevel.GameMaster)
        {
            from.SendLocalizedMessage(503396); // You cannot take this tree down.
            return;
        }

        from.AddToBackpack(new HolidayTreeDeed());

        Delete();

        BaseHouse.FindHouseAt(this)?.Addons?.Remove(this);
        from.SendLocalizedMessage(503393); // A deed for the tree has been placed in your backpack.
    }

    [SerializationGenerator(1, false)]
    public partial class Ornament : Item
    {
        [SerializableField(0)]
        private HolidayTree _tree;

        public Ornament(HolidayTree tree, int itemID) : base(itemID)
        {
            Movable = false;
            _tree = tree;
        }

        public override int LabelNumber => 1041118; // a tree ornaments

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (_tree == null)
            {
                Timer.DelayCall(Delete); // There was an issue and old trees will be regenerated
            }
            else
            {
                _tree._components.Add(this);
            }
        }

        private void MigrateFrom(V0Content content)
        {

        }
    }

    [SerializationGenerator(1, false)]
    public partial class TreeTrunk : Item
    {
        [SerializableField(0)]
        private HolidayTree _tree;

        public TreeTrunk(HolidayTree tree, int itemID) : base(itemID)
        {
            Movable = false;
            MoveToWorld(tree.Location, tree.Map);
            _tree = tree;
        }

        public override int LabelNumber => 1041117; // a tree for the holidays

        public override void OnDoubleClick(Mobile from)
        {
            if (_tree?.Deleted == false)
            {
                _tree.OnDoubleClick(from);
            }
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            if (_tree == null)
            {
                Timer.DelayCall(Delete); // There was an issue and old trees will be regenerated
            }
            else
            {
                _tree._components.Add(this);
            }
        }

        private void MigrateFrom(V0Content content)
        {
        }
    }
}
