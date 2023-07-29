using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class HireThief : BaseHire
{
    [Constructible]
    public HireThief()
    {
        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();

        Title = "the thief";
        HairItemID = Race.RandomHair(Female);
        HairHue = Race.RandomHairHue();
        Race.RandomFacialHair(this);

        SetStr(81, 95);
        SetDex(86, 100);
        SetInt(61, 75);

        SetDamage(10, 23);

        SetSkill(SkillName.Stealing, 66.0, 97.5);
        SetSkill(SkillName.Peacemaking, 65.0, 87.5);
        SetSkill(SkillName.MagicResist, 25.0, 47.5);
        SetSkill(SkillName.Healing, 65.0, 87.5);
        SetSkill(SkillName.Tactics, 65.0, 87.5);
        SetSkill(SkillName.Fencing, 65.0, 87.5);
        SetSkill(SkillName.Parry, 45.0, 60.5);
        SetSkill(SkillName.Lockpicking, 65, 87);
        SetSkill(SkillName.Hiding, 65, 87);
        SetSkill(SkillName.Snooping, 65, 87);

        Fame = 100;
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
        EquipItem(new Dagger());
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
