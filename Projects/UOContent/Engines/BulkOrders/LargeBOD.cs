using Server.Mobiles;

namespace Server.Engines.BulkOrders
{
    public abstract class LargeBOD : BaseBOD
    {
        private LargeBulkEntry[] m_Entries;

        public LargeBOD(
            int hue, int amountMax, bool requireExeptional, BulkMaterialType material, LargeBulkEntry[] entries
        ) : base(hue, amountMax, requireExeptional, material) =>
            m_Entries = entries;

        public LargeBOD()
        {
        }

        public LargeBOD(Serial serial) : base(serial)
        {
        }

        public LargeBulkEntry[] Entries
        {
            get => m_Entries;
            set
            {
                m_Entries = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override bool Complete
        {
            get
            {
                for (var i = 0; i < m_Entries.Length; ++i)
                {
                    if (m_Entries[i].Amount < AmountMax)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override int LabelNumber => 1045151; // a bulk order deed

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060655); // large bulk order

            if (RequireExceptional)
            {
                list.Add(1045141); // All items must be exceptional.
            }

            if (Material != BulkMaterialType.None)
            {
                list.Add(LargeBODGump.GetMaterialNumberFor(Material)); // All items must be made with x material.
            }

            list.Add(1060656, AmountMax.ToString()); // amount to make: ~1_val~

            for (var i = 0; i < m_Entries.Length; ++i)
            {
                list.Add(1060658 + i, "#{0}\t{1}", m_Entries[i].Details.Number, m_Entries[i].Amount); // ~1_val~: ~2_val~
            }
        }

        public override void OnDoubleClickNotAccessible(Mobile from)
        {
            OnDoubleClick(from);
        }

        public override void OnDoubleClickSecureTrade(Mobile from)
        {
            OnDoubleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack) || InSecureTrade || RootParent is PlayerVendor)
            {
                from.SendGump(new LargeBODGump(from, this));
            }
            else
            {
                from.SendLocalizedMessage(1045156); // You must have the deed in your backpack to use it.
            }
        }

        public override void EndCombine(Mobile from, Item item)
        {
            if (!(item is SmallBOD small))
            {
                from.SendLocalizedMessage(1045159); // That is not a bulk order.
                return;
            }

            LargeBulkEntry entry = null;

            for (var i = 0; i < m_Entries.Length; ++i)
            {
                if (m_Entries[i].Details.Type == small.Type)
                {
                    entry = m_Entries[i];
                    break;
                }
            }

            if (entry == null)
            {
                from.SendLocalizedMessage(1045160); // That is not a bulk order for this large request.
            }
            else if (RequireExceptional && !small.RequireExceptional)
            {
                from.SendLocalizedMessage(1045161); // Both orders must be of exceptional quality.
            }
            else if (Material >= BulkMaterialType.DullCopper && Material <= BulkMaterialType.Valorite &&
                     small.Material != Material)
            {
                from.SendLocalizedMessage(1045162); // Both orders must use the same ore type.
            }
            else if (Material >= BulkMaterialType.Spined && Material <= BulkMaterialType.Barbed &&
                     small.Material != Material)
            {
                from.SendLocalizedMessage(1049351); // Both orders must use the same leather type.
            }
            else if (AmountMax != small.AmountMax)
            {
                // The two orders have different requested amounts and cannot be combined.
                from.SendLocalizedMessage(1045163);
            }
            else if (small.AmountCur < small.AmountMax)
            {
                from.SendLocalizedMessage(1045164); // The order to combine with is not completed.
            }
            else if (entry.Amount >= AmountMax)
            {
                // The maximum amount of requested items have already been combined to this deed.
                from.SendLocalizedMessage(1045166);
            }
            else
            {
                entry.Amount += small.AmountCur;
                small.Delete();

                from.SendLocalizedMessage(1045165); // The orders have been combined.
                from.SendGump(new LargeBODGump(from, this));

                if (!Complete)
                {
                    BeginCombine(from);
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Entries.Length);

            for (var i = 0; i < m_Entries.Length; ++i)
            {
                m_Entries[i].Serialize(writer);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        m_Entries = new LargeBulkEntry[reader.ReadInt()];

                        for (var i = 0; i < m_Entries.Length; ++i)
                        {
                            m_Entries[i] = new LargeBulkEntry(this, reader);
                        }

                        break;
                    }
            }
        }
    }
}
