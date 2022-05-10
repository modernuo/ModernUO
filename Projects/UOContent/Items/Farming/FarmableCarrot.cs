using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class FarmableCarrot : FarmableCrop
{
    [Constructible]
    public FarmableCarrot() : base(GetCropID())
    {
    }

    public static int GetCropID() => 3190;

    public override Item GetCropObject() =>
        new Carrot
        {
            ItemID = Utility.Random(3191, 2)
        };

    public override int GetPickedID() => 3254;
}
