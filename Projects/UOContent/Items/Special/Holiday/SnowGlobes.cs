using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SnowGlobe : Item
{
    public SnowGlobe() : base(0xE2F)
    {
        LootType = LootType.Blessed;
        Light = LightType.Circle150;
    }

    public override double DefaultWeight => 1.0;
}

public enum SnowGlobeTypeOne
{
    Britain,
    Moonglow,
    Minoc,
    Magincia,
    BuccaneersDen,
    Trinsic,
    Yew,
    SkaraBrae,
    Jhelom,
    Nujelm,
    Papua,
    Delucia,
    Cove,
    Ocllo,
    SerpentsHold,
    EmpathAbbey,
    TheLycaeum,
    Vesper,
    Wind
}

[SerializationGenerator(1, false)]
public partial class SnowGlobeOne : SnowGlobe
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SnowGlobeTypeOne _place;

    [Constructible]
    public SnowGlobeOne() : this((SnowGlobeTypeOne)Utility.Random(19))
    {
    }

    [Constructible]
    public SnowGlobeOne(SnowGlobeTypeOne type) => _place = type;

    public override int LabelNumber => 1041454 + (int)_place;

    private void Deserialize(IGenericReader reader, int version)
    {
        _place = (SnowGlobeTypeOne)reader.ReadEncodedInt();
    }
}

public enum SnowGlobeTypeTwo
{
    AncientCitadel,
    BlackthornesCastle,
    CityofMontor,
    CityofMistas,
    ExodusLair,
    LakeofFire,
    Lakeshire,
    PassofKarnaugh,
    TheEtherealFortress,
    TwinOaksTavern,
    ChaosShrine,
    ShrineofHumility,
    ShrineofSacrifice,
    ShrineofCompassion,
    ShrineofHonor,
    ShrineofHonesty,
    ShrineofSpirituality,
    ShrineofJustice,
    ShrineofValor
}

[SerializationGenerator(1, false)]
public partial class SnowGlobeTwo : SnowGlobe
{
    /* Oddly, these are not localized. */
    private static readonly string[] _placeNames =
    {
        /* AncientCitadel */ "Ancient Citadel",
        /* BlackthornesCastle */ "Blackthorne's Castle",
        /* CityofMontor */ "City of Montor",
        /* CityofMistas */ "City of Mistas",
        /* ExodusLair */ "Exodus' Lair",
        /* LakeofFire */ "Lake of Fire",
        /* Lakeshire */ "Lakeshire",
        /* PassofKarnaugh */ "Pass of Karnaugh",
        /* TheEtherealFortress */ "The Ethereal Fortress",
        /* TwinOaksTavern */ "Twin Oaks Tavern",
        /* ChaosShrine */ "Chaos Shrine",
        /* ShrineofHumility */ "Shrine of Humility",
        /* ShrineofSacrifice */ "Shrine of Sacrifice",
        /* ShrineofCompassion */ "Shrine of Compassion",
        /* ShrineofHonor */ "Shrine of Honor",
        /* ShrineofHonesty */ "Shrine of Honesty",
        /* ShrineofSpirituality */ "Shrine of Spirituality",
        /* ShrineofJustice */ "Shrine of Justice",
        /* ShrineofValor */ "Shrine of Valor"
    };

    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SnowGlobeTypeTwo _place;

    [Constructible]
    public SnowGlobeTwo() : this((SnowGlobeTypeTwo)Utility.Random(_placeNames.Length))
    {
    }

    [Constructible]
    public SnowGlobeTwo(SnowGlobeTypeTwo type) => _place = type;

    public override string DefaultName
    {
        get
        {
            var idx = (int)_place;

            if (idx < 0 || idx >= _placeNames.Length)
            {
                return "a snowy scene";
            }

            return $"a snowy scene of {_placeNames[idx]}";
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _place = (SnowGlobeTypeTwo)reader.ReadEncodedInt();
    }
}

public enum SnowGlobeTypeThree
{
    Luna,
    Umbra,
    Zento,
    Heartwood,
    Covetous,
    Deceit,
    Destard,
    Hythloth,
    Khaldun,
    Shame,
    Wrong,
    Doom,
    TheCitadel,
    ThePalaceofParoxysmus,
    TheBlightedGrove,
    ThePrismofLight
}

[SerializationGenerator(1, false)]
public partial class SnowGlobeThree : SnowGlobe
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SnowGlobeTypeThree _place;

    [Constructible]
    public SnowGlobeThree() : this((SnowGlobeTypeThree)Utility.Random(16))
    {
    }

    [Constructible]
    public SnowGlobeThree(SnowGlobeTypeThree type) => _place = type;

    public override int LabelNumber =>
        _place switch
        {
            >= SnowGlobeTypeThree.Covetous => 1075440 + ((int)_place - 4),
            _                              => 1075294 + (int)_place
        };

    private void Deserialize(IGenericReader reader, int version)
    {
        _place = (SnowGlobeTypeThree)reader.ReadEncodedInt();
    }
}
