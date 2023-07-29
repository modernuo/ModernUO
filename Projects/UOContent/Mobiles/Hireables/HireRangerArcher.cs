using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class HireRangerArcher : BaseHire
{
    [Constructible]
    public HireRangerArcher() : base(AIType.AI_Archer)
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
        }

        EquipItem(new Shoes(Utility.RandomNeutralHue()));
        EquipItem(new Shirt());

        // Pick a random sword
        switch (Utility.Random(2))
        {
            case 0:
                {
                    EquipItem(new Bow());
                    break;
                }
            case 1:
                {
                    EquipItem(new CompositeBow());
                    break;
                }
        }

        EquipItem(new RangerChest());
        EquipItem(new RangerArms());
        EquipItem(new RangerGloves());
        EquipItem(new RangerGorget());
        EquipItem(new RangerLegs());
    }

    public override void GenerateLoot()
    {
        PackItem(new Arrow(100));
    }

    public override bool ClickTitle => false;
}
