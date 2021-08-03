using System;
using System.Collections.Generic;
using Server.Items;
using Server.Utilities;

namespace Server.Mobiles
{
    public class GenericBuyInfo : IBuyItemInfo
    {
        private int m_Amount;
        private IEntity m_DisplayEntity;

        private int m_Price;

        public GenericBuyInfo(Type type, int price, int amount, int itemID, int hue, object[] args = null) : this(
            null,
            type,
            price,
            amount,
            itemID,
            hue,
            args
        )
        {
        }

        public GenericBuyInfo(string name, Type type, int price, int amount, int itemID, int hue, object[] args = null)
        {
            Type = type;
            m_Price = price;
            MaxAmount = m_Amount = amount;
            ItemID = itemID;
            Hue = hue;
            Args = args;

            Name = name ?? (itemID < 0x4000 ? (1020000 + itemID).ToString() : (1078872 + itemID).ToString());
        }

        public virtual bool CanCacheDisplay => false;

        public Type Type { get; set; }

        public int DefaultPrice { get; private set; }

        public object[] Args { get; set; }

        public virtual int ControlSlots => 0;

        public string Name { get; set; }

        public int PriceScalar
        {
            get => DefaultPrice;
            set => DefaultPrice = value;
        }

        public int Price
        {
            get
            {
                if (DefaultPrice != 0)
                {
                    if (m_Price > 5000000)
                    {
                        long price = m_Price;

                        price *= DefaultPrice;
                        price += 50;
                        price /= 100;

                        if (price > int.MaxValue)
                        {
                            price = int.MaxValue;
                        }

                        return (int)price;
                    }

                    return (m_Price * DefaultPrice + 50) / 100;
                }

                return m_Price;
            }
            set => m_Price = value;
        }

        public int ItemID { get; set; }

        public int Hue { get; set; }

        public int Amount
        {
            get => m_Amount;
            set => m_Amount = Math.Max(value, 0);
        }

        public int MaxAmount { get; set; }

        // get a new instance of an object (we just bought it)
        public virtual IEntity GetEntity() => Type.CreateInstance<IEntity>(Args);

        // Attempt to restock with item, (return true if restock successful)
        public bool Restock(Item item, int amount) => false;

        public void OnRestock()
        {
            if (m_Amount <= 0)
            {
                MaxAmount = Math.Min(999, MaxAmount * 2);
            }
            else
            {
                /* NOTE: According to UO.com, the quantity is halved if the item does not reach 0
                 * Here we implement differently: the quantity is halved only if less than half
                 * of the maximum quantity was bought. That is, if more than half is sold, then
                 * there's clearly a demand and we should not cut down on the stock.
                 */

                var halfQuantity = MaxAmount;

                if (halfQuantity >= 999)
                {
                    halfQuantity = 640;
                }
                else if (halfQuantity > 20)
                {
                    halfQuantity /= 2;
                }

                if (m_Amount >= halfQuantity)
                {
                    MaxAmount = halfQuantity;
                }
            }

            m_Amount = MaxAmount;
        }

        public void DeleteDisplayEntity()
        {
            m_DisplayEntity?.Delete();
            m_DisplayEntity = null;
        }

        public IEntity GetDisplayEntity()
        {
            if (m_DisplayEntity?.Deleted == false)
            {
                return m_DisplayEntity;
            }

            var canCache = CanCacheDisplay;

            if (canCache)
            {
                m_DisplayEntity = DisplayCache.Cache.Lookup(Type);
            }

            if (m_DisplayEntity?.Deleted != false)
            {
                m_DisplayEntity = GetEntity();
                DisplayCache.Cache.Store(Type, m_DisplayEntity, canCache);
            }

            return m_DisplayEntity;
        }

        private class DisplayCache : Container
        {
            private static DisplayCache m_Cache;
            private List<Mobile> m_Mobiles;

            private Dictionary<Type, IEntity> m_Table;

            private DisplayCache() : base(0)
            {
                m_Table = new Dictionary<Type, IEntity>();
                m_Mobiles = new List<Mobile>();
            }

            public DisplayCache(Serial serial) : base(serial)
            {
            }

            public static DisplayCache Cache
            {
                get
                {
                    if (m_Cache?.Deleted != false)
                    {
                        m_Cache = new DisplayCache();
                    }

                    return m_Cache;
                }
            }

            public IEntity Lookup(Type key)
            {
                m_Table.TryGetValue(key, out var e);
                return e;
            }

            public void Store(Type key, IEntity obj, bool cache)
            {
                if (cache)
                {
                    m_Table[key] = obj;
                }

                if (obj is Item item)
                {
                    AddItem(item);
                }
                else if (obj is Mobile mobile)
                {
                    m_Mobiles.Add(mobile);
                }
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                for (var i = 0; i < m_Mobiles.Count; ++i)
                {
                    m_Mobiles[i].Delete();
                }

                m_Mobiles.Clear();

                for (var i = Items.Count - 1; i >= 0; --i)
                {
                    if (i < Items.Count)
                    {
                        Items[i].Delete();
                    }
                }

                if (m_Cache == this)
                {
                    m_Cache = null;
                }
            }

            public override void Serialize(IGenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write(0); // version

                writer.Write(m_Mobiles);
            }

            public override void Deserialize(IGenericReader reader)
            {
                base.Deserialize(reader);

                var version = reader.ReadInt();

                List<IEntity> entities = new (Items);
                entities.AddRange(reader.ReadEntityList<Mobile>());

                m_Mobiles = new List<Mobile>(); // This cannot be null in case it is referenced before disposing

                Timer.StartTimer(() =>
                    {
                        foreach (var entity in entities)
                        {
                            entity.Delete();
                        }
                    }
                );

                m_Table = new Dictionary<Type, IEntity>();

                if (m_Cache == null)
                {
                    m_Cache = this;
                }
                else
                {
                    Delete();
                }
            }
        }
    }
}
