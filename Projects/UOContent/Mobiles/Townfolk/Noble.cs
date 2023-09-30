using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Noble : BaseEscortable
{
    [Constructible]
    public Noble()
    {
        Title = "the noble";

        SetSkill(SkillName.Parry, 80.0, 100.0);
        SetSkill(SkillName.Swords, 80.0, 100.0);
        SetSkill(SkillName.Tactics, 80.0, 100.0);
    }

    public override bool CanTeach => true;
    public override bool ClickTitle => false; // Do not display 'the noble' when single-clicking

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
            AddItem(new FancyDress());
        }
        else
        {
            AddItem(new FancyShirt(GetRandomHue()));
        }

        var lowHue = GetRandomHue();

        AddItem(new ShortPants(lowHue));

        if (Female)
        {
            AddItem(new ThighBoots(lowHue));
        }
        else
        {
            AddItem(new Boots(lowHue));
        }

        if (!Female)
        {
            AddItem(new BodySash(lowHue));
        }

        AddItem(new Cloak(GetRandomHue()));

        if (!Female)
        {
            AddItem(new Longsword());
        }

        Utility.AssignRandomHair(this);

        PackGold(200, 250);
    }
}
