using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class FarmablePumpkin : FarmableCrop
{
    [Constructible]
    public FarmablePumpkin()
        : base(GetCropID())
    {
    }

    public static int GetCropID() => Utility.Random(3166, 3);

    public override Item GetCropObject() =>
        new Pumpkin
        {
            ItemID = Utility.Random(3178, 3)
        };

    public override int GetPickedID() => Utility.Random(3166, 3);
}
