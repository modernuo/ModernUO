namespace Server.Items
{
    public abstract class BaseWaterContainer : Container, IHasQuantity
    {
        private int m_Quantity;

        public BaseWaterContainer(int item_Id, bool filled)
            : base(item_Id) =>
            m_Quantity = filled ? MaxQuantity : 0;

        public BaseWaterContainer(Serial serial)
            : base(serial)
        {
        }

        public abstract int voidItem_ID { get; }
        public abstract int fullItem_ID { get; }
        public abstract int MaxQuantity { get; }

        public override int DefaultGumpID => 0x3e;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool IsEmpty => m_Quantity <= 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool IsFull => m_Quantity >= MaxQuantity;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Quantity
        {
            get => m_Quantity;
            set
            {
                if (value != m_Quantity)
                {
                    m_Quantity = value < 1 ? 0 :
                        value > MaxQuantity ? MaxQuantity : value;

                    Movable = !IsLockedDown ? IsEmpty : false;

                    ItemID = IsEmpty ? voidItem_ID : fullItem_ID;

                    if (!IsEmpty)
                    {
                        var rootParent = RootParent;

                        if (rootParent?.Map != null && rootParent.Map != Map.Internal)
                        {
                            MoveToWorld(rootParent.Location, rootParent.Map);
                        }
                    }

                    InvalidateProperties();
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsEmpty)
            {
                base.OnDoubleClick(from);
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (IsEmpty)
            {
                base.OnSingleClick(from);
            }
            else
            {
                if (Name == null)
                {
                    LabelTo(from, LabelNumber);
                }
                else
                {
                    LabelTo(from, Name);
                }
            }
        }

        public override void OnAosSingleClick(Mobile from)
        {
            if (IsEmpty)
            {
                base.OnAosSingleClick(from);
            }
            else
            {
                if (Name == null)
                {
                    LabelTo(from, LabelNumber);
                }
                else
                {
                    LabelTo(from, Name);
                }
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            if (IsEmpty)
            {
                base.GetProperties(list);
            }
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (!IsEmpty)
            {
                return false;
            }

            return base.OnDragDropInto(from, item, p);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
            writer.Write(m_Quantity);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            m_Quantity = reader.ReadInt();
        }
    }
}
