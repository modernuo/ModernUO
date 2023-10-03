using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LocalizedSign : Sign
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _number;

    [Constructible]
    public LocalizedSign(SignType type, SignFacing facing, int labelNumber) :
        base(0xB95 + 2 * (int)type + (int)facing) => _number = labelNumber;

    [Constructible]
    public LocalizedSign(int itemID, int labelNumber) : base(itemID) => _number = labelNumber;

    public override int LabelNumber => _number;
}
