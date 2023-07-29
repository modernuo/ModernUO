using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class HireMage : BaseHire
{
    [Constructible]
    public HireMage() : base(AIType.AI_Mage)
    {
        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();
        Title = "the mage";

        HairItemID = Race.RandomHair(Female);
        HairHue = Race.RandomHairHue();
        Race.RandomFacialHair(this);

        SetStr(61, 75);
        SetDex(81, 95);
        SetInt(86, 100);

        SetDamage(10, 23);

        SetSkill(SkillName.EvalInt, 100.0, 125);
        SetSkill(SkillName.Magery, 100, 125);
        SetSkill(SkillName.Meditation, 100, 125);
        SetSkill(SkillName.MagicResist, 100, 125);
        SetSkill(SkillName.Tactics, 100, 125);
        SetSkill(SkillName.Macing, 100, 125);

        Fame = 100;
        Karma = 100;

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

        EquipItem(new Shirt());
        EquipItem(new Robe(Utility.RandomNeutralHue()));

        if (Utility.RandomBool())
        {
            EquipItem(new Shoes(Utility.RandomNeutralHue()));
        }
        else
        {
            EquipItem(new ThighBoots());
        }

        PackGold(20, 100);
    }

    public override bool ClickTitle => false;
}
