using ModernUO.Serialization;

namespace Server.Engines.BulkOrders;

[SerializationGenerator(2)]
public partial class BOBFilter
{
    [SerializableField(0)]
    [SaveFlag(nameof(ShouldSerializeType))]
    private int _type;

    private bool ShouldSerializeType() => _type != 0;

    [SerializableField(1)]
    [SaveFlag(nameof(ShouldSerializeQuality))]
    private int _quality;

    private bool ShouldSerializeQuality() => _quality != 0;

    [SerializableField(2)]
    [SaveFlag(nameof(ShouldSerializeMaterial))]
    private int _material;

    private bool ShouldSerializeMaterial() => _material != 0;

    [SerializableField(3)]
    [SaveFlag(nameof(ShouldSerializeQuantity))]
    private int _quantity;

    private bool ShouldSerializeQuantity() => _quantity != 0;

    private void Deserialize(IGenericReader reader, int version)
    {
        if (version == 1)
        {
            _type = reader.ReadEncodedInt();
            _quality = reader.ReadEncodedInt();
            _material = reader.ReadEncodedInt();
            _quantity = reader.ReadEncodedInt();
        }
    }

    public bool IsDefault => _type == 0 && _quality == 0 && _material == 0 && _quantity == 0;

    public void Clear()
    {
        Type = 0;
        Quality = 0;
        Material = 0;
        Quantity = 0;
    }
}
