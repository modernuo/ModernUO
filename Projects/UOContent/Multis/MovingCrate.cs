using System;
using System.Collections.Generic;
using Server.Items;
using Server.Network;

namespace Server.Multis
{
    public class MovingCrate : Container
    {
        public static readonly int MaxItemsPerSubcontainer = 20;
        public static readonly int Rows = 3;
        public static readonly int Columns = 5;
        public static readonly int HorizontalSpacing = 25;
        public static readonly int VerticalSpacing = 25;

        private Timer m_InternalizeTimer;

        public MovingCrate(BaseHouse house) : base(0xE3D)
        {
            Hue = 0x8A5;
            Movable = false;

            House = house;
        }

        public MovingCrate(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061690; // Packing Crate

        public BaseHouse House { get; set; }

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

                        if (!(subItem is Container) && subItem.StackWith(null, dropped, false))
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
                    Container box = packingBox;
                    var subItems = box.Items;

                    if (subItems.Count < MaxItemsPerSubcontainer)
                    {
                        box.DropItem(dropped);
                        return;
                    }
                }
            }

            // 3. Drop the item into a new container
            Container subContainer = new PackingBox();
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
            if (m_InternalizeTimer == null)
            {
                m_InternalizeTimer = new InternalizeTimer(this);
                m_InternalizeTimer.Start();
            }
            else
            {
                m_InternalizeTimer.Stop();
                m_InternalizeTimer.Start();
            }
        }

        public void Hide()
        {
            if (m_InternalizeTimer != null)
            {
                m_InternalizeTimer.Stop();
                m_InternalizeTimer = null;
            }

            var toRemove = new List<Item>();
            foreach (var item in Items)
            {
                if (item is PackingBox && item.Items.Count == 0)
                {
                    toRemove.Add(item);
                }
            }

            foreach (var item in toRemove)
            {
                item.Delete();
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

            m_InternalizeTimer?.Stop();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1);

            writer.Write(House);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            House = reader.ReadEntity<BaseHouse>();

            if (House != null)
            {
                House.MovingCrate = this;
                Timer.DelayCall(Hide);
            }
            else
            {
                Timer.DelayCall(Delete);
            }

            if (version == 0)
            {
                MaxItems = -1; // reset to default
            }
        }

        public class InternalizeTimer : Timer
        {
            private readonly MovingCrate m_Crate;

            public InternalizeTimer(MovingCrate crate) : base(TimeSpan.FromMinutes(5.0))
            {
                m_Crate = crate;

                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                m_Crate.Hide();
            }
        }
    }

    public class PackingBox : BaseContainer
    {
        public PackingBox() : base(0x9A8) => Movable = false;

        public PackingBox(Serial serial) : base(serial)
        {
        }

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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            if (version == 0)
            {
                MaxItems = -1; // reset to default
            }
        }
    }
}
