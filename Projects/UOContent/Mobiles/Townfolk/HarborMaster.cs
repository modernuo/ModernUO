using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class HarborMaster : BaseCreature
{
    [Constructible]
    public HarborMaster() : base(AIType.AI_Animal, FightMode.None)
    {
        InitStats(31, 41, 51);
        SetSkill(SkillName.Mining, 36, 68);

        SetSpeed(0.2, 0.4);
        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();
        Blessed = true;

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");
            Title = "the Harbor Mistress";
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");
            Title = "the Harbor Master";
        }

        AddItem(new Shirt(Utility.RandomDyedHue()));
        AddItem(new Boots());
        AddItem(new LongPants(Utility.RandomNeutralHue()));
        AddItem(new QuarterStaff());

        Utility.AssignRandomHair(this);

        Container pack = new Backpack();

        pack.DropItem(new Gold(250, 300));

        pack.Movable = false;

        AddItem(pack);
    }

    public override bool CanTeach => false;

    public override bool ClickTitle => false;
}
