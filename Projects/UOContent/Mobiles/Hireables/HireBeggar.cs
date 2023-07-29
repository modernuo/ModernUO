using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class HireBeggar : BaseHire
{
    [Constructible]
    public HireBeggar()
    {
        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();

        Title = "the beggar";
        HairItemID = Race.RandomHair(Female);
        HairHue = Race.RandomHairHue();
        Race.RandomFacialHair(this);

        SetStr(26, 26);
        SetDex(21, 21);
        SetInt(36, 36);

        SetDamage(1, 1);

        SetSkill(SkillName.Begging, 66, 97);
        SetSkill(SkillName.Tactics, 5, 27);
        SetSkill(SkillName.Wrestling, 5, 27);
        SetSkill(SkillName.Magery, 2, 2);

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
