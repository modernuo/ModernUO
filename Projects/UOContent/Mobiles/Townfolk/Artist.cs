using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Artist : BaseCreature
{
    [Constructible]
    public Artist() : base(AIType.AI_Animal, FightMode.None)
    {
        InitStats(31, 41, 51);
        SetSkill(SkillName.Healing, 36, 68);

        SetSpeed(0.2, 0.4);
        SpeechHue = Utility.RandomDyedHue();
        Title = "the artist";
        Hue = Race.Human.RandomSkinHue();

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");
        }

        AddItem(new Doublet(Utility.RandomDyedHue()));
        AddItem(new Sandals(Utility.RandomNeutralHue()));
        AddItem(new ShortPants(Utility.RandomNeutralHue()));
        AddItem(new HalfApron(Utility.RandomDyedHue()));

        Utility.AssignRandomHair(this);

        Container pack = new Backpack();

        pack.DropItem(new Gold(250, 300));

        pack.Movable = false;

        AddItem(pack);
    }

    public override bool CanTeach => true;

    public override bool ClickTitle => false;
}
