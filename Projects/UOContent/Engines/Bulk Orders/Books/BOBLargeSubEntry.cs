using System;
using ModernUO.Serialization;

namespace Server.Engines.BulkOrders;

[SerializationGenerator(0)]
public partial class BOBLargeSubEntry
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

    public BOBLargeSubEntry()
    {
    }

    public BOBLargeSubEntry(LargeBulkEntry lbe)
    {
        _itemType = lbe.Details.Type;
        _amountCur = lbe.Amount;
        _number = lbe.Details.Number;
        _graphic = lbe.Details.Graphic;
    }
}
