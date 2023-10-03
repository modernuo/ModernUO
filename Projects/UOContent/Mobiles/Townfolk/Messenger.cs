using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Messenger : BaseEscortable
{
    [Constructible]
    public Messenger() => Title = "the messenger";

    public override bool CanTeach => true;
    public override bool ClickTitle => false; // Do not display 'the messenger' when single-clicking

    private static int GetRandomHue()
    {
        return Utility.Random(6) switch
        {
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

        var randomHair = Utility.Random(4);
        HairItemID = randomHair == 4 ? 0x203B : 0x2048 + randomHair;

        HairHue = Race.RandomHairHue();

        PackGold(200, 250);
    }
}
