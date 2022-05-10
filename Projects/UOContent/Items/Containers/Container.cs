using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.Items;

[ManualDirtyChecking]
public abstract class BaseContainer : Container
{
    public BaseContainer(int itemID) : base(itemID)
    {
    }

    public BaseContainer(Serial serial) : base(serial)
    {
    }

    public override int DefaultMaxWeight => IsSecure ? 0 : base.DefaultMaxWeight;

    public override bool IsAccessibleTo(Mobile m) => BaseHouse.CheckAccessible(m, this) && base.IsAccessibleTo(m);

    public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
    {
        if (IsSecure && !BaseHouse.CheckHold(m, this, item, message, checkItems, plusItems, plusWeight))
        {
            return false;
        }

        return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
    }

    public override bool CheckItemUse(Mobile from, Item item)
    {
        if (IsDecoContainer && item is BaseBook)
        {
            return true;
        }

        return base.CheckItemUse(from, item);
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);
        SetSecureLevelEntry.AddTo(from, this, list);
    }

    public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
    {
        if (!CheckHold(from, dropped, sendFullMessage, true))
        {
            return false;
        }

        var house = BaseHouse.FindHouseAt(this);

        if (house?.HasLockedDownItem(this) == true)
        {
            if (dropped is VendorRentalContract || dropped is Container container &&
                container.FindItemByType<VendorRentalContract>() != null)
            {
                from.SendLocalizedMessage(1062492); // You cannot place a rental contract in a locked down container.
                return false;
            }

            if (!house.LockDown(from, dropped, false))
            {
                return false;
            }
        }

        var list = Items;

        for (var i = 0; i < list.Count; ++i)
        {
            var item = list[i];

            if (item is not Container && item.StackWith(from, dropped, false))
            {
                return true;
            }
        }

        DropItem(dropped);

        return true;
    }

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        if (!CheckHold(from, item, true, true))
        {
            return false;
        }

        var house = BaseHouse.FindHouseAt(this);

        if (house?.HasLockedDownItem(this) == true)
        {
            if (item is VendorRentalContract || item is Container container &&
                container.FindItemByType<VendorRentalContract>() != null)
            {
                from.SendLocalizedMessage(1062492); // You cannot place a rental contract in a locked down container.
                return false;
            }

            if (!house.LockDown(from, item, false))
            {
                return false;
            }
        }

        item.Location = new Point3D(p.X, p.Y, 0);
        AddItem(item);

        from.SendSound(GetDroppedSound(item), GetWorldLocation());

        return true;
    }

    public override void UpdateTotal(Item sender, TotalType type, int delta)
    {
        base.UpdateTotal(sender, type, delta);

        if (type == TotalType.Weight)
        {
            (RootParent as Mobile)?.InvalidateProperties();
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.Player || from.InRange(GetWorldLocation(), 2) || RootParent is PlayerVendor)
        {
            Open(from);
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    public virtual void Open(Mobile from)
    {
        DisplayTo(from);
    }

    /* Note: base class insertion; we cannot serialize anything here */
    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);
    }
}

[SerializationGenerator(0, false)]
public partial class CreatureBackpack : Backpack // Used on BaseCreature
{
    [Constructible]
    public CreatureBackpack(string name)
    {
        Name = name;
        Layer = Layer.Backpack;
        Hue = 5;
        Weight = 3.0;
    }

    public override void AddNameProperty(ObjectPropertyList list)
    {
        if (Name != null)
        {
            list.Add(1075257, Name); // Contents of ~1_PETNAME~'s pack.
        }
        else
        {
            base.AddNameProperty(list);
        }
    }

    public override void OnItemRemoved(Item item)
    {
        if (Items.Count == 0)
        {
            Delete();
        }

        base.OnItemRemoved(item);
    }

    public override bool OnDragLift(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        from.SendLocalizedMessage(500169); // You cannot pick that up.
        return false;
    }

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p) => false;

    public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage) => false;
}

[SerializationGenerator(0, false)]
public partial class StrongBackpack : Backpack // Used on Pack animals
{
    [Constructible]
    public StrongBackpack()
    {
        Layer = Layer.Backpack;
        Weight = 13.0;
    }

    public override int DefaultMaxWeight => 1600;

    public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight) =>
        base.CheckHold(m, item, false, checkItems, plusItems, plusWeight);

    public override bool CheckContentDisplay(Mobile from) =>
        RootParent is BaseCreature creature && creature.Controlled && creature.ControlMaster == from ||
        base.CheckContentDisplay(from);
}

[SerializationGenerator(0, false)]
public partial class Backpack : BaseContainer, IDyable
{
    [Constructible]
    public Backpack() : base(0xE75)
    {
        Layer = Layer.Backpack;
        Weight = 3.0;
    }

    public override int DefaultMaxWeight
    {
        get
        {
            if (Core.ML && Parent is Mobile m && m.Player && m.Backpack == this)
            {
                return 550;
            }

            return base.DefaultMaxWeight;
        }
    }

    public bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;

        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class Pouch : TrappableContainer
{
    [Constructible]
    public Pouch() : base(0xE79) => Weight = 1.0;
}

[SerializationGenerator(0, false)]
public abstract partial class BaseBagBall : BaseContainer, IDyable
{
    public BaseBagBall(int itemID) : base(itemID) => Weight = 1.0;

    public bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;

        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class SmallBagBall : BaseBagBall
{
    [Constructible]
    public SmallBagBall() : base(0x2256)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class LargeBagBall : BaseBagBall
{
    [Constructible]
    public LargeBagBall() : base(0x2257)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class Bag : BaseContainer, IDyable
{
    [Constructible]
    public Bag() : base(0xE76) => Weight = 2.0;

    public bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;

        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class Barrel : BaseContainer
{
    [Constructible]
    public Barrel() : base(0xE77) => Weight = 25.0;
}

[SerializationGenerator(0, false)]
public partial class Keg : BaseContainer
{
    [Constructible]
    public Keg() : base(0xE7F) => Weight = 15.0;
}

[SerializationGenerator(0, false)]
public partial class PicnicBasket : BaseContainer
{
    [Constructible]
    public PicnicBasket() : base(0xE7A) => Weight = 2.0;
}

[SerializationGenerator(0, false)]
public partial class Basket : BaseContainer
{
    [Constructible]
    public Basket() : base(0x990) => Weight = 1.0;
}

[Furniture]
[Flippable(0x9AA, 0xE7D)]
[SerializationGenerator(0, false)]
public partial class WoodenBox : LockableContainer
{
    [Constructible]
    public WoodenBox() : base(0x9AA) => Weight = 4.0;
}

[Furniture]
[Flippable(0x9A9, 0xE7E)]
[SerializationGenerator(0, false)]
public partial class SmallCrate : LockableContainer
{
    [Constructible]
    public SmallCrate() : base(0x9A9) => Weight = 2.0;
}

[Furniture]
[Flippable(0xE3F, 0xE3E)]
[SerializationGenerator(0, false)]
public partial class MediumCrate : LockableContainer
{
    [Constructible]
    public MediumCrate() : base(0xE3F) => Weight = 2.0;
}

[Furniture]
[Flippable(0xE3D, 0xE3C)]
[SerializationGenerator(0, false)]
public partial class LargeCrate : LockableContainer
{
    [Constructible]
    public LargeCrate() : base(0xE3D) => Weight = 1.0;
}

[DynamicFlipping]
[Flippable(0x9A8, 0xE80)]
[SerializationGenerator(0, false)]
public partial class MetalBox : LockableContainer
{
    [Constructible]
    public MetalBox() : base(0x9A8)
    {
    }
}

[DynamicFlipping]
[Flippable(0x9AB, 0xE7C)]
[SerializationGenerator(0, false)]
public partial class MetalChest : LockableContainer
{
    [Constructible]
    public MetalChest() : base(0x9AB)
    {
    }
}

[DynamicFlipping, Flippable(0xE41, 0xE40)]
[SerializationGenerator(0, false)]
public partial class MetalGoldenChest : LockableContainer
{
    [Constructible]
    public MetalGoldenChest() : base(0xE41)
    {
    }
}

[Furniture]
[Flippable(0xe43, 0xe42)]
[SerializationGenerator(0, false)]
public partial class WoodenChest : LockableContainer
{
    [Constructible]
    public WoodenChest() : base(0xe43) => Weight = 2.0;
}

[Furniture]
[Flippable(0x280B, 0x280C)]
[SerializationGenerator(0, false)]
public partial class PlainWoodenChest : LockableContainer
{
    [Constructible]
    public PlainWoodenChest() : base(0x280B)
    {
    }
}

[Furniture]
[Flippable(0x280D, 0x280E)]
[SerializationGenerator(0, false)]
public partial class OrnateWoodenChest : LockableContainer
{
    [Constructible]
    public OrnateWoodenChest() : base(0x280D)
    {
    }
}

[Furniture]
[Flippable(0x280F, 0x2810)]
[SerializationGenerator(0, false)]
public partial class GildedWoodenChest : LockableContainer
{
    [Constructible]
    public GildedWoodenChest() : base(0x280F)
    {
    }
}

[Furniture]
[Flippable(0x2811, 0x2812)]
[SerializationGenerator(0, false)]
public partial class WoodenFootLocker : LockableContainer
{
    [Constructible]
    public WoodenFootLocker() : base(0x2811) => GumpID = 0x10B;
}

[Furniture]
[Flippable(0x2813, 0x2814)]
[SerializationGenerator(0, false)]
public partial class FinishedWoodenChest : LockableContainer
{
    [Constructible]
    public FinishedWoodenChest() : base(0x2813)
    {
    }
}

[Furniture]
[SerializationGenerator(0)]
[Flippable(0x2DF1, 0x2DF2)]
public partial class RarewoodChest : LockableContainer
{
    [Constructible]
    public RarewoodChest() : base(0x2DF1)
    {
    }
}

[Furniture]
[SerializationGenerator(0)]
[Flippable(0x2DF3, 0x2DF4)]
public partial class DecorativeBox : LockableContainer
{
    [Constructible]
    public DecorativeBox() : base(0x2DF4)
    {
    }
}
