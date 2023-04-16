using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class HirePaladin : BaseHire
{
    [Constructible]
    public HirePaladin()
    {
        SpeechHue = Utility.RandomDyedHue();
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

        Title = "the paladin";
        HairItemID = Race.RandomHair(Female);
        HairHue = Race.RandomHairHue();
        Race.RandomFacialHair(this);

        switch (Utility.Random(5))
        {
            case 0:
                {
                    break;
                }
            case 1:
                {
                    EquipItem(new Bascinet());
                    break;
                }
            case 2:
                {
                    EquipItem(new CloseHelm());
                    break;
                }
            case 3:
                {
                    EquipItem(new NorseHelm());
                    break;
                }
            case 4:
                {
                    EquipItem(new Helmet());
                    break;
                }
        }

        SetStr(86, 100);
        SetDex(81, 95);
        SetInt(61, 75);

        SetDamage(10, 23);

        SetSkill(SkillName.Swords, 66.0, 97.5);
        SetSkill(SkillName.Anatomy, 65.0, 87.5);
        SetSkill(SkillName.MagicResist, 25.0, 47.5);
        SetSkill(SkillName.Healing, 65.0, 87.5);
        SetSkill(SkillName.Tactics, 65.0, 87.5);
        SetSkill(SkillName.Wrestling, 15.0, 37.5);
        SetSkill(SkillName.Parry, 45.0, 60.5);
        SetSkill(SkillName.Chivalry, 85, 100);

        Fame = 100;
        Karma = 250;

        EquipItem(new Shoes(Utility.RandomNeutralHue()));
        EquipItem(new Shirt());
        EquipItem(new VikingSword());
        EquipItem(new MetalKiteShield());

        EquipItem(new PlateChest());
        EquipItem(new PlateLegs());
        EquipItem(new PlateArms());
        EquipItem(new LeatherGorget());
        PackGold(20, 100);
    }

    public override bool ClickTitle => false;
}
