using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class EscortableMage : BaseEscortable
{
    [Constructible]
    public EscortableMage()
    {
        Title = "the mage";

        SetSkill(SkillName.EvalInt, 80.0, 100.0);
        SetSkill(SkillName.Inscribe, 80.0, 100.0);
        SetSkill(SkillName.Magery, 80.0, 100.0);
        SetSkill(SkillName.Meditation, 80.0, 100.0);
        SetSkill(SkillName.MagicResist, 80.0, 100.0);
    }

    public override bool CanTeach => true;
    public override bool ClickTitle => false; // Do not display 'the mage' when single-clicking

    private static int GetRandomHue()
    {
        return Utility.Random(5) switch
        {
            0 => Utility.RandomBlueHue(),
            1 => Utility.RandomGreenHue(),
            2 => Utility.RandomRedHue(),
            3 => Utility.RandomYellowHue(),
            4 => Utility.RandomNeutralHue(),
            _ => Utility.RandomBlueHue()
        };
    }

    public override void InitOutfit()
    {
        AddItem(new Robe(GetRandomHue()));

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

        Utility.AssignRandomHair(this);

        PackGold(200, 250);
    }
}
