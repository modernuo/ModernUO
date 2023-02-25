using System;
using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Factions;
using Server.Mobiles;
using Server.Regions;

namespace Server.Items;

[SerializationGenerator(1, false)]
public partial class RepairDeed : Item
{
    public enum RepairSkillType
    {
        Smithing,
        Tailoring,
        Tinkering,
        Carpentry,
        Fletching
    }

    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private RepairSkillType _skill;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _crafter;

    [Constructible]
    public RepairDeed(RepairSkillType skill, double level, bool normalizeLevel) : this(
        skill,
        level,
        null,
        normalizeLevel
    )
    {
    }

    [Constructible]
    public RepairDeed(
        RepairSkillType skill = RepairSkillType.Smithing, double level = 100.0,
        Mobile crafter = null, bool normalizeLevel = true
    ) : base(0x14F0)
    {
        if (normalizeLevel)
        {
            SkillLevel = (int)(level / 10) * 10;
        }
        else
        {
            SkillLevel = level;
        }

        _skill = skill;
        _crafter = crafter?.RawName;
        Hue = 0x1BC;
        LootType = LootType.Blessed;
    }

    public override bool DisplayLootType => false;

    [CommandProperty(AccessLevel.GameMaster)]
    [SerializableProperty(1)]
    public double SkillLevel
    {
        get => _skillLevel;
        set
        {
            _skillLevel = Math.Clamp(value, 0, 120.0);
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    public override void AddNameProperty(IPropertyList list)
    {
        // A repair service contract from ~1_SKILL_TITLE~ ~2_SKILL_NAME~.
        list.Add(1061133, $"{GetSkillTitle(_skillLevel)}\t{RepairSkillInfo.GetInfo(_skill).Name}");
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_crafter != null)
        {
            list.Add(1050043, _crafter); // crafted by ~1_NAME~
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        if (Deleted || !from.CanSee(this))
        {
            return;
        }

        // A repair service contract from ~1_SKILL_TITLE~ ~2_SKILL_NAME~.
        LabelTo(from, 1061133, $"{GetSkillTitle(_skillLevel)}\t{RepairSkillInfo.GetInfo(_skill).Name}");

        if (_crafter != null)
        {
            LabelTo(from, 1050043, _crafter); // crafted by ~1_NAME~
        }
    }

    private static TextDefinition GetSkillTitle(double skillLevel)
    {
        var skill = (int)(skillLevel / 10);

        if (skill >= 11)
        {
            return 1062008 + skill - 11;
        }

        if (skill >= 5)
        {
            return 1061123 + skill - 5;
        }

        return skill switch
        {
            4 => "a Novice",
            3 => "a Neophyte",
            _ => "a Newbie"
        };
    }

    public static RepairSkillType GetTypeFor(CraftSystem s)
    {
        for (var i = 0; i < RepairSkillInfo.Table.Length; i++)
        {
            if (RepairSkillInfo.Table[i].System == s)
            {
                return (RepairSkillType)i;
            }
        }

        return RepairSkillType.Smithing;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Check(from))
        {
            Repair.Do(from, RepairSkillInfo.GetInfo(_skill).System, this);
        }
    }

    public bool Check(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1047012); // The contract must be in your backpack to use it.
            return false;
        }

        if (!VerifyRegion(from))
        {
            RepairSkillInfo.GetInfo(_skill).NotNearbyMessage.SendMessageTo(from);
            return false;
        }

        return true;
    }

    public bool VerifyRegion(Mobile m) => m.Region.IsPartOf<TownRegion>() &&
                                          Faction.IsNearType(m, RepairSkillInfo.GetInfo(_skill).NearbyTypes, 6);

    private void Deserialize(IGenericReader reader, int version)
    {
        _skill = (RepairSkillType)reader.ReadInt();
        _skillLevel = reader.ReadDouble();
        Timer.DelayCall((item, crafter) => item._crafter = crafter?.RawName, this, reader.ReadEntity<Mobile>());
    }

    private class RepairSkillInfo
    {
        public RepairSkillInfo(
            CraftSystem system, Type[] nearbyTypes, TextDefinition notNearbyMessage,
            TextDefinition name
        )
        {
            System = system;
            NearbyTypes = nearbyTypes;
            NotNearbyMessage = notNearbyMessage;
            Name = name;
        }

        public RepairSkillInfo(CraftSystem system, Type nearbyType, TextDefinition notNearbyMessage, TextDefinition name)
            : this(system, new[] { nearbyType }, notNearbyMessage, name)
        {
        }

        public TextDefinition NotNearbyMessage { get; }

        public TextDefinition Name { get; }

        public CraftSystem System { get; }

        public Type[] NearbyTypes { get; }

        public static RepairSkillInfo[] Table { get; } =
        {
            new(DefBlacksmithy.CraftSystem, typeof(Blacksmith), 1047013, 1023015),
            new(DefTailoring.CraftSystem, typeof(Tailor), 1061132, 1022981),
            new(DefTinkering.CraftSystem, typeof(Tinker), 1061166, 1022983),
            new(DefCarpentry.CraftSystem, typeof(Carpenter), 1061135, 1060774),
            new(DefBowFletching.CraftSystem, typeof(Bowyer), 1061134, 1023005)
        };

        public static RepairSkillInfo GetInfo(RepairSkillType type)
        {
            var v = (int)type;

            if (v < 0 || v >= Table.Length)
            {
                v = 0;
            }

            return Table[v];
        }
    }
}
