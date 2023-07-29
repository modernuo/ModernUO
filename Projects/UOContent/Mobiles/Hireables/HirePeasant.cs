using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class HirePeasant : BaseHire
{
    [Constructible]
    public HirePeasant()
    {
        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();

        Title = "the peasant";
        HairItemID = Race.RandomHair(Female);
        HairHue = Race.RandomHairHue();
        Race.RandomFacialHair(this);

        SetStr(26, 26);
        SetDex(21, 21);
        SetInt(16, 16);

        SetDamage(10, 23);

        SetSkill(SkillName.Tactics, 5, 27);
        SetSkill(SkillName.Wrestling, 5, 5);
        SetSkill(SkillName.Swords, 5, 27);

        Fame = 0;
        Karma = 0;

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");

            switch (Utility.Random(2))
            {
                case 0:
                    {
                        EquipItem(new Skirt(Utility.RandomNeutralHue()));
                        break;
                    }
                case 1:
                    {
                        EquipItem(new Kilt(Utility.RandomNeutralHue()));
                        break;
                    }
            }
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");
            EquipItem(new ShortPants(Utility.RandomNeutralHue()));
        }

        EquipItem(new Katana());

        EquipItem(new Sandals(Utility.RandomNeutralHue()));

        switch (Utility.Random(2))
        {
            case 0:
                {
                    EquipItem(new Doublet(Utility.RandomNeutralHue()));
                    break;
                }
            case 1:
                {
                    EquipItem(new Shirt(Utility.RandomNeutralHue()));
                    break;
                }
        }

        PackGold(0, 25);
    }

    public override bool ClickTitle => false;
}
