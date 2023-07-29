using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class HireFighter : BaseHire
{
    [Constructible]
    public HireFighter()
    {
        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();

        Title = "the fighter";
        HairItemID = Race.RandomHair(Female);
        HairHue = Race.RandomHairHue();
        Race.RandomFacialHair(this);

        SetStr(91, 91);
        SetDex(91, 91);
        SetInt(50, 50);

        SetDamage(7, 14);

        SetSkill(SkillName.Tactics, 36, 67);
        SetSkill(SkillName.Magery, 22, 22);
        SetSkill(SkillName.Swords, 64, 100);
        SetSkill(SkillName.Parry, 60, 82);
        SetSkill(SkillName.Macing, 36, 67);
        SetSkill(SkillName.Focus, 36, 67);
        SetSkill(SkillName.Wrestling, 25, 47);

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
        }

        switch (Utility.Random(2))
        {
            case 0:
                {
                    EquipItem(new Shoes(Utility.RandomNeutralHue()));
                    break;
                }
            case 1:
                {
                    EquipItem(new Boots(Utility.RandomNeutralHue()));
                    break;
                }
        }

        EquipItem(new Shirt());

        // Pick a random sword
        BaseWeapon weapon = Utility.Random(5) switch
        {
            1 => new Broadsword(),
            2 => new VikingSword(),
            3 => new BattleAxe(),
            4 => new TwoHandedAxe(),
            _ => new Longsword()
        };

        EquipItem(weapon);

        // Pick a random shield
        if (FindItemOnLayer(Layer.TwoHanded) == null)
        {
            BaseShield shield = Utility.Random(6) switch
            {
                1 => new HeaterShield(),
                2 => new MetalKiteShield(),
                3 => new MetalShield(),
                4 => new WoodenKiteShield(),
                5 => new WoodenShield(),
                _ => new BronzeShield()
            };

            EquipItem(shield);
        }

        BaseArmor helm = Utility.Random(5) switch
        {
            1 => new Bascinet(),
            2 => new CloseHelm(),
            3 => new NorseHelm(),
            4 => new Helmet(),
            _ => null
        };

        EquipItem(helm);

        // Pick some armour
        switch (Utility.Random(4))
        {
            case 0: // Leather
                {
                    EquipItem(new LeatherChest());
                    EquipItem(new LeatherArms());
                    EquipItem(new LeatherGloves());
                    EquipItem(new LeatherGorget());
                    EquipItem(new LeatherLegs());
                    break;
                }
            case 1: // Studded Leather
                {
                    EquipItem(new StuddedChest());
                    EquipItem(new StuddedArms());
                    EquipItem(new StuddedGloves());
                    EquipItem(new StuddedGorget());
                    EquipItem(new StuddedLegs());
                    break;
                }
            case 2: // Ringmail
                {
                    EquipItem(new RingmailChest());
                    EquipItem(new RingmailArms());
                    EquipItem(new RingmailGloves());
                    EquipItem(new RingmailLegs());
                    break;
                }
            case 3: // Chain
                {
                    EquipItem(new ChainChest());
                    //EquipItem(new ChainCoif());
                    EquipItem(new ChainLegs());
                    break;
                }
        }

        PackGold(25, 100);
    }

    public override bool ClickTitle => false;
}
