using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Actor : BaseCreature
{
    [Constructible]
    public Actor() : base(AIType.AI_Animal, FightMode.None)
    {
        InitStats(31, 41, 51);

        SetSpeed(0.2, 0.4);
        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");
            AddItem(new FancyDress(Utility.RandomDyedHue()));
            Title = "the actress";
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");
            AddItem(new LongPants(Utility.RandomNeutralHue()));
            AddItem(new FancyShirt(Utility.RandomDyedHue()));
            Title = "the actor";
        }

        AddItem(new Boots(Utility.RandomNeutralHue()));

        Utility.AssignRandomHair(this);

        var pack = new Backpack
        {
            Movable = false
        };

        pack.DropItem(new Gold(250, 300));
        AddItem(pack);
    }

    public override bool ClickTitle => false;
}
