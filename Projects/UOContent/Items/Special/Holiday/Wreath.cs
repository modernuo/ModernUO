using ModernUO.Serialization;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class WreathAddon : Item, IDyable, IAddon
{
    [Constructible]
    public WreathAddon() : this(Utility.RandomDyedHue())
    {
    }

    [Constructible]
    public WreathAddon(int hue) : base(0x232C)
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

        if (ItemID == 0x232C)
        {
            return BaseAddon.IsWall(p.X, p.Y - 1, p.Z, map); // North wall
        }

        return BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map); // West wall
    }

    public Item Deed => new WreathDeed(Hue);

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

        if (house?.IsCoOwner(from) == true)
        {
            if (from.InRange(GetWorldLocation(), 3))
            {
                from.CloseGump<WreathAddonGump>();
                from.SendGump(new WreathAddonGump(from, this));
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }
    }

    private class WreathAddonGump : Gump
    {
        private readonly WreathAddon _addon;
        private readonly Mobile _from;

        public WreathAddonGump(Mobile from, WreathAddon addon) : base(150, 50)
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

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (_addon.Deleted)
            {
                return;
            }

            if (info.ButtonID == 1)
            {
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
}

[Flippable(0x14F0, 0x14EF)]
[SerializationGenerator(0)]
public partial class WreathDeed : Item
{
    [Constructible]
    public WreathDeed() : this(Utility.RandomDyedHue())
    {
    }

    [Constructible]
    public WreathDeed(int hue) : base(0x14F0)
    {
        Weight = 1.0;
        Hue = hue;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1062837; // holiday wreath deed

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
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
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
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

        if (house?.IsCoOwner(from) == true)
        {
            var northWall = BaseAddon.IsWall(loc.X, loc.Y - 1, loc.Z, from.Map);
            var westWall = BaseAddon.IsWall(loc.X - 1, loc.Y, loc.Z, from.Map);

            if (northWall && westWall)
            {
                from.SendGump(new WreathDeedGump(from, loc, this));
            }
            else
            {
                PlaceAddon(from, loc, northWall, westWall);
            }
        }
        else
        {
            from.SendLocalizedMessage(1042036); // That location is not in your house.
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
            itemID = 0x232C;
        }
        else if (westWall)
        {
            itemID = 0x232D;
        }
        else
        {
            from.SendLocalizedMessage(1062840); // The decoration must be placed next to a wall.
        }

        if (itemID > 0)
        {
            Item addon = new WreathAddon(Hue);

            addon.ItemID = itemID;
            addon.MoveToWorld(loc, from.Map);

            house.Addons.Add(addon);
            Delete();
        }
    }

    private class WreathDeedGump : Gump
    {
        private readonly WreathDeed _deed;
        private readonly Mobile _from;
        private readonly Point3D _loc;

        public WreathDeedGump(Mobile from, Point3D loc, WreathDeed deed) : base(150, 50)
        {
            _from = from;
            _loc = loc;
            _deed = deed;

            AddBackground(0, 0, 300, 150, 0xA28);

            AddPage(0);

            AddItem(90, 30, 0x232D);
            AddItem(180, 30, 0x232C);
            AddButton(50, 35, 0x868, 0x869, 1);
            AddButton(145, 35, 0x868, 0x869, 2);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (_deed.Deleted)
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1:
                    {
                        _deed.PlaceAddon(_from, _loc, false, true);
                        break;
                    }
                case 2:
                    {
                        _deed.PlaceAddon(_from, _loc, true, false);
                        break;
                    }
            }
        }
    }
}
