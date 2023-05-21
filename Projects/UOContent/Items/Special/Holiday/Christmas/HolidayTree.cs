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

    private Item[] _components;

    public HolidayTree(Mobile from, HolidayTreeType type, Point3D loc) : base(1)
    {
        Movable = false;
        MoveToWorld(loc, from.Map);

        Placer = from;

        var index = 0;

        switch (type)
        {
            case HolidayTreeType.Classic:
                {
                    ItemID = 0xCD7;

                    _components = new Item[28];

                    AddItem(0, 0, 0, new TreeTrunk(this, 0xCD6), index++);

                    AddOrnament(0, 0, 2, 0xF22, index++);
                    AddOrnament(0, 0, 9, 0xF18, index++);
                    AddOrnament(0, 0, 15, 0xF20, index++);
                    AddOrnament(0, 0, 19, 0xF17, index++);
                    AddOrnament(0, 0, 20, 0xF24, index++);
                    AddOrnament(0, 0, 20, 0xF1F, index++);
                    AddOrnament(0, 0, 20, 0xF19, index++);
                    AddOrnament(0, 0, 21, 0xF1B, index++);
                    AddOrnament(0, 0, 28, 0xF2F, index++);
                    AddOrnament(0, 0, 30, 0xF23, index++);
                    AddOrnament(0, 0, 32, 0xF2A, index++);
                    AddOrnament(0, 0, 33, 0xF30, index++);
                    AddOrnament(0, 0, 34, 0xF29, index++);
                    AddOrnament(0, 1, 7, 0xF16, index++);
                    AddOrnament(0, 1, 7, 0xF1E, index++);
                    AddOrnament(0, 1, 12, 0xF0F, index++);
                    AddOrnament(0, 1, 13, 0xF13, index++);
                    AddOrnament(0, 1, 18, 0xF12, index++);
                    AddOrnament(0, 1, 19, 0xF15, index++);
                    AddOrnament(0, 1, 25, 0xF28, index++);
                    AddOrnament(0, 1, 29, 0xF1A, index++);
                    AddOrnament(0, 1, 37, 0xF2B, index++);
                    AddOrnament(1, 0, 13, 0xF10, index++);
                    AddOrnament(1, 0, 14, 0xF1C, index++);
                    AddOrnament(1, 0, 16, 0xF14, index++);
                    AddOrnament(1, 0, 17, 0xF26, index++);
                    AddOrnament(1, 0, 22, 0xF27, index);
                    break;
                }
            case HolidayTreeType.Modern:
                {
                    ItemID = 0x1B7E;
                    _components = new Item[23];

                    AddOrnament(0, 0, 2, 0xF2F, index++);
                    AddOrnament(0, 0, 2, 0xF20, index++);
                    AddOrnament(0, 0, 2, 0xF22, index++);
                    AddOrnament(0, 0, 5, 0xF30, index++);
                    AddOrnament(0, 0, 5, 0xF15, index++);
                    AddOrnament(0, 0, 5, 0xF1F, index++);
                    AddOrnament(0, 0, 5, 0xF2B, index++);
                    AddOrnament(0, 0, 6, 0xF0F, index++);
                    AddOrnament(0, 0, 7, 0xF1E, index++);
                    AddOrnament(0, 0, 7, 0xF24, index++);
                    AddOrnament(0, 0, 8, 0xF29, index++);
                    AddOrnament(0, 0, 9, 0xF18, index++);
                    AddOrnament(0, 0, 14, 0xF1C, index++);
                    AddOrnament(0, 0, 15, 0xF13, index++);
                    AddOrnament(0, 0, 15, 0xF20, index++);
                    AddOrnament(0, 0, 16, 0xF26, index++);
                    AddOrnament(0, 0, 17, 0xF12, index++);
                    AddOrnament(0, 0, 18, 0xF17, index++);
                    AddOrnament(0, 0, 20, 0xF1B, index++);
                    AddOrnament(0, 0, 23, 0xF28, index++);
                    AddOrnament(0, 0, 25, 0xF18, index++);
                    AddOrnament(0, 0, 25, 0xF2A, index++);
                    AddOrnament(0, 1, 7, 0xF16, index);
                    break;
                }
        }
    }

    public override int LabelNumber => 1041117; // a tree for the holidays

    public bool CouldFit(IPoint3D p, Map map) => map.CanFit((Point3D)p, 20);

    Item IAddon.Deed => new HolidayTreeDeed();

    public override void OnAfterDelete()
    {
        for (var i = 0; i < _components.Length; ++i)
        {
            _components[i]?.Delete();
        }

        _components = null;
    }

    private void AddOrnament(int x, int y, int z, int itemID, int index)
    {
        AddItem(x + 1, y + 1, z + 11, new Ornament(itemID), index);
    }

    private void AddItem(int x, int y, int z, Item item, int index)
    {
        item.MoveToWorld(new Point3D(Location.X + x, Location.Y + y, Location.Z + z), Map);

        _components[index] = item;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _placer = reader.ReadEntity<Mobile>();

        var count = reader.ReadInt();

        _components = new Item[count];

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
        if (BaseHouse.FindHouseAt(this) == null)
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

    [SerializationGenerator(0, false)]
    private partial class Ornament : Item
    {
        public Ornament(int itemID) : base(itemID) => Movable = false;

        public override int LabelNumber => 1041118; // a tree ornament
    }

    [SerializationGenerator(0, false)]
    private partial class TreeTrunk : Item
    {
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
    }
}
