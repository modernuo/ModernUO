using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class FarmableOnion : FarmableCrop
{
    [Constructible]
    public FarmableOnion() : base(GetCropID())
    {
    }

    public static int GetCropID() => 3183;

    public override Item GetCropObject() =>
        new Onion
        {
            ItemID = Utility.Random(3181, 2)
        };

    public override int GetPickedID() => 3254;
}
