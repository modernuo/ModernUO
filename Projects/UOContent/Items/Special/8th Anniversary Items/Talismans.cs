using System;
using Server.Mobiles;
using Server.Spells.Ninjitsu;

namespace Server.Items
{
    public enum TalismanForm
    {
        Ferret = 1031672,
        Squirrel = 1031671,
        CuSidhe = 1031670,
        Reptalon = 1075202
    }

    public class BaseFormTalisman : Item
    {
        public BaseFormTalisman() : base(0x2F59)
        {
            LootType = LootType.Blessed;
            Layer = Layer.Talisman;
            Weight = 1.0;
        }

        public BaseFormTalisman(Serial serial) : base(serial)
        {
        }

        public virtual TalismanForm Form => TalismanForm.Squirrel;

        public override void AddNameProperty(IPropertyList list)
        {
            list.Add(1075200, $"{(int)Form:#}");
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }

        public override void OnRemoved(IEntity parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile m)
            {
                AnimalForm.RemoveContext(m, true);
            }
        }

        public static bool EntryEnabled(Mobile m, Type type)
        {
            if (type == typeof(Squirrel))
            {
                return m.Talisman is SquirrelFormTalisman;
            }

            if (type == typeof(Ferret))
            {
                return m.Talisman is FerretFormTalisman;
            }

            if (type == typeof(CuSidhe))
            {
                return m.Talisman is CuSidheFormTalisman;
            }

            if (type == typeof(Reptalon))
            {
                return m.Talisman is ReptalonFormTalisman;
            }

            return true;
        }
    }

    public class FerretFormTalisman : BaseFormTalisman
    {
        [Constructible]
        public FerretFormTalisman()
        {
        }

        public FerretFormTalisman(Serial serial) : base(serial)
        {
        }

        public override TalismanForm Form => TalismanForm.Ferret;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class SquirrelFormTalisman : BaseFormTalisman
    {
        [Constructible]
        public SquirrelFormTalisman()
        {
        }

        public SquirrelFormTalisman(Serial serial) : base(serial)
        {
        }

        public override TalismanForm Form => TalismanForm.Squirrel;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class CuSidheFormTalisman : BaseFormTalisman
    {
        [Constructible]
        public CuSidheFormTalisman()
        {
        }

        public CuSidheFormTalisman(Serial serial) : base(serial)
        {
        }

        public override TalismanForm Form => TalismanForm.CuSidhe;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class ReptalonFormTalisman : BaseFormTalisman
    {
        [Constructible]
        public ReptalonFormTalisman()
        {
        }

        public ReptalonFormTalisman(Serial serial) : base(serial)
        {
        }

        public override TalismanForm Form => TalismanForm.Reptalon;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
