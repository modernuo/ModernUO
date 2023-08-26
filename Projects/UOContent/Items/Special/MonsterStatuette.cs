using System;
using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items;

public enum MonsterStatuetteType
{
    Crocodile,
    Daemon,
    Dragon,
    EarthElemental,
    Ettin,
    Gargoyle,
    Gorilla,
    Lich,
    Lizardman,
    Ogre,
    Orc,
    Ratman,
    Skeleton,
    Troll,
    Cow,
    Zombie,
    Llama,
    Ophidian,
    Reaper,
    Mongbat,
    Gazer,
    FireElemental,
    Wolf,
    PhillipsWoodenSteed,
    Seahorse,
    Harrower,
    Efreet,
    Slime,
    PlagueBeast,
    RedDeath,
    Spider,
    OphidianArchMage,
    OphidianWarrior,
    OphidianKnight,
    OphidianMage,
    DreadHorn,
    Minotaur,
    BlackCat,
    HalloweenGhoul,
    Santa
}

public class MonsterStatuetteInfo
{
    private static readonly MonsterStatuetteInfo[] m_Table =
    {
        /* Crocodile */ new(1041249, 0x20DA, 660),
        /* Daemon */ new(1041250, 0x20D3, 357),
        /* Dragon */ new(1041251, 0x20D6, 362),
        /* EarthElemental */ new(1041252, 0x20D7, 268),
        /* Ettin */ new(1041253, 0x20D8, 367),
        /* Gargoyle */ new(1041254, 0x20D9, 372),
        /* Gorilla */ new(1041255, 0x20F5, 158),
        /* Lich */ new(1041256, 0x20F8, 1001),
        /* Lizardman */ new(1041257, 0x20DE, 417),
        /* Ogre */ new(1041258, 0x20DF, 427),
        /* Orc */ new(1041259, 0x20E0, 1114),
        /* Ratman */ new(1041260, 0x20E3, 437),
        /* Skeleton */ new(1041261, 0x20E7, 1165),
        /* Troll */ new(1041262, 0x20E9, 461),
        /* Cow */ new(1041263, 0x2103, 120),
        /* Zombie */ new(1041264, 0x20EC, 471),
        /* Llama */ new(1041265, 0x20F6, 1011),
        /* Ophidian */ new(1049742, 0x2133, 634),
        /* Reaper */ new(1049743, 0x20FA, 442),
        /* Mongbat */ new(1049744, 0x20F9, 422),
        /* Gazer */ new(1049768, 0x20F4, 377),
        /* FireElemental */ new(1049769, 0x20F3, 838),
        /* Wolf */ new(1049770, 0x2122, 229),
        /* Phillip's Steed */ new(1063488, 0x3FFE, 168),
        /* Seahorse */ new(1070819, 0x25BA, 138),
        /* Harrower */ new(1080520, 0x25BB, new[] { 0x289, 0x28A, 0x28B }),
        /* Efreet */ new(1080521, 0x2590, 0x300),
        /* Slime */ new(1015246, 0x20E8, 456),
        /* PlagueBeast */ new(1029747, 0x2613, 0x1BF),
        /* RedDeath */ new(1094932, 0x2617, Array.Empty<int>()),
        /* Spider */ new(1029668, 0x25C4, 1170),
        /* OphidianArchMage */ new(1029641, 0x25A9, 639),
        /* OphidianWarrior */ new(1029645, 0x25AD, 634),
        /* OphidianKnight */ new(1029642, 0x25aa, 634),
        /* OphidianMage */ new(1029643, 0x25ab, 639),
        /* DreadHorn */ new(1031651, 0x2D83, 0xA8),
        /* Minotaur */ new(1031657, 0x2D89, 0x596),
        /* Black Cat */ new(1096928, 0x4688, 0x69),
        /* HalloweenGhoul */ new(1076782, 0x2109, 0x482),
        /* Santa */ new(1097968, 0x4A98, 0x669)
    };

    public MonsterStatuetteInfo(int labelNumber, int itemID, int baseSoundID)
    {
        LabelNumber = labelNumber;
        ItemID = itemID;
        Sounds = new[] { baseSoundID, baseSoundID + 1, baseSoundID + 2, baseSoundID + 3, baseSoundID + 4 };
    }

    public MonsterStatuetteInfo(int labelNumber, int itemID, int[] sounds)
    {
        LabelNumber = labelNumber;
        ItemID = itemID;
        Sounds = sounds;
    }

    public int LabelNumber { get; }

    public int ItemID { get; }

    public int[] Sounds { get; }

    public static MonsterStatuetteInfo GetInfo(MonsterStatuetteType type)
    {
        var v = (int)type;

        if (v < 0 || v >= m_Table.Length)
        {
            v = 0;
        }

        return m_Table[v];
    }
}

[SerializationGenerator(0, false)]
public partial class MonsterStatuette : Item, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _turnedOn;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public MonsterStatuette(MonsterStatuetteType type = MonsterStatuetteType.Crocodile) : base(
        MonsterStatuetteInfo.GetInfo(type).ItemID
    )
    {
        LootType = LootType.Blessed;

        _type = type;

        Hue = GetStatuetteHue(type);
    }

    public static int GetStatuetteHue(MonsterStatuetteType type, int fallback = 0) =>
        type switch
        {
            MonsterStatuetteType.Slime          => Utility.RandomSlimeHue(),
            MonsterStatuetteType.RedDeath       => 0x21,
            MonsterStatuetteType.HalloweenGhoul => 0xF4,
            _                                   => fallback
        };

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public MonsterStatuetteType Type
    {
        get => _type;
        set
        {
            _type = value;
            ItemID = MonsterStatuetteInfo.GetInfo(_type).ItemID;

            Hue = GetStatuetteHue(_type, Hue);

            InvalidateProperties();
            this.MarkDirty();
        }
    }

    public override int LabelNumber => MonsterStatuetteInfo.GetInfo(_type).LabelNumber;

    public override double DefaultWeight => 1.0;

    public override bool HandlesOnMovement => _turnedOn && IsLockedDown;

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (_turnedOn && IsLockedDown && (!m.Hidden || m.AccessLevel == AccessLevel.Player) &&
            Utility.InRange(m.Location, Location, 2) && !Utility.InRange(oldLocation, Location, 2))
        {
            var sounds = MonsterStatuetteInfo.GetInfo(_type).Sounds;

            if (sounds.Length > 0)
            {
                Effects.PlaySound(Location, Map, sounds.RandomElement());
            }
        }

        base.OnMovement(m, oldLocation);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Core.ML && IsRewardItem)
        {
            list.Add(RewardSystem.GetRewardYearLabel(this, new object[] { _type })); // X Year Veteran Reward
        }

        if (_turnedOn)
        {
            list.Add(502695); // turned on
        }
        else
        {
            list.Add(502696); // turned off
        }
    }

    public bool IsOwner(Mobile mob) => BaseHouse.FindHouseAt(this)?.IsOwner(mob) == true;

    public override void OnDoubleClick(Mobile from)
    {
        if (IsOwner(from))
        {
            var onOffGump = new OnOffGump(this);
            from.SendGump(onOffGump);
        }
        else
        {
            from.SendLocalizedMessage(502691); // You must be the owner to use this.
        }
    }

    private class OnOffGump : Gump
    {
        private readonly MonsterStatuette m_Statuette;

        public OnOffGump(MonsterStatuette statuette) : base(150, 200)
        {
            m_Statuette = statuette;

            AddBackground(0, 0, 300, 150, 0xA28);

            AddHtmlLocalized(45, 20, 300, 35, statuette.TurnedOn ? 1011035 : 1011034); // [De]Activate this item

            AddButton(40, 53, 0xFA5, 0xFA7, 1);
            AddHtmlLocalized(80, 55, 65, 35, 1011036); // OKAY

            AddButton(150, 53, 0xFA5, 0xFA7, 0);
            AddHtmlLocalized(190, 55, 100, 35, 1011012); // CANCEL
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;

            if (info.ButtonID == 1)
            {
                var newValue = !m_Statuette.TurnedOn;
                m_Statuette.TurnedOn = newValue;

                if (newValue && !m_Statuette.IsLockedDown)
                {
                    from.SendLocalizedMessage(502693); // Remember, this only works when locked down.
                }
            }
            else
            {
                from.SendLocalizedMessage(502694); // Cancelled action.
            }
        }
    }
}
