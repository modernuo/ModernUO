using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class FarmableWheat : FarmableCrop
{
    [Constructible]
    public FarmableWheat() : base(GetCropID())
    {
    }

    public static int GetCropID() => Utility.Random(3157, 4);

    public override Item GetCropObject() => new WheatSheaf();

    public override int GetPickedID() => Utility.Random(3502, 2);
}
