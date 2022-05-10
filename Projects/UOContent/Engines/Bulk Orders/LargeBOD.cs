using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.BulkOrders
{
    [SerializationGenerator(1)]
    public abstract partial class LargeBOD : BaseBOD
    {
        [InvalidateProperties]
        [SerializableField(0)]
        private LargeBulkEntry[] _entries;

        public LargeBOD(
            int hue, int amountMax, bool requireExeptional, BulkMaterialType material, LargeBulkEntry[] entries
        ) : base(hue, amountMax, requireExeptional, material) =>
            _entries = entries;

        public LargeBOD()
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override bool Complete
        {
            get
            {
                for (var i = 0; i < _entries.Length; ++i)
                {
                    if (_entries[i].Amount < AmountMax)
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

            for (var i = 0; i < _entries.Length; ++i)
            {
                list.Add(1060658 + i, "#{0}\t{1}", _entries[i].Details.Number, _entries[i].Amount); // ~1_val~: ~2_val~
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
            if (item is not SmallBOD small)
            {
                from.SendLocalizedMessage(1045159); // That is not a bulk order.
                return;
            }

            LargeBulkEntry entry = null;

            for (var i = 0; i < _entries.Length; ++i)
            {
                if (_entries[i].Details.Type == small.Type)
                {
                    entry = _entries[i];
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

        private void Deserialize(IGenericReader reader, int version)
        {
            _entries = new LargeBulkEntry[reader.ReadInt()];
            for (var i = 0; i < _entries.Length; i++)
            {
                _entries[i] = new LargeBulkEntry(reader, this);
            }
        }
    }
}
