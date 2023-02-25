using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[Flippable(0x13E4, 0x13E3)]
[SerializationGenerator(0, false)]
public partial class RunicHammer : BaseRunicTool
{
    [Constructible]
    public RunicHammer(CraftResource resource) : base(resource, 0x13E3)
    {
        Weight = 8.0;
        Layer = Layer.OneHanded;
        Hue = CraftResources.GetHue(resource);
    }

    [Constructible]
    public RunicHammer(CraftResource resource, int uses) : base(resource, uses, 0x13E3)
    {
        Weight = 8.0;
        Layer = Layer.OneHanded;
        Hue = CraftResources.GetHue(resource);
    }

    public override CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;

    public override int LabelNumber
    {
        get
        {
            var index = CraftResources.GetIndex(Resource);

            if (index >= 1 && index <= 8)
            {
                return 1049019 + index;
            }

            return 1045128; // runic smithy hammer
        }
    }

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        var index = CraftResources.GetIndex(Resource);

        if (index >= 1 && index <= 8)
        {
            return;
        }

        if (!CraftResources.IsStandard(Resource))
        {
            var num = CraftResources.GetLocalizationNumber(Resource);

            if (num > 0)
            {
                list.Add(num);
            }
            else
            {
                list.Add(CraftResources.GetName(Resource));
            }
        }
    }
}
