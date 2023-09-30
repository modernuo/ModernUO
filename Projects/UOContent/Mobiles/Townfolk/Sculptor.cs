using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Sculptor : BaseCreature
{
    [Constructible]
    public Sculptor() : base(AIType.AI_Animal, FightMode.None)
    {
        InitStats(31, 41, 51);

        SetSpeed(0.2, 0.4);
        SpeechHue = Utility.RandomDyedHue();
        Title = "the sculptor";
        Hue = Race.Human.RandomSkinHue();

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");
            AddItem(new Kilt(Utility.RandomNeutralHue()));
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");
            AddItem(new LongPants(Utility.RandomNeutralHue()));
        }

        AddItem(new Doublet(Utility.RandomNeutralHue()));
        AddItem(new HalfApron());

        Utility.AssignRandomHair(this);

        Container pack = new Backpack();

        pack.DropItem(new Gold(250, 300));

        pack.Movable = false;

        AddItem(pack);
    }

    public override bool ClickTitle => false;
}
