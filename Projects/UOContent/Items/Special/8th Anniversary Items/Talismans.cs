using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Spells.Ninjitsu;

namespace Server.Items;

public enum TalismanForm
{
    Ferret = 1031672,
    Squirrel = 1031671,
    CuSidhe = 1031670,
    Reptalon = 1075202
}

[SerializationGenerator(0)]
public partial class BaseFormTalisman : Item
{
    public BaseFormTalisman() : base(0x2F59)
    {
        LootType = LootType.Blessed;
        Layer = Layer.Talisman;
        Weight = 1.0;
    }

    public virtual TalismanForm Form => TalismanForm.Squirrel;

    public override void AddNameProperty(IPropertyList list)
    {
        list.Add(1075200, $"{(int)Form:#}");
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

[SerializationGenerator(0)]
public partial class FerretFormTalisman : BaseFormTalisman
{
    [Constructible]
    public FerretFormTalisman()
    {
    }

    public override TalismanForm Form => TalismanForm.Ferret;
}

[SerializationGenerator(0)]
public partial class SquirrelFormTalisman : BaseFormTalisman
{
    [Constructible]
    public SquirrelFormTalisman()
    {
    }

    public override TalismanForm Form => TalismanForm.Squirrel;
}

[SerializationGenerator(0)]
public partial class CuSidheFormTalisman : BaseFormTalisman
{
    [Constructible]
    public CuSidheFormTalisman()
    {
    }

    public override TalismanForm Form => TalismanForm.CuSidhe;

}

[SerializationGenerator(0)]
public partial class ReptalonFormTalisman : BaseFormTalisman
{
    [Constructible]
    public ReptalonFormTalisman()
    {
    }

    public override TalismanForm Form => TalismanForm.Reptalon;
}
