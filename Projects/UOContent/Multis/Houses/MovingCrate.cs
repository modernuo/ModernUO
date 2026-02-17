using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Network;

namespace Server.Multis;

[SerializationGenerator(0)]
public partial class MovingCrate : Container
{
    private const int MaxItemsPerSubcontainer = 20;
    private const int Rows = 3;
    private const int Columns = 5;
    private const int HorizontalSpacing = 25;
    private const int VerticalSpacing = 25;

    private Timer _internalizeTimer;

    [SerializableField(0)]
    private BaseHouse _house;

    public MovingCrate(BaseHouse house) : base(0xE3D)
    {
        Hue = 0x8A5;
        Movable = false;

        House = house;
    }

    public override int LabelNumber => 1061690; // Packing Crate

    public override int DefaultMaxItems => 0;
    public override int DefaultMaxWeight => 0;

    public override bool IsDecoContainer => false;

    /*
    public override void AddNameProperties( ObjectPropertyList list )
    {
      base.AddNameProperties( list );

      if (House != null && House.InternalizedVendors.Count > 0)
        list.Add( 1061833, House.InternalizedVendors.Count.ToString() ); // This packing crate contains ~1_COUNT~ vendors/barkeepers.
    }
    */

    public override void DropItem(Item dropped)
    {
        // 1. Try to stack the item
        foreach (var item in Items)
        {
            if (item is PackingBox)
            {
                var subItems = item.Items;

                for (var i = 0; i < subItems.Count; i++)
                {
                    var subItem = subItems[i];

                    if (subItem is not Container && subItem.StackWith(null, dropped, false))
                    {
                        return;
                    }
                }
            }
        }

        // 2. Try to drop the item into an existing container
        foreach (var item in Items)
        {
            if (item is PackingBox packingBox)
            {
                var subItems = packingBox.Items;

                if (subItems.Count < MaxItemsPerSubcontainer)
                {
                    packingBox.DropItem(dropped);
                    return;
                }
            }
        }

        // 3. Drop the item into a new container
        var subContainer = new PackingBox();
        subContainer.DropItem(dropped);

        var location = GetFreeLocation();
        if (location != Point3D.Zero)
        {
            AddItem(subContainer);
            subContainer.Location = location;
        }
        else
        {
            base.DropItem(subContainer);
        }
    }

    private Point3D GetFreeLocation()
    {
        var positions = new bool[Rows, Columns];

        foreach (var item in Items)
        {
            if (item is PackingBox)
            {
                var i = (item.Y - Bounds.Y) / VerticalSpacing;
                if (i < 0)
                {
                    i = 0;
                }
                else if (i >= Rows)
                {
                    i = Rows - 1;
                }

                var j = (item.X - Bounds.X) / HorizontalSpacing;
                if (j < 0)
                {
                    j = 0;
                }
                else if (j >= Columns)
                {
                    j = Columns - 1;
                }

                positions[i, j] = true;
            }
        }

        for (var i = 0; i < Rows; i++)
        {
            for (var j = 0; j < Columns; j++)
            {
                if (!positions[i, j])
                {
                    var x = Bounds.X + j * HorizontalSpacing;
                    var y = Bounds.Y + i * VerticalSpacing;

                    return new Point3D(x, y, 0);
                }
            }
        }

        return Point3D.Zero;
    }

    public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
    {
        if (m.AccessLevel < AccessLevel.GameMaster)
        {
            m.SendLocalizedMessage(1061145); // You cannot place items into a house moving crate.
            return false;
        }

        return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
    }

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject) => House?.Deleted == false &&
        base.CheckLift(from, item, ref reject) && House.IsOwner(from);

    public override bool CheckItemUse(Mobile from, Item item) =>
        House?.Deleted == false && base.CheckItemUse(from, item) && House.IsOwner(from);

    public override void OnItemRemoved(Item item)
    {
        base.OnItemRemoved(item);

        if (TotalItems == 0)
        {
            Delete();
        }
    }

    public void RestartTimer()
    {
        if (_internalizeTimer == null)
        {
            _internalizeTimer = new InternalizeTimer(this);
            _internalizeTimer.Start();
        }
        else
        {
            _internalizeTimer.Stop();
            _internalizeTimer.Start();
        }
    }

    public void Hide()
    {
        if (_internalizeTimer != null)
        {
            _internalizeTimer.Stop();
            _internalizeTimer = null;
        }

        using var queue = EnumerateItems(predicate: item => item is PackingBox && item.Items.Count == 0);
        while (queue.Count > 0)
        {
            queue.Dequeue().Delete();
        }

        if (TotalItems == 0)
        {
            Delete();
        }
        else
        {
            Internalize();
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (House?.MovingCrate == this)
        {
            House.MovingCrate = null;
        }

        _internalizeTimer?.Stop();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (House != null)
        {
            House.MovingCrate = this;
            Timer.StartTimer(Hide);
        }
        else
        {
            Timer.StartTimer(Delete);
        }
    }

    public class InternalizeTimer : Timer
    {
        private readonly MovingCrate m_Crate;

        public InternalizeTimer(MovingCrate crate) : base(TimeSpan.FromMinutes(5.0))
        {
            m_Crate = crate;
        }

        protected override void OnTick()
        {
            m_Crate.Hide();
        }
    }
}

[SerializationGenerator(0)]
public partial class PackingBox : BaseContainer
{
    public PackingBox() : base(0x9A8) => Movable = false;

    public override int LabelNumber => 1061690; // Packing Crate

    public override int DefaultGumpID => 0x4B;
    public override int DefaultDropSound => 0x42;

    public override Rectangle2D Bounds => new(16, 51, 168, 73);

    public override int DefaultMaxItems => 0;
    public override int DefaultMaxWeight => 0;

    public override void SendCantStoreMessage(Mobile to, Item item)
    {
        to.SendLocalizedMessage(1061145); // You cannot place items into a house moving crate.
    }

    public override void OnItemRemoved(Item item)
    {
        base.OnItemRemoved(item);

        if (item.GetBounce() == null && TotalItems == 0)
        {
            Delete();
        }
    }

    public override void OnItemBounceCleared(Item item)
    {
        base.OnItemBounceCleared(item);

        if (TotalItems == 0)
        {
            Delete();
        }
    }
}
