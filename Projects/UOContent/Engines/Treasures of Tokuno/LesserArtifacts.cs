using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AncientFarmersKasa : Kasa
{
    [Constructible]
    public AncientFarmersKasa()
    {
        Attributes.BonusStr = 5;
        Attributes.BonusStam = 5;
        Attributes.RegenStam = 5;

        SkillBonuses.SetValues(0, SkillName.AnimalLore, 5.0);
    }

    public override int LabelNumber => 1070922; // Ancient Farmer's Kasa
    public override int BaseColdResistance => 19;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class AncientSamuraiDo : PlateDo
{
    [Constructible]
    public AncientSamuraiDo()
    {
        ArmorAttributes.LowerStatReq = 100;
        ArmorAttributes.MageArmor = 1;
        SkillBonuses.SetValues(0, SkillName.Parry, 10.0);
    }

    public override int LabelNumber => 1070926; // Ancient Samurai Do

    public override int BasePhysicalResistance => 15;
    public override int BaseFireResistance => 12;
    public override int BaseColdResistance => 10;
    public override int BasePoisonResistance => 11;
    public override int BaseEnergyResistance => 8;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class ArmsOfTacticalExcellence : LeatherHiroSode
{
    [Constructible]
    public ArmsOfTacticalExcellence()
    {
        Attributes.BonusDex = 5;
        SkillBonuses.SetValues(0, SkillName.Tactics, 12.0);
    }

    public override int LabelNumber => 1070921; // Arms of Tactical Excellence

    public override int BaseFireResistance => 9;
    public override int BaseColdResistance => 13;
    public override int BasePoisonResistance => 8;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class BlackLotusHood : ClothNinjaHood
{
    [Constructible]
    public BlackLotusHood()
    {
        Attributes.LowerManaCost = 6;
        Attributes.AttackChance = 6;
        ClothingAttributes.SelfRepair = 5;
    }

    public override int LabelNumber => 1070919; // Black Lotus Hood

    public override int BasePhysicalResistance => 0;
    public override int BaseFireResistance => 11;
    public override int BaseColdResistance => 15;
    public override int BasePoisonResistance => 11;
    public override int BaseEnergyResistance => 11;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class DaimyosHelm : PlateBattleKabuto
{
    [Constructible]
    public DaimyosHelm()
    {
        ArmorAttributes.LowerStatReq = 100;
        ArmorAttributes.MageArmor = 1;
        ArmorAttributes.SelfRepair = 3;
        Attributes.WeaponSpeed = 10;
    }

    public override int LabelNumber => 1070920; // Daimyo's Helm

    public override int BaseColdResistance => 10;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class DemonForks : Sai
{
    [Constructible]
    public DemonForks()
    {
        WeaponAttributes.ResistFireBonus = 10;
        WeaponAttributes.ResistPoisonBonus = 10;

        Attributes.ReflectPhysical = 10;
        Attributes.WeaponDamage = 35;
        Attributes.DefendChance = 10;
    }

    public override int LabelNumber => 1070917; // Demon Forks

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class DragonNunchaku : Nunchaku
{
    [Constructible]
    public DragonNunchaku()
    {
        WeaponAttributes.ResistFireBonus = 5;
        WeaponAttributes.SelfRepair = 3;
        WeaponAttributes.HitFireball = 50;

        Attributes.WeaponDamage = 40;
        Attributes.WeaponSpeed = 20;
    }

    public override int LabelNumber => 1070914; // Dragon Nunchaku

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class Exiler : Tetsubo
{
    [Constructible]
    public Exiler()
    {
        WeaponAttributes.HitDispel = 33;
        Slayer = SlayerName.Exorcism;

        Attributes.WeaponDamage = 40;
        Attributes.WeaponSpeed = 20;
    }

    public override int LabelNumber => 1070913; // Exiler

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois,
        out int nrgy, out int chaos, out int direct
    )
    {
        phys = fire = cold = pois = chaos = direct = 0;

        nrgy = 100;
    }
}

[SerializationGenerator(0, false)]
public partial class GlovesOfTheSun : LeatherNinjaMitts
{
    [Constructible]
    public GlovesOfTheSun()
    {
        Attributes.RegenHits = 2;
        Attributes.NightSight = 1;
        Attributes.LowerManaCost = 5;
        Attributes.LowerRegCost = 18;
    }

    public override int LabelNumber => 1070924; // Gloves of the Sun

    public override int BaseFireResistance => 24;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class HanzosBow : Yumi
{
    [Constructible]
    public HanzosBow()
    {
        WeaponAttributes.HitLeechHits = 40;
        WeaponAttributes.SelfRepair = 3;

        Attributes.WeaponDamage = 50;

        SkillBonuses.SetValues(0, SkillName.Ninjitsu, 10);
    }

    public override int LabelNumber => 1070918; // Hanzo's Bow

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class LegsOfStability : PlateSuneate
{
    [Constructible]
    public LegsOfStability()
    {
        Attributes.BonusStam = 5;

        ArmorAttributes.SelfRepair = 3;
        ArmorAttributes.LowerStatReq = 100;
        ArmorAttributes.MageArmor = 1;
    }

    public override int LabelNumber => 1070925; // Legs of Stability

    public override int BasePhysicalResistance => 20;
    public override int BasePoisonResistance => 18;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class PeasantsBokuto : Bokuto
{
    [Constructible]
    public PeasantsBokuto()
    {
        WeaponAttributes.SelfRepair = 3;
        WeaponAttributes.HitLowerDefend = 30;

        Attributes.WeaponDamage = 35;
        Attributes.WeaponSpeed = 10;
        Slayer = SlayerName.SnakesBane;
    }

    public override int LabelNumber => 1070912; // Peasant's Bokuto

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class PilferedDancerFans : Tessen
{
    [Constructible]
    public PilferedDancerFans()
    {
        Attributes.WeaponDamage = 20;
        Attributes.WeaponSpeed = 20;
        Attributes.CastRecovery = 2;
        Attributes.DefendChance = 5;
        Attributes.SpellChanneling = 1;
    }

    public override int LabelNumber => 1070916; // Pilfered Dancer Fans

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class TheDestroyer : NoDachi
{
    [Constructible]
    public TheDestroyer()
    {
        WeaponAttributes.HitLeechStam = 40;

        Attributes.BonusStr = 6;
        Attributes.AttackChance = 10;
        Attributes.WeaponDamage = 50;
    }

    public override int LabelNumber => 1070915; // The Destroyer

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class TomeOfEnlightenment : Spellbook
{
    [Constructible]
    public TomeOfEnlightenment()
    {
        LootType = LootType.Regular;
        Hue = 0x455;

        Attributes.BonusInt = 5;
        Attributes.SpellDamage = 10;
        Attributes.CastSpeed = 1;
    }

    public override int LabelNumber => 1070934; // Tome of Enlightenment
}

[SerializationGenerator(0, false)]
public partial class LeurociansMempoOfFortune : LeatherMempo
{
    [Constructible]
    public LeurociansMempoOfFortune()
    {
        LootType = LootType.Regular;
        Hue = 0x501;

        Attributes.Luck = 300;
        Attributes.RegenMana = 1;
    }

    public override int LabelNumber => 1071460; // Leurocian's mempo of fortune

    public override int BasePhysicalResistance => 15;
    public override int BaseFireResistance => 10;
    public override int BaseColdResistance => 10;
    public override int BasePoisonResistance => 10;
    public override int BaseEnergyResistance => 15;
    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

// Non weapon/armor ones:

[SerializationGenerator(0, false)]
public partial class AncientUrn : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _urnName;

    [Constructible]
    public AncientUrn() : this(Names.RandomElement())
    {
    }

    [Constructible]
    public AncientUrn(string urnName) : base(0x241D)
    {
        _urnName = urnName;
        Weight = 1.0;
    }

    public static string[] Names { get; } =
    {
        "Akira",
        "Avaniaga",
        "Aya",
        "Chie",
        "Emiko",
        "Fumiyo",
        "Gennai",
        "Gennosuke",
        "Genjo",
        "Hamato",
        "Harumi",
        "Ikuyo",
        "Juri",
        "Kaori",
        "Kaoru",
        "Kiyomori",
        "Mayako",
        "Motoki",
        "Musashi",
        "Nami",
        "Nobukazu",
        "Roku",
        "Romi",
        "Ryo",
        "Sanzo",
        "Sakamae",
        "Satoshi",
        "Takamori",
        "Takuro",
        "Teruyo",
        "Toshiro",
        "Yago",
        "Yeijiro",
        "Yoshi",
        "Zeshin"
    };

    public override int LabelNumber => 1071014; // Ancient Urn

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Utility.Intern(ref _urnName);
    }

    public override void AddNameProperty(IPropertyList list)
    {
        list.Add(1070935, _urnName); // Ancient Urn of ~1_name~
    }

    public override void OnSingleClick(Mobile from)
    {
        LabelTo(from, 1070935, _urnName); // Ancient Urn of ~1_name~
    }
}

[SerializationGenerator(0, false)]
public partial class HonorableSwords : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _swordsName;

    [Constructible]
    public HonorableSwords() : this(AncientUrn.Names.RandomElement())
    {
    }

    [Constructible]
    public HonorableSwords(string swordsName) : base(0x2853)
    {
        _swordsName = swordsName;

        Weight = 5.0;
    }

    public override int LabelNumber => 1071015; // Honorable Swords

    public override void AddNameProperty(IPropertyList list)
    {
        list.Add(1070936, _swordsName); // Honorable Swords of ~1_name~
    }

    public override void OnSingleClick(Mobile from)
    {
        LabelTo(from, 1070936, _swordsName); // Honorable Swords of ~1_name~
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Utility.Intern(ref _swordsName);
    }
}

[Furniture]
[Flippable(0x2811, 0x2812)]
[SerializationGenerator(0, false)]
public partial class ChestOfHeirlooms : LockableContainer
{
    [Constructible]
    public ChestOfHeirlooms() : base(0x2811)
    {
        Locked = true;
        LockLevel = 95;
        MaxLockLevel = 140;
        RequiredSkill = 95;

        TrapType = TrapType.ExplosionTrap;
        TrapLevel = 10;
        TrapPower = 100;

        GumpID = 0x10B;

        for (var i = 0; i < 10; ++i)
        {
            var item = Loot.ChestOfHeirloomsContains();

            var attributeCount = Utility.RandomMinMax(1, 5);
            var min = 20;
            var max = 80;

            if (item is BaseWeapon weapon)
            {
                if (Core.AOS)
                {
                    BaseRunicTool.ApplyAttributesTo(weapon, attributeCount, min, max);
                }
                else
                {
                    weapon.DamageLevel = (WeaponDamageLevel)Utility.Random(6);
                    weapon.AccuracyLevel = (WeaponAccuracyLevel)Utility.Random(6);
                    weapon.DurabilityLevel = (WeaponDurabilityLevel)Utility.Random(6);
                }
            }
            else if (item is BaseArmor armor)
            {
                if (Core.AOS)
                {
                    BaseRunicTool.ApplyAttributesTo(armor, attributeCount, min, max);
                }
                else
                {
                    armor.ProtectionLevel = (ArmorProtectionLevel)Utility.Random(6);
                    armor.Durability = (ArmorDurabilityLevel)Utility.Random(6);
                }
            }
            else if (Core.AOS)
            {
                if (item is BaseHat hat)
                {
                    BaseRunicTool.ApplyAttributesTo(hat, attributeCount, min, max);
                }
                else if (item is BaseJewel jewel)
                {
                    BaseRunicTool.ApplyAttributesTo(jewel, attributeCount, min, max);
                }
            }

            DropItem(item);
        }
    }

    public override int LabelNumber => 1070937; // Chest of heirlooms
}

[SerializationGenerator(0, false)]
public partial class FluteOfRenewal : BambooFlute
{
    [Constructible]
    public FluteOfRenewal()
    {
        // -1 to exclude Fey slayer.  Try to confirm no fey slayer on this on OSI
        Slayer = SlayerGroup.Groups[Utility.Random(SlayerGroup.Groups.Length - 1)].Super.Name;

        ReplenishesCharges = true;
    }

    public override int LabelNumber => 1070927; // Flute of Renewal

    public override int InitMinUses => 300;
    public override int InitMaxUses => 300;
}

public enum LesserPigmentType
{
    None,
    PaleOrange,
    FreshRose,
    ChaosBlue,
    Silver,
    NobleGold,
    LightGreen,
    PaleBlue,
    FreshPlum,
    DeepBrown,
    BurntBrown
}

[SerializationGenerator(0, false)]
public partial class LesserPigmentsOfTokuno : BasePigmentsOfTokuno
{
    private static readonly int[][] _table =
    {
        // Hue, Label
        new[]
        {
            /*PigmentType.None,*/ 0, -1
        },
        new[]
        {
            /*PigmentType.PaleOrange,*/ 0x02E, 1071458
        },
        new[]
        {
            /*PigmentType.FreshRose,*/ 0x4B9, 1071455
        },
        new[]
        {
            /*PigmentType.ChaosBlue,*/ 0x005, 1071459
        },
        new[]
        {
            /*PigmentType.Silver,*/ 0x3E9, 1071451
        },
        new[]
        {
            /*PigmentType.NobleGold,*/ 0x227, 1071457
        },
        new[]
        {
            /*PigmentType.LightGreen,*/ 0x1C8, 1071454
        },
        new[]
        {
            /*PigmentType.PaleBlue,*/ 0x24F, 1071456
        },
        new[]
        {
            /*PigmentType.FreshPlum,*/ 0x145, 1071450
        },
        new[]
        {
            /*PigmentType.DeepBrown,*/ 0x3F0, 1071452
        },
        new[]
        {
            /*PigmentType.BurntBrown,*/ 0x41A, 1071453
        }
    };

    [Constructible]
    public LesserPigmentsOfTokuno() : this((LesserPigmentType)Utility.Random(0, 11))
    {
    }

    [Constructible]
    public LesserPigmentsOfTokuno(LesserPigmentType type) : base(1)
    {
        Weight = 1.0;
        Type = type;
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public LesserPigmentType Type
    {
        get => _type;
        set
        {
            _type = value;

            var v = (int)_type;

            if (v >= 0 && v < _table.Length)
            {
                Hue = _table[v][0];
                Label = _table[v][1];
            }
            else
            {
                Hue = 0;
                Label = -1;
            }
        }
    }

    public static int[] GetInfo(LesserPigmentType type)
    {
        var v = (int)type;

        if (v < 0 || v >= _table.Length)
        {
            v = 0;
        }

        return _table[v];
    }
}

[SerializationGenerator(0, false)]
public partial class MetalPigmentsOfTokuno : BasePigmentsOfTokuno
{
    [Constructible]
    public MetalPigmentsOfTokuno() : base(1)
    {
        RandomHue();
        Label = -1;
    }

    public void RandomHue()
    {
        var a = Utility.Random(0, 30);
        if (a != 0)
        {
            Hue = a + 0x960;
        }
        else
        {
            Hue = 0;
        }
    }
}
