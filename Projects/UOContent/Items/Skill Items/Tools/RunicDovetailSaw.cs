using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RunicDovetailSaw : BaseRunicTool
{
    [Constructible]
    public RunicDovetailSaw(CraftResource resource) : base(resource, 0x1028)
    {
        Weight = 2.0;
        Hue = CraftResources.GetHue(resource);
    }

    [Constructible]
    public RunicDovetailSaw(CraftResource resource, int uses) : base(resource, uses, 0x1028)
    {
        Weight = 2.0;
        Hue = CraftResources.GetHue(resource);
    }

    public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;

    public override int LabelNumber
    {
        get
        {
            var index = CraftResources.GetIndex(Resource);

            if (index >= 1 && index <= 6)
            {
                return 1072633 + index;
            }

            return 1024137; // dovetail saw
        }
    }
}
