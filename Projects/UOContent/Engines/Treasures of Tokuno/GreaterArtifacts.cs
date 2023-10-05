using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DarkenedSky : Kama
{
    [Constructible]
    public DarkenedSky()
    {
        WeaponAttributes.HitLightning = 60;
        Attributes.WeaponSpeed = 25;
        Attributes.WeaponDamage = 50;
    }

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override int LabelNumber => 1070966; // Darkened Sky

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois,
        out int nrgy, out int chaos, out int direct
    )
    {
        phys = fire = pois = chaos = direct = 0;
        cold = nrgy = 50;
    }
}

[SerializationGenerator(0, false)]
public partial class KasaOfTheRajin : Kasa
{
    [Constructible]
    public KasaOfTheRajin() => Attributes.SpellDamage = 12;

    public override int LabelNumber => 1070969; // Kasa of the Raj-in

    public override int BasePhysicalResistance => 12;
    public override int BaseFireResistance => 17;
    public override int BaseColdResistance => 21;
    public override int BasePoisonResistance => 17;
    public override int BaseEnergyResistance => 17;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class RuneBeetleCarapace : PlateDo
{
    [Constructible]
    public RuneBeetleCarapace()
    {
        Attributes.BonusMana = 10;
        Attributes.RegenMana = 3;
        Attributes.LowerManaCost = 15;
        ArmorAttributes.LowerStatReq = 100;
        ArmorAttributes.MageArmor = 1;
    }

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override int LabelNumber => 1070968; // Rune Beetle Carapace

    public override int BaseColdResistance => 14;
    public override int BaseEnergyResistance => 14;
}

[SerializationGenerator(0, false)]
public partial class Stormgrip : LeatherNinjaMitts
{
    [Constructible]
    public Stormgrip()
    {
        Attributes.BonusInt = 8;
        Attributes.Luck = 125;
        Attributes.WeaponDamage = 25;
    }

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override int LabelNumber => 1070970; // Stormgrip

    public override int BasePhysicalResistance => 10;
    public override int BaseColdResistance => 18;
    public override int BaseEnergyResistance => 18;
}

[SerializationGenerator(0, false)]
public partial class SwordOfTheStampede : NoDachi
{
    [Constructible]
    public SwordOfTheStampede()
    {
        WeaponAttributes.HitHarm = 100;
        Attributes.AttackChance = 10;
        Attributes.WeaponDamage = 60;
    }

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override int LabelNumber => 1070964; // Sword of the Stampede

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois,
        out int nrgy, out int chaos, out int direct
    )
    {
        phys = fire = pois = nrgy = chaos = direct = 0;
        cold = 100;
    }
}

[SerializationGenerator(0, false)]
public partial class SwordsOfProsperity : Daisho
{
    [Constructible]
    public SwordsOfProsperity()
    {
        WeaponAttributes.MageWeapon = 30;
        Attributes.SpellChanneling = 1;
        Attributes.CastSpeed = 1;
        Attributes.Luck = 200;
    }

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override int LabelNumber => 1070963; // Swords of Prosperity

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois,
        out int nrgy, out int chaos, out int direct
    )
    {
        phys = cold = pois = nrgy = chaos = direct = 0;
        fire = 100;
    }
}

[SerializationGenerator(0, false)]
public partial class TheHorselord : Yumi
{
    [Constructible]
    public TheHorselord()
    {
        Attributes.BonusDex = 5;
        Attributes.RegenMana = 1;
        Attributes.Luck = 125;
        Attributes.WeaponDamage = 50;

        Slayer = SlayerName.ElementalBan;
        Slayer2 = SlayerName.ReptilianDeath;
    }

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override int LabelNumber => 1070967; // The Horselord
}

[SerializationGenerator(0, false)]
public partial class TomeOfLostKnowledge : Spellbook
{
    [Constructible]
    public TomeOfLostKnowledge()
    {
        LootType = LootType.Regular;
        Hue = 0x530;

        SkillBonuses.SetValues(0, SkillName.Magery, 15.0);
        Attributes.BonusInt = 8;
        Attributes.LowerManaCost = 15;
        Attributes.SpellDamage = 15;
    }

    public override int LabelNumber => 1070971; // Tome of Lost Knowledge
}

[SerializationGenerator(0, false)]
public partial class WindsEdge : Tessen
{
    [Constructible]
    public WindsEdge()
    {
        WeaponAttributes.HitLeechMana = 40;

        Attributes.WeaponDamage = 50;
        Attributes.WeaponSpeed = 50;
        Attributes.DefendChance = 10;
    }

    public override int LabelNumber => 1070965; // Wind's Edge

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

public enum PigmentType
{
    None,
    ParagonGold,
    VioletCouragePurple,
    InvulnerabilityBlue,
    LunaWhite,
    DryadGreen,
    ShadowDancerBlack,
    BerserkerRed,
    NoxGreen,
    RumRed,
    FireOrange,
    FadedCoal,
    Coal,
    FadedGold,
    StormBronze,
    Rose,
    MidnightCoal,
    FadedBronze,
    FadedRose,
    DeepRose
}

[SerializationGenerator(0, false)]
public partial class PigmentsOfTokuno : BasePigmentsOfTokuno
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
            /*PigmentType.ParagonGold,*/ 0x501, 1070987
        },
        new[]
        {
            /*PigmentType.VioletCouragePurple,*/ 0x486, 1070988
        },
        new[]
        {
            /*PigmentType.InvulnerabilityBlue,*/ 0x4F2, 1070989
        },
        new[]
        {
            /*PigmentType.LunaWhite,*/ 0x47E, 1070990
        },
        new[]
        {
            /*PigmentType.DryadGreen,*/ 0x48F, 1070991
        },
        new[]
        {
            /*PigmentType.ShadowDancerBlack,*/ 0x455, 1070992
        },
        new[]
        {
            /*PigmentType.BerserkerRed,*/ 0x21, 1070993
        },
        new[]
        {
            /*PigmentType.NoxGreen,*/ 0x58C, 1070994
        },
        new[]
        {
            /*PigmentType.RumRed,*/ 0x66C, 1070995
        },
        new[]
        {
            /*PigmentType.FireOrange,*/ 0x54F, 1070996
        },
        new[]
        {
            /*PigmentType.Fadedcoal,*/ 0x96A, 1079579
        },
        new[]
        {
            /*PigmentType.Coal,*/ 0x96B, 1079580
        },
        new[]
        {
            /*PigmentType.FadedGold,*/ 0x972, 1079581
        },
        new[]
        {
            /*PigmentType.StormBronze,*/ 0x977, 1079582
        },
        new[]
        {
            /*PigmentType.Rose,*/ 0x97C, 1079583
        },
        new[]
        {
            /*PigmentType.MidnightCoal,*/ 0x96C, 1079584
        },
        new[]
        {
            /*PigmentType.FadedBronze,*/ 0x975, 1079585
        },
        new[]
        {
            /*PigmentType.FadedRose,*/ 0x97B, 1079586
        },
        new[]
        {
            /*PigmentType.DeepRose,*/ 0x97E, 1079587
        }
    };

    [Constructible]
    public PigmentsOfTokuno(PigmentType type = PigmentType.None) : this(
        type,
        type is PigmentType.None or >= PigmentType.FadedCoal ? 10 : 50
    )
    {
    }

    [Constructible]
    public PigmentsOfTokuno(PigmentType type, int uses) : base(uses)
    {
        Weight = 1.0;
        Type = type;
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public PigmentType Type
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

    public override int LabelNumber => 1070933; // Pigments of Tokuno

    public static int[] GetInfo(PigmentType type)
    {
        var v = (int)type;

        if (v < 0 || v >= _table.Length)
        {
            v = 0;
        }

        return _table[v];
    }
}
