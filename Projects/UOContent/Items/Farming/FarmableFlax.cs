using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class FarmableFlax : FarmableCrop
{
    [Constructible]
    public FarmableFlax() : base(GetCropID())
    {
    }

    public static int GetCropID() => Utility.Random(6809, 3);

    public override Item GetCropObject() =>
        new Flax
        {
            ItemID = Utility.Random(6812, 2)
        };

    public override int GetPickedID() => 3254;
}
