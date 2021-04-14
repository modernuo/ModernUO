using System;
using System.Collections.Generic;
using Server.Multis;

namespace Server.Mobiles
{
    public class VendorInventory
    {
        public static readonly TimeSpan GracePeriod = TimeSpan.FromDays(7.0);

        private readonly Timer m_ExpireTimer;

        public VendorInventory(BaseHouse house, Mobile owner, string vendorName, string shopName)
        {
            House = house;
            Owner = owner;
            VendorName = vendorName;
            ShopName = shopName;

            Items = new List<Item>();

            ExpireTime = Core.Now + GracePeriod;
            m_ExpireTimer = new ExpireTimer(this, GracePeriod);
            m_ExpireTimer.Start();
        }

        public VendorInventory(BaseHouse house, IGenericReader reader)
        {
            House = house;

            var version = reader.ReadEncodedInt();

            Owner = reader.ReadEntity<Mobile>();
            VendorName = reader.ReadString();
            ShopName = reader.ReadString();

            Items = reader.ReadEntityList<Item>();
            Gold = reader.ReadInt();

            ExpireTime = reader.ReadDeltaTime();

            if (Items.Count == 0 && Gold == 0)
            {
                Timer.DelayCall(Delete);
            }
            else
            {
                var delay = ExpireTime - Core.Now;
                m_ExpireTimer = new ExpireTimer(this, delay > TimeSpan.Zero ? delay : TimeSpan.Zero);
                m_ExpireTimer.Start();
            }
        }

        public BaseHouse House { get; set; }

        public string VendorName { get; set; }

        public string ShopName { get; set; }

        public Mobile Owner { get; set; }

        public List<Item> Items { get; }

        public int Gold { get; set; }

        public DateTime ExpireTime { get; }

        public void AddItem(Item item)
        {
            item.Internalize();
            Items.Add(item);
        }

        public void Delete()
        {
            foreach (var item in Items)
            {
                item.Delete();
            }

            Items.Clear();
            Gold = 0;

            House?.VendorInventories.Remove(this);

            m_ExpireTimer.Stop();
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(Owner);
            writer.Write(VendorName);
            writer.Write(ShopName);

            Items.Tidy();
            writer.Write(Items);
            writer.Write(Gold);

            writer.WriteDeltaTime(ExpireTime);
        }

        private class ExpireTimer : Timer
        {
            private readonly VendorInventory m_Inventory;

            public ExpireTimer(VendorInventory inventory, TimeSpan delay) : base(delay)
            {
                m_Inventory = inventory;

                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                var house = m_Inventory.House;

                if (house != null)
                {
                    if (m_Inventory.Gold > 0)
                    {
                        house.MovingCrate ??= new MovingCrate(house);

                        Banker.Deposit(house.MovingCrate, m_Inventory.Gold);
                    }

                    foreach (var item in m_Inventory.Items)
                    {
                        if (!item.Deleted)
                        {
                            house.DropToMovingCrate(item);
                        }
                    }

                    m_Inventory.Gold = 0;
                    m_Inventory.Items.Clear();
                }

                m_Inventory.Delete();
            }
        }
    }
}
