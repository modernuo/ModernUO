using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class BrideGroom : BaseEscortable
{
    [Constructible]
    public BrideGroom()
    {
        if (Female)
        {
            Title = "the bride";
        }
        else
        {
            Title = "the groom";
        }
    }

    public override bool CanTeach => true;
    public override bool ClickTitle => false; // Do not display 'the groom' when single-clicking

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
            AddItem(new FancyDress());
        }
        else
        {
            AddItem(new FancyShirt());
        }

        var lowHue = GetRandomHue();

        AddItem(new LongPants(lowHue));

        if (Female)
        {
            AddItem(new Shoes(lowHue));
        }
        else
        {
            AddItem(new Boots(lowHue));
        }

        if (Utility.RandomBool())
        {
            HairItemID = 0x203B;
        }
        else
        {
            HairItemID = 0x203C;
        }

        HairHue = Race.RandomHairHue();

        PackGold(200, 250);
    }
}
