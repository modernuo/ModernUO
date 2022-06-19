using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class FarmableTurnip : FarmableCrop
{
    [Constructible]
    public FarmableTurnip() : base(GetCropID())
    {
    }

    public static int GetCropID() => Utility.Random(3169, 3);

    public override Item GetCropObject() =>
        new Turnip
        {
            ItemID = Utility.Random(3385, 2)
        };

    public override int GetPickedID() => 3254;
}
