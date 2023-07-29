using Server.Items;
using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class HireBard : BaseHire
{
    [Constructible]
    public HireBard()
    {
        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();

        Title = "the bard";
        HairItemID = Race.RandomHair(Female);
        HairHue = Race.RandomHairHue();
        Race.RandomFacialHair(this);

        SetStr(16, 16);
        SetDex(26, 26);
        SetInt(26, 26);

        SetDamage(5, 10);

        SetSkill(SkillName.Tactics, 35, 57);
        SetSkill(SkillName.Magery, 22, 22);
        SetSkill(SkillName.Swords, 45, 67);
        SetSkill(SkillName.Archery, 36, 67);
        SetSkill(SkillName.Parry, 45, 60);
        SetSkill(SkillName.Musicianship, 66.0, 97.5);
        SetSkill(SkillName.Peacemaking, 65.0, 87.5);

        Fame = 100;
        Karma = 100;

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");

            switch (Utility.Random(2))
            {
                case 0:
                    {
                        EquipItem(new Skirt(Utility.RandomDyedHue()));
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

        EquipItem(new Shoes(Utility.RandomNeutralHue()));

        switch (Utility.Random(2))
        {
            case 0:
                {
                    EquipItem(new Doublet(Utility.RandomDyedHue()));
                    break;
                }
            case 1:
                {
                    EquipItem(new Shirt(Utility.RandomDyedHue()));
                    break;
                }
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);

        if (Utility.RandomBool())
        {
            PackItem(Loot.RandomInstrument());
        }

        switch (Utility.Random(2))
        {
            case 0:
                {
                    PackItem(new Longsword());
                    break;
                }
            case 1:
                {
                    PackItem(new Bow());
                    PackItem(new Arrow(100));
                    break;
                }
        }

        PackGold(10, 50);
    }

    public override bool ClickTitle => false;
}
