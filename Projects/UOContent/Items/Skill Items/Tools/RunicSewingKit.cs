using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RunicSewingKit : BaseRunicTool
{
    [Constructible]
    public RunicSewingKit(CraftResource resource) : base(resource, 0xF9D)
    {
        Weight = 2.0;
        Hue = CraftResources.GetHue(resource);
    }

    [Constructible]
    public RunicSewingKit(CraftResource resource, int uses) : base(resource, uses, 0xF9D)
    {
        Weight = 2.0;
        Hue = CraftResources.GetHue(resource);
    }

    public override CraftSystem CraftSystem => DefTailoring.CraftSystem;

    public override void AddNameProperty(IPropertyList list)
    {
        if (CraftResources.IsStandard(Resource))
        {
            list.Add(1061119, " "); // ~1_LEATHER_TYPE~ runic sewing kit
            return;
        }

        var num = CraftResources.GetLocalizationNumber(Resource);

        if (num > 0)
        {
            // ~1_LEATHER_TYPE~ runic sewing kit
            list.AddLocalized(1061119, num);
        }
        else
        {
            // ~1_LEATHER_TYPE~ runic sewing kit
            list.Add(1061119, CraftResources.GetName(Resource));
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        if (CraftResources.IsStandard(Resource))
        {
            LabelTo(from, 1061119, " "); // ~1_LEATHER_TYPE~ runic sewing kit
            return;
        }

        var num = CraftResources.GetLocalizationNumber(Resource);
        if (num > 0)
        {
            LabelTo(from, 1061119, $"#{num}"); // ~1_LEATHER_TYPE~ runic sewing kit
        }
        else
        {
            LabelTo(from, 1061119, CraftResources.GetName(Resource)); // ~1_LEATHER_TYPE~ runic sewing kit
        }
    }
}
