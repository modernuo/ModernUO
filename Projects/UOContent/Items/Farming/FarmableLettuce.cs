using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class FarmableLettuce : FarmableCrop
{
    [Constructible]
    public FarmableLettuce() : base(GetCropID())
    {
    }

    public static int GetCropID() => 3254;

    public override Item GetCropObject() =>
        new Lettuce
        {
            ItemID = Utility.Random(3184, 2)
        };

    public override int GetPickedID() => 3254;
}
