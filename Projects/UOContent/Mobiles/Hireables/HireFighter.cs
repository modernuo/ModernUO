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
        switch (Utility.Random(5))
        {
            case 0:
                {
                    EquipItem(new Longsword());
                    break;
                }
            case 1:
                {
                    EquipItem(new Broadsword());
                    break;
                }
            case 2:
                {
                    EquipItem(new VikingSword());
                    break;
                }
            case 3:
                {
                    EquipItem(new BattleAxe());
                    break;
                }
            case 4:
                {
                    EquipItem(new TwoHandedAxe());
                    break;
                }
        }

        // Pick a random shield
        if (FindItemOnLayer(Layer.TwoHanded) == null)
        {
            switch (Utility.Random(8))
            {
                case 0:
                    {
                        EquipItem(new BronzeShield());
                        break;
                    }
                case 1:
                    {
                        EquipItem(new HeaterShield());
                        break;
                    }
                case 2:
                    {
                        EquipItem(new MetalKiteShield());
                        break;
                    }
                case 3:
                    {
                        EquipItem(new MetalShield());
                        break;
                    }
                case 4:
                    {
                        EquipItem(new WoodenKiteShield());
                        break;
                    }
                case 5:
                    {
                        EquipItem(new WoodenShield());
                        break;
                    }
                case 6:
                    {
                        EquipItem(new OrderShield());
                        break;
                    }
                case 7:
                    {
                        EquipItem(new ChaosShield());
                        break;
                    }
            }
        }

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
