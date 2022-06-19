using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class FarmableCotton : FarmableCrop
{
    [Constructible]
    public FarmableCotton() : base(GetCropID())
    {
    }

    public static int GetCropID() => Utility.Random(3153, 4);

    public override Item GetCropObject() => new Cotton();

    public override int GetPickedID() => 3254;
}
