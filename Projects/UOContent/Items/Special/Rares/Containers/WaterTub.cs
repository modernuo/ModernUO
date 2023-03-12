using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Tub : BaseWaterContainer
{
    private const int _emptyItemId = 0xe83;
    private const int _fullItemId = 0xe7b;

    [Constructible]
    public Tub(bool filled = false) : base(filled ? _fullItemId : _emptyItemId, filled)
    {
    }

    public override int EmptyItemId => _emptyItemId;
    public override int FullItemId => _fullItemId;
    public override int MaxQuantity => 50;
}
