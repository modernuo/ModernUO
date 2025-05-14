using ModernUO.Serialization;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MistletoeAddon : Item, IDyable, IAddon
{
    [Constructible]
    public MistletoeAddon() : this(Utility.RandomDyedHue())
    {
    }

    [Constructible]
    public MistletoeAddon(int hue) : base(0x2375)
    {
        Hue = hue;
        Movable = false;
    }

    public bool CouldFit(IPoint3D p, Map map)
    {
        if (!map.CanFit(p.X, p.Y, p.Z, ItemData.Height))
        {
            return false;
        }

        if (ItemID == 0x2375)
        {
            return BaseAddon.IsWall(p.X, p.Y - 1, p.Z, map); // North wall
        }

        return BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map);     // West wall
    }

    public Item Deed => new MistletoeDeed(Hue);

    public virtual bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        var house = BaseHouse.FindHouseAt(this);

        if (house?.IsCoOwner(from) == true)
        {
            if (from.InRange(GetWorldLocation(), 1))
            {
                Hue = sender.DyedHue;
                return true;
            }

            from.SendLocalizedMessage(500295); // You are too far away to do that.
            return false;
        }

        return false;
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        FixMovingCrate();
    }

    private void FixMovingCrate()
    {
        if (Deleted)
        {
            return;
        }

        if (Movable || IsLockedDown)
        {
            var deed = Deed;

            if (Parent is Item item)
            {
                item.AddItem(deed);
                deed.Location = Location;
            }
            else
            {
                deed.MoveToWorld(Location, Map);
            }

            Delete();
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        var house = BaseHouse.FindHouseAt(this);

        if (house?.IsCoOwner(from) != true)
        {
            return;
        }

        if (from.InRange(GetWorldLocation(), 3))
        {
            from.SendGump(new MistletoeAddonGump(from, this));
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    private class MistletoeAddonGump : Gump
    {
        private readonly MistletoeAddon _addon;
        private readonly Mobile _from;

        public override bool Singleton => true;

        public MistletoeAddonGump(Mobile from, MistletoeAddon addon) : base(150, 50)
        {
            _from = from;
            _addon = addon;

            AddPage(0);

            AddBackground(0, 0, 220, 170, 0x13BE);
            AddBackground(10, 10, 200, 150, 0xBB8);
            AddHtmlLocalized(20, 30, 180, 60, 1062839);  // Do you wish to re-deed this decoration?
            AddHtmlLocalized(55, 100, 160, 25, 1011011); // CONTINUE
            AddButton(20, 100, 0xFA5, 0xFA7, 1);
            AddHtmlLocalized(55, 125, 160, 25, 1011012); // CANCEL
            AddButton(20, 125, 0xFA5, 0xFA7, 0);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_addon.Deleted || info.ButtonID != 1)
            {
                return;
            }

            if (_from.InRange(_addon.GetWorldLocation(), 3))
            {
                _from.AddToBackpack(_addon.Deed);
                _addon.Delete();
            }
            else
            {
                _from.SendLocalizedMessage(500295); // You are too far away to do that.
            }
        }
    }
}

[Flippable(0x14F0, 0x14EF)]
[SerializationGenerator(0, false)]
public partial class MistletoeDeed : Item
{
    [Constructible]
    public MistletoeDeed(int hue = 0) : base(0x14F0)
    {
        Hue = hue;
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1070882; // Mistletoe Deed

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        LabelTo(from, 1070880); // Winter 2004
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1070880); // Winter 2004
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        var house = BaseHouse.FindHouseAt(from);

        if (house?.IsCoOwner(from) == true)
        {
            from.SendLocalizedMessage(1062838); // Where would you like to place this decoration?
            from.BeginTarget(-1, true, TargetFlags.None, Placement_OnTarget);
        }
        else
        {
            from.SendLocalizedMessage(502092); // You must be in your house to do this.
        }
    }

    public void Placement_OnTarget(Mobile from, object targeted)
    {
        if (targeted is not IPoint3D p)
        {
            return;
        }

        var loc = new Point3D(p);

        var house = BaseHouse.FindHouseAt(loc, from.Map, 16);

        if (house?.IsCoOwner(from) != true)
        {
            from.SendLocalizedMessage(1042036); // That location is not in your house.
            return;
        }

        var northWall = BaseAddon.IsWall(loc.X, loc.Y - 1, loc.Z, from.Map);
        var westWall = BaseAddon.IsWall(loc.X - 1, loc.Y, loc.Z, from.Map);

        if (northWall && westWall)
        {
            from.SendGump(new MistletoeDeedGump(loc, this));
        }
        else
        {
            PlaceAddon(from, loc, northWall, westWall);
        }
    }

    private void PlaceAddon(Mobile from, Point3D loc, bool northWall, bool westWall)
    {
        if (Deleted)
        {
            return;
        }

        var house = BaseHouse.FindHouseAt(loc, from.Map, 16);

        if (house?.IsCoOwner(from) != true)
        {
            from.SendLocalizedMessage(1042036); // That location is not in your house.
            return;
        }

        var itemID = 0;

        if (northWall)
        {
            itemID = 0x2374;
        }
        else if (westWall)
        {
            itemID = 0x2375;
        }
        else
        {
            from.SendLocalizedMessage(1070883); // The mistletoe must be placed next to a wall.
        }

        if (itemID > 0)
        {
            var addon = new MistletoeAddon(Hue)
            {
                ItemID = itemID
            };

            addon.MoveToWorld(loc, from.Map);

            house.Addons.Add(addon);
            Delete();
        }
    }

    private class MistletoeDeedGump : StaticGump<MistletoeDeedGump>
    {
        private readonly MistletoeDeed _deed;
        private readonly Point3D _loc;

        public override bool Singleton => false;

        public MistletoeDeedGump(Point3D loc, MistletoeDeed deed) : base(150, 50)
        {
            _loc = loc;
            _deed = deed;
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddBackground(0, 0, 300, 150, 0xA28);

            builder.AddPage();

            builder.AddItem(90, 30, 0x2375);
            builder.AddItem(180, 30, 0x2374);
            builder.AddButton(50, 35, 0x868, 0x869, 1);
            builder.AddButton(145, 35, 0x868, 0x869, 2);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_deed.Deleted || info.ButtonID == 0)
            {
                return;
            }

            var from = sender.Mobile;

            if (info.ButtonID == 1)
            {
                _deed.PlaceAddon(from, _loc, false, true);
            }
            else
            {
                _deed.PlaceAddon(from, _loc, true, false);
            }
        }
    }
}
