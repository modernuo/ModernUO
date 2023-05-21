using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MelisandesFermentedWine : GreaterExplosionPotion
{
    [Constructible]
    public MelisandesFermentedWine()
    {
        Stackable = false;
        ItemID = 0x99B;

        // TODO update
        Hue = Utility.Random(3) switch
        {
            1 => 0xF,
            2 => 0x48D,
            _ => 0xB,
        };
    }

    public override int LabelNumber => 1072114; // Melisande's Fermented Wine

    public override void Drink(Mobile from)
    {
        if (MondainsLegacy.CheckML(from))
        {
            base.Drink(from);
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1074502); // It looks explosive.
        list.Add(1075085); // Requirement: Mondain's Legacy
    }
}
