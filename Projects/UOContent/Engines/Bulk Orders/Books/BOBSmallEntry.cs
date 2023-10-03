using System;
using ModernUO.Serialization;

namespace Server.Engines.BulkOrders;

[SerializationGenerator(1)]
public partial class BOBSmallEntry : BaseBOBEntry
{
    [SerializableField(0, setter: "private")]
    private Type _itemType;

    [EncodedInt]
    [SerializableField(1, setter: "private")]
    private int _amountCur;

    [EncodedInt]
    [SerializableField(2, setter: "private")]
    private int _number;

    [EncodedInt]
    [SerializableField(3, setter: "private")]
    private int _graphic;

    public BOBSmallEntry(SmallBOD bod)
    {
        _itemType = bod.Type;
        RequireExceptional = bod.RequireExceptional;

        if (bod is SmallTailorBOD)
        {
            DeedType = BODType.Tailor;
        }
        else if (bod is SmallSmithBOD)
        {
            DeedType = BODType.Smith;
        }

        Material = bod.Material;
        _amountCur = bod.AmountCur;
        AmountMax = bod.AmountMax;
        _number = bod.Number;
        _graphic = bod.Graphic;
    }

    public override Item Reconstruct()
    {
        SmallBOD bod = null;

        if (DeedType == BODType.Smith)
        {
            bod = new SmallSmithBOD(AmountCur, AmountMax, ItemType, Number, Graphic, RequireExceptional, Material);
        }
        else if (DeedType == BODType.Tailor)
        {
            bod = new SmallTailorBOD(AmountCur, AmountMax, ItemType, Number, Graphic, RequireExceptional, Material);
        }

        return bod;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _itemType = reader.ReadType();

        RequireExceptional = reader.ReadBool();

        DeedType = (BODType)reader.ReadEncodedInt();

        Material = (BulkMaterialType)reader.ReadEncodedInt();
        AmountCur = reader.ReadEncodedInt();
        AmountMax = reader.ReadEncodedInt();
        Number = reader.ReadEncodedInt();
        Graphic = reader.ReadEncodedInt();
        Price = reader.ReadEncodedInt();
    }
}
