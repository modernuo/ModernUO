using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Utilities;

namespace Server.Engines.Stealables;

public static class StealableArtifacts
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(StealableArtifacts));

    private static bool _enabled;
    private static Type[] _typesOfEntries;
    private static StealableInstance[] _artifacts;

    private static Timer _respawnTimer;
    private static Dictionary<Item, StealableInstance> _table;

    public static void Configure()
    {
        GenericPersistence.Register("StealableArtifacts", Serialize, Deserialize);
    }

    private static void RemoveStealableArtifacts()
    {
        _enabled = false;
        _respawnTimer?.Stop();
        _respawnTimer = null;

        if (_artifacts?.Length > 0)
        {
            foreach (var si in _artifacts)
            {
                var item = si.Item;
                if (item.Deleted == false && !item.Movable && item.Parent == null)
                {
                    item.Delete();
                }
            }
        }

        _artifacts = null;
    }

    private static void CreateStealableArtifacts()
    {
        _enabled = true;

        _artifacts = new StealableInstance[Entries.Length];
        _table ??= new Dictionary<Item, StealableInstance>(Entries.Length);

        for (var i = 0; i < Entries.Length; i++)
        {
            _artifacts[i] = new StealableInstance(Entries[i]);
        }

        _respawnTimer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromMinutes(15.0), CheckRespawn);
    }

    public static StealableEntry[] Entries { get; } =
    {
        // Doom - Artifact rarity 1
        new(Map.Malas, new Point3D(317, 56, -1), 72, 108, typeof(RockArtifact)),
        new(Map.Malas, new Point3D(360, 31, 8), 72, 108, typeof(SkullCandleArtifact)),
        new(Map.Malas, new Point3D(369, 372, -1), 72, 108, typeof(BottleArtifact)),
        new(Map.Malas, new Point3D(378, 372, 0), 72, 108, typeof(DamagedBooksArtifact)),
        // Doom - Artifact rarity 2
        new(Map.Malas, new Point3D(432, 16, -1), 144, 216, typeof(StretchedHideArtifact)),
        new(Map.Malas, new Point3D(489, 9, 0), 144, 216, typeof(BrazierArtifact)),
        // Doom - Artifact rarity 3
        new(Map.Malas, new Point3D(471, 96, -1), 288, 432, typeof(LampPostArtifact), GetLampPostHue()),
        new(Map.Malas, new Point3D(421, 198, 2), 288, 432, typeof(BooksNorthArtifact)),
        new(Map.Malas, new Point3D(431, 189, -1), 288, 432, typeof(BooksWestArtifact)),
        new(Map.Malas, new Point3D(435, 196, -1), 288, 432, typeof(BooksFaceDownArtifact)),
        // Doom - Artifact rarity 5
        new(Map.Malas, new Point3D(447, 9, 8), 1152, 1728, typeof(StuddedLeggingsArtifact)),
        new(Map.Malas, new Point3D(423, 28, 0), 1152, 1728, typeof(EggCaseArtifact)),
        new(Map.Malas, new Point3D(347, 44, 4), 1152, 1728, typeof(SkinnedGoatArtifact)),
        new(Map.Malas, new Point3D(497, 57, -1), 1152, 1728, typeof(GruesomeStandardArtifact)),
        new(Map.Malas, new Point3D(381, 375, 11), 1152, 1728, typeof(BloodyWaterArtifact)),
        new(Map.Malas, new Point3D(489, 369, 2), 1152, 1728, typeof(TarotCardsArtifact)),
        new(Map.Malas, new Point3D(497, 369, 5), 1152, 1728, typeof(BackpackArtifact)),
        // Doom - Artifact rarity 7
        new(Map.Malas, new Point3D(475, 23, 4), 4608, 6912, typeof(StuddedTunicArtifact)),
        new(Map.Malas, new Point3D(423, 28, 0), 4608, 6912, typeof(CocoonArtifact)),
        // Doom - Artifact rarity 8
        new(Map.Malas, new Point3D(354, 36, -1), 9216, 13824, typeof(SkinnedDeerArtifact)),
        // Doom - Artifact rarity 9
        new(Map.Malas, new Point3D(433, 11, -1), 18432, 27648, typeof(SaddleArtifact)),
        new(Map.Malas, new Point3D(403, 31, 4), 18432, 27648, typeof(LeatherTunicArtifact)),
        // Doom - Artifact rarity 10
        new(Map.Malas, new Point3D(257, 70, -2), 36864, 55296, typeof(ZyronicClaw)),
        new(Map.Malas, new Point3D(354, 176, 7), 36864, 55296, typeof(TitansHammer)),
        new(Map.Malas, new Point3D(369, 389, -1), 36864, 55296, typeof(BladeOfTheRighteous)),
        new(Map.Malas, new Point3D(467, 92, 4), 36864, 55296, typeof(InquisitorsResolution)),
        // Doom - Artifact rarity 12
        new(Map.Malas, new Point3D(487, 364, -1), 147456, 221184, typeof(RuinedPaintingArtifact)),

        // Yomotsu Mines - Artifact rarity 1
        new(Map.Malas, new Point3D(18, 110, -1), 72, 108, typeof(Basket1Artifact)),
        new(Map.Malas, new Point3D(66, 114, -1), 72, 108, typeof(Basket2Artifact)),
        // Yomotsu Mines - Artifact rarity 2
        new(Map.Malas, new Point3D(63, 12, 11), 144, 216, typeof(Basket4Artifact)),
        new(Map.Malas, new Point3D(5, 29, -1), 144, 216, typeof(Basket5NorthArtifact)),
        new(Map.Malas, new Point3D(30, 81, 3), 144, 216, typeof(Basket5WestArtifact)),
        // Yomotsu Mines - Artifact rarity 3
        new(Map.Malas, new Point3D(115, 7, -1), 288, 432, typeof(Urn1Artifact)),
        new(Map.Malas, new Point3D(85, 13, -1), 288, 432, typeof(Urn2Artifact)),
        new(Map.Malas, new Point3D(110, 53, -1), 288, 432, typeof(Sculpture1Artifact)),
        new(Map.Malas, new Point3D(108, 37, -1), 288, 432, typeof(Sculpture2Artifact)),
        new(Map.Malas, new Point3D(121, 14, -1), 288, 432, typeof(TeapotNorthArtifact)),
        new(Map.Malas, new Point3D(121, 115, -1), 288, 432, typeof(TeapotWestArtifact)),
        new(Map.Malas, new Point3D(84, 40, -1), 288, 432, typeof(TowerLanternArtifact)),
        // Yomotsu Mines - Artifact rarity 9
        new(Map.Malas, new Point3D(94, 7, -1), 18432, 27648, typeof(ManStatuetteSouthArtifact)),

        // Fan Dancer's Dojo - Artifact rarity 1
        new(Map.Malas, new Point3D(113, 640, -2), 72, 108, typeof(Basket3NorthArtifact)),
        new(Map.Malas, new Point3D(102, 355, -1), 72, 108, typeof(Basket3WestArtifact)),
        // Fan Dancer's Dojo - Artifact rarity 2
        new(Map.Malas, new Point3D(99, 370, -1), 144, 216, typeof(Basket6Artifact)),
        new(Map.Malas, new Point3D(100, 357, -1), 144, 216, typeof(ZenRock1Artifact)),
        // Fan Dancer's Dojo - Artifact rarity 3
        new(Map.Malas, new Point3D(73, 473, -1), 288, 432, typeof(FanNorthArtifact)),
        new(Map.Malas, new Point3D(99, 372, -1), 288, 432, typeof(FanWestArtifact)),
        new(Map.Malas, new Point3D(92, 326, -1), 288, 432, typeof(BowlsVerticalArtifact)),
        new(Map.Malas, new Point3D(97, 470, -1), 288, 432, typeof(ZenRock2Artifact)),
        new(Map.Malas, new Point3D(103, 691, -1), 288, 432, typeof(ZenRock3Artifact)),
        // Fan Dancer's Dojo - Artifact rarity 4
        new(Map.Malas, new Point3D(103, 336, 4), 576, 864, typeof(Painting1NorthArtifact)),
        new(Map.Malas, new Point3D(59, 381, 4), 576, 864, typeof(Painting1WestArtifact)),
        new(Map.Malas, new Point3D(84, 401, 2), 576, 864, typeof(Painting2NorthArtifact)),
        new(Map.Malas, new Point3D(59, 392, 2), 576, 864, typeof(Painting2WestArtifact)),
        new(Map.Malas, new Point3D(107, 483, -1), 576, 864, typeof(TripleFanNorthArtifact)),
        new(Map.Malas, new Point3D(50, 475, -1), 576, 864, typeof(TripleFanWestArtifact)),
        new(Map.Malas, new Point3D(107, 460, -1), 576, 864, typeof(BowlArtifact)),
        new(Map.Malas, new Point3D(90, 502, -1), 576, 864, typeof(CupsArtifact)),
        new(Map.Malas, new Point3D(107, 688, -1), 576, 864, typeof(BowlsHorizontalArtifact)),
        new(Map.Malas, new Point3D(112, 676, -1), 576, 864, typeof(SakeArtifact)),
        // Fan Dancer's Dojo - Artifact rarity 5
        new(Map.Malas, new Point3D(135, 614, -1), 1152, 1728, typeof(SwordDisplay1NorthArtifact)),
        new(Map.Malas, new Point3D(50, 482, -1), 1152, 1728, typeof(SwordDisplay1WestArtifact)),
        new(Map.Malas, new Point3D(119, 672, -1), 1152, 1728, typeof(Painting3Artifact)),
        // Fan Dancer's Dojo - Artifact rarity 6
        new(Map.Malas, new Point3D(90, 326, -1), 2304, 3456, typeof(Painting4NorthArtifact)),
        new(Map.Malas, new Point3D(99, 354, -1), 2304, 3456, typeof(Painting4WestArtifact)),
        new(Map.Malas, new Point3D(179, 652, -1), 2304, 3456, typeof(SwordDisplay2NorthArtifact)),
        new(Map.Malas, new Point3D(118, 627, -1), 2304, 3456, typeof(SwordDisplay2WestArtifact)),
        // Fan Dancer's Dojo - Artifact rarity 7
        new(Map.Malas, new Point3D(90, 483, -1), 4608, 6912, typeof(FlowersArtifact)),
        // Fan Dancer's Dojo - Artifact rarity 8
        new(Map.Malas, new Point3D(71, 562, -1), 9216, 13824, typeof(DolphinLeftArtifact)),
        new(Map.Malas, new Point3D(102, 677, -1), 9216, 13824, typeof(DolphinRightArtifact)),
        new(Map.Malas, new Point3D(61, 499, 0), 9216, 13824, typeof(SwordDisplay3SouthArtifact)),
        new(Map.Malas, new Point3D(182, 669, -1), 9216, 13824, typeof(SwordDisplay3EastArtifact)),
        new(Map.Malas, new Point3D(162, 647, -1), 9216, 13824, typeof(SwordDisplay4WestArtifact)),
        new(Map.Malas, new Point3D(124, 624, 0), 9216, 13824, typeof(Painting5NorthArtifact)),
        new(Map.Malas, new Point3D(146, 649, 2), 9216, 13824, typeof(Painting5WestArtifact)),
        // Fan Dancer's Dojo - Artifact rarity 9
        new(Map.Malas, new Point3D(100, 488, -1), 18432, 27648, typeof(SwordDisplay4NorthArtifact)),
        new(Map.Malas, new Point3D(175, 606, 0), 18432, 27648, typeof(SwordDisplay5NorthArtifact)),
        new(Map.Malas, new Point3D(157, 608, -1), 18432, 27648, typeof(SwordDisplay5WestArtifact)),
        new(Map.Malas, new Point3D(187, 643, 1), 18432, 27648, typeof(Painting6NorthArtifact)),
        new(Map.Malas, new Point3D(146, 623, 1), 18432, 27648, typeof(Painting6WestArtifact)),
        new(Map.Malas, new Point3D(178, 629, -1), 18432, 27648, typeof(ManStatuetteEastArtifact))
    };

    public static Type[] TypesOfEntries
    {
        get
        {
            if (_typesOfEntries == null)
            {
                _typesOfEntries = new Type[Entries.Length];

                for (var i = 0; i < Entries.Length; i++)
                {
                    _typesOfEntries[i] = Entries[i].Type;
                }
            }

            return _typesOfEntries;
        }
    }

    private static int GetLampPostHue() =>
        Utility.RandomDouble() < 0.9 ? 0 : Utility.RandomList(0x455, 0x47E, 0x482, 0x486, 0x48F, 0x4F2, 0x58C, 0x66C);

    public static void Initialize()
    {
        CommandSystem.Register("GenStealArties", AccessLevel.Administrator, GenStealArties_OnCommand);
        CommandSystem.Register("RemoveStealArties", AccessLevel.Administrator, RemoveStealArties_OnCommand);
    }

    [Usage("GenStealArties"), Description("Generates the stealable artifacts spawner.")]
    private static void GenStealArties_OnCommand(CommandEventArgs args)
    {
        var from = args.Mobile;

        if (_enabled)
        {
            from.SendMessage("Stealable artifacts spawner already present.");
            return;
        }

        CreateStealableArtifacts();
        from.SendMessage("Stealable artifacts spawner generated.");
    }

    [Usage("RemoveStealArties")]
    [Description("Removes the stealable artifacts spawner and every not yet stolen stealable artifacts.")]
    private static void RemoveStealArties_OnCommand(CommandEventArgs args)
    {
        var from = args.Mobile;

        if (!_enabled)
        {
            from.SendMessage("Stealable artifacts spawner not present.");
            return;
        }

        RemoveStealableArtifacts();
        from.SendMessage("Stealable artifacts spawner removed.");
    }

    public static StealableInstance GetStealableInstance(Item item)
    {
        if (_enabled && _table.TryGetValue(item, out var value))
        {
            return value;
        }

        return null;
    }

    public static void CheckRespawn()
    {
        foreach (var si in _artifacts)
        {
            si.CheckRespawn();
        }
    }

    private static void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(1); // version

        writer.Write(_enabled);

        if (_enabled)
        {
            writer.WriteEncodedInt(_artifacts.Length);

            for (var i = 0; i < _artifacts.Length; i++)
            {
                var si = _artifacts[i];

                writer.Write(si.Item);
                writer.WriteDeltaTime(si.NextRespawn);
            }
        }
    }

    private static void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        _enabled = version > 0 && reader.ReadBool();

        if (_enabled)
        {
            _artifacts = new StealableInstance[Entries.Length];
            _table = new Dictionary<Item, StealableInstance>(Entries.Length);

            var length = reader.ReadEncodedInt();

            for (var i = 0; i < length; i++)
            {
                var item = reader.ReadEntity<Item>();
                var nextRespawn = reader.ReadDeltaTime();

                if (i < _artifacts.Length)
                {
                    var si = new StealableInstance(Entries[i], item, nextRespawn);
                    _artifacts[i] = si;

                    if (si.Item != null)
                    {
                        _table[si.Item] = si;
                    }
                }
            }

            for (var i = length; i < Entries.Length; i++)
            {
                _artifacts[i] = new StealableInstance(Entries[i]);
            }

            _enabled = _artifacts.Length > 0;

            if (_enabled)
            {
                _respawnTimer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromMinutes(15.0), CheckRespawn);
            }
            else
            {
                _artifacts = null;
            }
        }
    }

    public class StealableEntry
    {
        public StealableEntry(Map map, Point3D location, int minDelay, int maxDelay, Type type, int hue = 0)
        {
            Map = map;
            Location = location;
            MinDelay = minDelay;
            MaxDelay = maxDelay;
            Type = type;
            Hue = hue;
        }

        public Map Map { get; }

        public Point3D Location { get; }

        public int MinDelay { get; }

        public int MaxDelay { get; }

        public Type Type { get; }

        public int Hue { get; }

        public Item CreateInstance()
        {
            try
            {
                var item = Type.CreateInstance<Item>();

                if (Hue > 0)
                {
                    item.Hue = Hue;
                }

                item.Movable = false;
                item.MoveToWorld(Location, Map);

                return item;
            }
            catch (Exception e)
            {
                logger.Warning(e, $"Failed to construct stealable artifact: {Type.FullName}");
                return null;
            }
        }
    }

    public class StealableInstance
    {
        private Item m_Item;

        public StealableInstance(StealableEntry entry) : this(entry, null, Core.Now)
        {
        }

        public StealableInstance(StealableEntry entry, Item item, DateTime nextRespawn)
        {
            m_Item = item;
            NextRespawn = nextRespawn;
            Entry = entry;
        }

        public StealableEntry Entry { get; }

        public Item Item
        {
            get => m_Item;
            set
            {
                if (m_Item != null && value == null)
                {
                    var delay = Utility.RandomMinMax(Entry.MinDelay, Entry.MaxDelay);
                    NextRespawn = Core.Now + TimeSpan.FromMinutes(delay);
                }

                if (_enabled)
                {
                    if (m_Item != null)
                    {
                        _table.Remove(m_Item);
                    }

                    if (value != null)
                    {
                        _table[value] = this;
                    }
                }

                m_Item = value;
            }
        }

        public DateTime NextRespawn { get; set; }

        public void CheckRespawn()
        {
            if (Item != null && (Item.Deleted || Item.Movable || Item.Parent != null))
            {
                Item = null;
            }

            if (Item == null && Core.Now >= NextRespawn)
            {
                Item = Entry.CreateInstance();
            }
        }
    }

    [ManualDirtyChecking]
    [TypeAlias("Server.Items.StealableArtifactsSpawner")]
    [Obsolete("Deprecated in favor of the static system. Only used for legacy deserialization")]
    public class StealableArtifactsSpawner : Item
    {
        private StealableArtifactsSpawner()
        {
            Delete();
        }

        public StealableArtifactsSpawner(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            StealableArtifacts.Deserialize(reader);

            Timer.DelayCall(Delete);
        }
    }
}
