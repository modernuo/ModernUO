using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class FarmableCabbage : FarmableCrop
{
    [Constructible]
    public FarmableCabbage() : base(GetCropID())
    {
    }

    public static int GetCropID() => 3254;

    public override Item GetCropObject() =>
        new Cabbage
        {
            ItemID = Utility.Random(3195, 2)
        };

    public override int GetPickedID() => 3254;
}
