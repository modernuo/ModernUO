using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Peasant : BaseEscortable
{
    [Constructible]
    public Peasant() => Title = "the peasant";

    public override bool CanTeach => true;
    public override bool ClickTitle => false; // Do not display 'the peasant' when single-clicking

    private static int GetRandomHue()
    {
        return Utility.Random(6) switch
        {
            0 => 0,
            1 => Utility.RandomBlueHue(),
            2 => Utility.RandomGreenHue(),
            3 => Utility.RandomRedHue(),
            4 => Utility.RandomYellowHue(),
            5 => Utility.RandomNeutralHue(),
            _ => 0
        };
    }

    public override void InitOutfit()
    {
        if (Female)
        {
            AddItem(new PlainDress());
        }
        else
        {
            AddItem(new Shirt(GetRandomHue()));
        }

        var lowHue = GetRandomHue();

        AddItem(new ShortPants(lowHue));

        if (Female)
        {
            AddItem(new Boots(lowHue));
        }
        else
        {
            AddItem(new Shoes(lowHue));
        }

        // if (!Female)
        // AddItem( new BodySash( lowHue ) );

        // AddItem( new Cloak( GetRandomHue() ) );

        // if (!Female)
        // AddItem( new Longsword() );

        Utility.AssignRandomHair(this);

        PackGold(200, 250);
    }
}
