using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Bucket : BaseWaterContainer
{
    private const int _emptyItemId = 0x14e0;
    private const int _fullItemId = 0x2004;

    [Constructible]
    public Bucket(bool filled = false) : base(filled ? _fullItemId : _emptyItemId, filled)
    {
    }

    public override int EmptyItemId => _emptyItemId;
    public override int FullItemId => _fullItemId;
    public override int MaxQuantity => 25;
}
