using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class HireRanger : BaseHire
{
    [Constructible]
    public HireRanger()
    {
        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();

        Title = "the ranger";
        HairItemID = Race.RandomHair(Female);
        HairHue = Race.RandomHairHue();
        Race.RandomFacialHair(this);

        SetStr(91, 91);
        SetDex(76, 76);
        SetInt(61, 61);

        SetDamage(13, 24);

        SetSkill(SkillName.Wrestling, 15, 37);
        SetSkill(SkillName.Parry, 45, 60);
        SetSkill(SkillName.Archery, 66, 97);
        SetSkill(SkillName.Magery, 62, 62);
        SetSkill(SkillName.Swords, 35, 57);
        SetSkill(SkillName.Fencing, 15, 37);
        SetSkill(SkillName.Tactics, 65, 87);

        Fame = 100;
        Karma = 125;

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");
            EquipItem(new ShortPants(Utility.RandomNeutralHue()));
        }

        EquipItem(new Shoes(Utility.RandomNeutralHue()));
        EquipItem(new Shirt());

        // Pick a random sword
        switch (Utility.Random(3))
        {
            case 0:
                {
                    EquipItem(new Longsword());
                    break;
                }
            case 1:
                {
                    EquipItem(new VikingSword());
                    break;
                }
            case 2:
                {
                    EquipItem(new Broadsword());
                    break;
                }
        }

        EquipItem(new StuddedChest());
        EquipItem(new StuddedArms());
        EquipItem(new StuddedGloves());
        EquipItem(new StuddedLegs());
        EquipItem(new StuddedGorget());
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        PackItem(new Arrow(20));
        PackGold(10, 75);
    }
}
