using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WaterBarrel : BaseWaterContainer
{
    private const int _emptyItemId = 0xe77;
    private const int _fullItemId = 0x154d;

    [Constructible]
    public WaterBarrel(bool filled = false) : base(filled ? _fullItemId : _emptyItemId, filled)
    {
    }

    public override int LabelNumber => 1025453; /* water barrel */

    public override int EmptyItemId => _emptyItemId;
    public override int FullItemId => _fullItemId;
    public override int MaxQuantity => 100;
}
