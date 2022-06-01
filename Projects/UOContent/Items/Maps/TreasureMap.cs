using System;
using System.Collections.Generic;
using System.IO;
using Server.ContextMenus;
using Server.Engines.Harvest;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using Server.Utilities;

namespace Server.Items
{
    public class TreasureMap : MapItem
    {
        public const double LootChance = 0.01; // 1% chance to appear as loot

        private static Point2D[] m_Locations;
        private static Point2D[] m_HavenLocations;

        private static readonly Type[][] m_SpawnTypes =
        {
            new[] { typeof(HeadlessOne), typeof(Skeleton) },
            new[] { typeof(Mongbat), typeof(Ratman), typeof(HeadlessOne), typeof(Skeleton), typeof(Zombie) },
            new[] { typeof(OrcishMage), typeof(Gargoyle), typeof(Gazer), typeof(HellHound), typeof(EarthElemental) },
            new[] { typeof(Lich), typeof(OgreLord), typeof(DreadSpider), typeof(AirElemental), typeof(FireElemental) },
            new[] { typeof(DreadSpider), typeof(LichLord), typeof(Daemon), typeof(ElderGazer), typeof(OgreLord) },
            new[] { typeof(LichLord), typeof(Daemon), typeof(ElderGazer), typeof(PoisonElemental), typeof(BloodElemental) },
            new[] { typeof(AncientWyrm), typeof(Balron), typeof(BloodElemental), typeof(PoisonElemental), typeof(Titan) }
        };

        private bool m_Completed;
        private Mobile m_CompletedBy;
        private Mobile m_Decoder;
        private int m_Level;
        private Map m_Map;

        [Constructible]
        public TreasureMap(int level, Map map)
        {
            m_Level = level;
            m_Map = map;

            if (level == 0)
            {
                ChestLocation = GetRandomHavenLocation();
            }
            else
            {
                ChestLocation = GetRandomLocation();
            }

            Width = 300;
            Height = 300;

            var width = 600;
            var height = 600;

            var x1 = ChestLocation.X - Utility.RandomMinMax(width / 4, width / 4 * 3);
            var y1 = ChestLocation.Y - Utility.RandomMinMax(height / 4, height / 4 * 3);

            if (x1 < 0)
            {
                x1 = 0;
            }

            if (y1 < 0)
            {
                y1 = 0;
            }

            var x2 = x1 + width;
            var y2 = y1 + height;

            if (x2 >= 5120)
            {
                x2 = 5119;
            }

            if (y2 >= 4096)
            {
                y2 = 4095;
            }

            x1 = x2 - width;
            y1 = y2 - height;

            Bounds = new Rectangle2D(x1, y1, width, height);
            Protected = true;

            AddWorldPin(ChestLocation.X, ChestLocation.Y);
        }

        public TreasureMap(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level
        {
            get => m_Level;
            set
            {
                m_Level = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Completed
        {
            get => m_Completed;
            set
            {
                m_Completed = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile CompletedBy
        {
            get => m_CompletedBy;
            set
            {
                m_CompletedBy = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Decoder
        {
            get => m_Decoder;
            set
            {
                m_Decoder = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map ChestMap
        {
            get => m_Map;
            set
            {
                m_Map = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D ChestLocation { get; set; }

        public override int LabelNumber
        {
            get
            {
                if (m_Decoder != null)
                {
                    if (m_Level == 6)
                    {
                        return 1063453;
                    }

                    return 1041516 + m_Level;
                }

                if (m_Level == 6)
                {
                    return 1063452;
                }

                return 1041510 + m_Level;
            }
        }

        public static Point2D GetRandomLocation()
        {
            if (m_Locations == null)
            {
                LoadLocations();
            }

            return m_Locations?.RandomElement() ?? Point2D.Zero;
        }

        public static Point2D GetRandomHavenLocation()
        {
            if (m_HavenLocations == null)
            {
                LoadLocations();
            }

            return m_HavenLocations?.RandomElement() ?? Point2D.Zero;
        }

        private static void LoadLocations()
        {
            var filePath = Path.Combine(Core.BaseDirectory, "Data/treasure.cfg");

            var list = new List<Point2D>();
            var havenList = new List<Point2D>();

            if (File.Exists(filePath))
            {
                using var ip = new StreamReader(filePath);
                string line;

                while ((line = ip.ReadLine()) != null)
                {
                    try
                    {
                        var split = line.Split(' ');

                        int x = Convert.ToInt32(split[0]), y = Convert.ToInt32(split[1]);

                        var loc = new Point2D(x, y);
                        list.Add(loc);

                        if (IsInHavenIsland(loc))
                        {
                            havenList.Add(loc);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            m_Locations = list.ToArray();
            m_HavenLocations = havenList.ToArray();
        }

        public static bool IsInHavenIsland(IPoint2D loc) => loc.X >= 3314 && loc.X <= 3814 && loc.Y >= 2345 && loc.Y <= 3095;

        public static BaseCreature Spawn(int level, Point3D p, bool guardian)
        {
            if (level >= 0 && level < m_SpawnTypes.Length)
            {
                BaseCreature bc;

                try
                {
                    bc = m_SpawnTypes[level].RandomElement().CreateInstance<BaseCreature>();
                }
                catch
                {
                    return null;
                }

                bc.Home = p;
                bc.RangeHome = 5;

                if (guardian && level == 0)
                {
                    bc.Name = "a chest guardian";
                    bc.Hue = 0x835;
                }

                return bc;
            }

            return null;
        }

        public static BaseCreature Spawn(int level, Point3D p, Map map, Mobile target, bool guardian)
        {
            if (map == null)
            {
                return null;
            }

            var c = Spawn(level, p, guardian);

            if (c != null)
            {
                var spawned = false;

                for (var i = 0; !spawned && i < 10; ++i)
                {
                    var x = p.X - 3 + Utility.Random(7);
                    var y = p.Y - 3 + Utility.Random(7);

                    if (map.CanSpawnMobile(x, y, p.Z))
                    {
                        c.MoveToWorld(new Point3D(x, y, p.Z), map);
                        spawned = true;
                    }
                    else
                    {
                        var z = map.GetAverageZ(x, y);

                        if (map.CanSpawnMobile(x, y, z))
                        {
                            c.MoveToWorld(new Point3D(x, y, z), map);
                            spawned = true;
                        }
                    }
                }

                if (!spawned)
                {
                    c.Delete();
                    return null;
                }

                if (target != null)
                {
                    c.Combatant = target;
                }

                return c;
            }

            return null;
        }

        public static bool HasDiggingTool(Mobile m)
        {
            if (m.Backpack == null)
            {
                return false;
            }

            foreach (var tool in m.Backpack.FindItemsByType<BaseHarvestTool>())
            {
                if (tool.HarvestSystem == Mining.System)
                {
                    return true;
                }
            }

            return false;
        }

        public void OnBeginDig(Mobile from)
        {
            if (m_Completed)
            {
                from.SendLocalizedMessage(503028); // The treasure for this map has already been found.
            }
            else if (m_Level == 0 && !CheckYoung(from))
            {
                from.SendLocalizedMessage(1046447); // Only a young player may use this treasure map.
            }
            /*
            else if (from != m_Decoder)
            {
              from.SendLocalizedMessage( 503016 ); // Only the person who decoded this map may actually dig up the treasure.
            }
            */
            else if (m_Decoder != from && !HasRequiredSkill(from))
            {
                from.SendLocalizedMessage(
                    503031
                ); // You did not decode this map and have no clue where to look for the treasure.
            }
            else if (!from.CanBeginAction<TreasureMap>())
            {
                from.SendLocalizedMessage(503020); // You are already digging treasure.
            }
            else if (from.Map != m_Map)
            {
                from.SendLocalizedMessage(1010479); // You seem to be in the right place, but may be on the wrong facet!
            }
            else
            {
                from.SendLocalizedMessage(503033); // Where do you wish to dig?
                from.Target = new DigTarget(this);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            if (!m_Completed && m_Decoder == null)
            {
                Decode(from);
            }
            else
            {
                DisplayTo(from);
            }
        }

        private bool CheckYoung(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }

            if (from is PlayerMobile mobile && mobile.Young)
            {
                return true;
            }

            if (from == Decoder)
            {
                Level = 1;
                from.SendLocalizedMessage(1046446); // This is now a level one treasure map.
                return true;
            }

            return false;
        }

        private double GetMinSkillLevel()
        {
            return m_Level switch
            {
                1 => -3.0,
                2 => 41.0,
                3 => 51.0,
                4 => 61.0,
                5 => 70.0,
                6 => 70.0,
                _ => 0.0
            };
        }

        private bool HasRequiredSkill(Mobile from) => from.Skills.Cartography.Value >= GetMinSkillLevel();

        public void Decode(Mobile from)
        {
            if (m_Completed || m_Decoder != null)
            {
                return;
            }

            if (m_Level == 0)
            {
                if (!CheckYoung(from))
                {
                    from.SendLocalizedMessage(1046447); // Only a young player may use this treasure map.
                    return;
                }
            }
            else
            {
                var minSkill = GetMinSkillLevel();

                if (from.Skills.Cartography.Value < minSkill)
                {
                    from.SendLocalizedMessage(503013); // The map is too difficult to attempt to decode.
                }

                var maxSkill = minSkill + 60.0;

                if (!from.CheckSkill(SkillName.Cartography, minSkill, maxSkill))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503018); // You fail to make anything of the map.
                    return;
                }
            }

            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503019); // You successfully decode a treasure map!
            Decoder = from;

            if (Core.AOS)
            {
                LootType = LootType.Blessed;
            }

            DisplayTo(from);
        }

        public override void DisplayTo(Mobile from)
        {
            if (m_Completed)
            {
                SendLocalizedMessageTo(from, 503014); // This treasure hunt has already been completed.
            }
            else if (m_Level == 0 && !CheckYoung(from))
            {
                from.SendLocalizedMessage(1046447); // Only a young player may use this treasure map.
                return;
            }
            else if (m_Decoder != from && !HasRequiredSkill(from))
            {
                from.SendLocalizedMessage(
                    503031
                ); // You did not decode this map and have no clue where to look for the treasure.
                return;
            }
            else
            {
                SendLocalizedMessageTo(
                    from,
                    503017
                ); // The treasure is marked by the red pin. Grab a shovel and go dig it up!
            }

            from.PlaySound(0x249);
            base.DisplayTo(from);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (!m_Completed)
            {
                if (m_Decoder == null)
                {
                    list.Add(new DecodeMapEntry(this));
                }
                else
                {
                    var digTool = HasDiggingTool(from);

                    list.Add(new OpenMapEntry(this));
                    list.Add(new DigEntry(this, digTool));
                }
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(m_Map == Map.Felucca ? 1041502 : 1041503); // for somewhere in Felucca : for somewhere in Trammel

            if (m_Completed)
            {
                list.Add(1041507, m_CompletedBy?.RawName ?? "someone"); // completed by ~1_val~
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Completed)
            {
                from.NetState.SendMessageLocalizedAffix(
                    Serial,
                    ItemID,
                    MessageType.Label,
                    0x3B2,
                    3,
                    1048030,
                    "",
                    AffixType.Append,
                    $" completed by {m_CompletedBy?.RawName ?? "someone"}"
                );
            }
            else if (m_Decoder != null)
            {
                if (m_Level == 6)
                {
                    LabelTo(from, 1063453);
                }
                else
                {
                    LabelTo(from, 1041516 + m_Level);
                }
            }
            else
            {
                if (m_Level == 6)
                {
                    LabelTo(from, 1041522, $"#{1063452}\t \t#{(m_Map == Map.Felucca ? 1041502 : 1041503)}");
                }
                else
                {
                    LabelTo(from, 1041522, $"#{1041510 + m_Level}\t \t#{(m_Map == Map.Felucca ? 1041502 : 1041503)}");
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);

            writer.Write(m_CompletedBy);

            writer.Write(m_Level);
            writer.Write(m_Completed);
            writer.Write(m_Decoder);
            writer.Write(m_Map);
            writer.Write(ChestLocation);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_CompletedBy = reader.ReadEntity<Mobile>();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Level = reader.ReadInt();
                        m_Completed = reader.ReadBool();
                        m_Decoder = reader.ReadEntity<Mobile>();
                        m_Map = reader.ReadMap();
                        ChestLocation = reader.ReadPoint2D();

                        if (version == 0 && m_Completed)
                        {
                            m_CompletedBy = m_Decoder;
                        }

                        break;
                    }
            }

            if (Core.AOS && m_Decoder != null && LootType == LootType.Regular)
            {
                LootType = LootType.Blessed;
            }
        }

        private class DigTarget : Target
        {
            private readonly TreasureMap m_Map;

            public DigTarget(TreasureMap map) : base(6, true, TargetFlags.None) => m_Map = map;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Map.Deleted)
                {
                    return;
                }

                var map = m_Map.m_Map;

                if (m_Map.m_Completed)
                {
                    from.SendLocalizedMessage(503028); // The treasure for this map has already been found.
                }
                /*
                else if (from != m_Map.m_Decoder)
                {
                  from.SendLocalizedMessage( 503016 ); // Only the person who decoded this map may actually dig up the treasure.
                }
                */
                else if (m_Map.m_Decoder != from && !m_Map.HasRequiredSkill(from))
                {
                    from.SendLocalizedMessage(
                        503031
                    ); // You did not decode this map and have no clue where to look for the treasure.
                }
                else if (!from.CanBeginAction<TreasureMap>())
                {
                    from.SendLocalizedMessage(503020); // You are already digging treasure.
                }
                else if (!HasDiggingTool(from))
                {
                    from.SendMessage("You must have a digging tool to dig for treasure.");
                }
                else if (from.Map != map)
                {
                    from.SendLocalizedMessage(1010479); // You seem to be in the right place, but may be on the wrong facet!
                }
                else
                {
                    var p = targeted as IPoint3D;

                    var targ3D = (p as Item)?.GetWorldLocation() ?? new Point3D(p);

                    int maxRange;
                    var skillValue = from.Skills.Mining.Value;

                    if (skillValue >= 100.0)
                    {
                        maxRange = 4;
                    }
                    else if (skillValue >= 81.0)
                    {
                        maxRange = 3;
                    }
                    else if (skillValue >= 51.0)
                    {
                        maxRange = 2;
                    }
                    else
                    {
                        maxRange = 1;
                    }

                    var loc = m_Map.ChestLocation;
                    int x = loc.X, y = loc.Y;

                    var chest3D0 = new Point3D(loc, 0);

                    if (Utility.InRange(targ3D, chest3D0, maxRange))
                    {
                        if (from.Location.X == x && from.Location.Y == y)
                        {
                            from.SendLocalizedMessage(
                                503030
                            ); // The chest can't be dug up because you are standing on top of it.
                        }
                        else if (map != null)
                        {
                            var z = map.GetAverageZ(x, y);

                            if (!map.CanFit(x, y, z, 16, true))
                            {
                                from.SendLocalizedMessage(
                                    503021
                                ); // You have found the treasure chest but something is keeping it from being dug up.
                            }
                            else if (from.BeginAction<TreasureMap>())
                            {
                                new DigTimer(from, m_Map, new Point3D(x, y, z), map).Start();
                            }
                            else
                            {
                                from.SendLocalizedMessage(503020); // You are already digging treasure.
                            }
                        }
                    }
                    else if (m_Map.Level > 0)
                    {
                        if (Utility.InRange(targ3D, chest3D0, 8)) // We're close, but not quite
                        {
                            from.SendLocalizedMessage(503032); // You dig and dig but no treasure seems to be here.
                        }
                        else
                        {
                            from.SendLocalizedMessage(503035); // You dig and dig but fail to find any treasure.
                        }
                    }
                    else
                    {
                        if (Utility.InRange(targ3D, chest3D0, 8)) // We're close, but not quite
                        {
                            from.SendAsciiMessage(0x44, "The treasure chest is very close!");
                        }
                        else
                        {
                            var dir = Utility.GetDirection(targ3D, chest3D0);

                            var sDir = dir switch
                            {
                                Direction.North => "north",
                                Direction.Right => "northeast",
                                Direction.East  => "east",
                                Direction.Down  => "southeast",
                                Direction.South => "south",
                                Direction.Left  => "southwest",
                                Direction.West  => "west",
                                _               => "northwest"
                            };

                            from.SendAsciiMessage(0x44, "Try looking for the treasure chest more to the {0}.", sDir);
                        }
                    }
                }
            }
        }

        private class DigTimer : Timer
        {
            private readonly Mobile m_From;
            private readonly long m_LastMoveTime;
            private readonly Map m_Map;
            private readonly long m_NextActionTime;

            private readonly long m_NextSkillTime;
            private readonly long m_NextSpellTime;
            private readonly TreasureMap m_TreasureMap;
            private TreasureMapChest m_Chest;

            private int m_Count;

            private TreasureChestDirt m_Dirt1;
            private TreasureChestDirt m_Dirt2;

            private Point3D m_Location;

            public DigTimer(Mobile from, TreasureMap treasureMap, Point3D location, Map map) : base(
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1.0)
            )
            {
                m_From = from;
                m_TreasureMap = treasureMap;

                m_Location = location;
                m_Map = map;

                m_NextSkillTime = from.NextSkillTime;
                m_NextSpellTime = from.NextSpellTime;
                m_NextActionTime = from.NextActionTime;
                m_LastMoveTime = from.LastMoveTime;
            }

            private void Terminate()
            {
                Stop();
                m_From.EndAction<TreasureMap>();

                m_Chest?.Delete();

                if (m_Dirt1 != null)
                {
                    m_Dirt1.Delete();
                    m_Dirt2.Delete();
                }
            }

            protected override void OnTick()
            {
                if (m_NextSkillTime != m_From.NextSkillTime || m_NextSpellTime != m_From.NextSpellTime ||
                    m_NextActionTime != m_From.NextActionTime)
                {
                    Terminate();
                    return;
                }

                if (m_LastMoveTime != m_From.LastMoveTime)
                {
                    m_From.SendLocalizedMessage(
                        503023
                    ); // You cannot move around while digging up treasure. You will need to start digging anew.
                    Terminate();
                    return;
                }

                var z = m_Chest != null ? m_Chest.Z + m_Chest.ItemData.Height : int.MinValue;
                var height = 16;

                if (z > m_Location.Z)
                {
                    height -= z - m_Location.Z;
                }
                else
                {
                    z = m_Location.Z;
                }

                if (!m_Map.CanFit(m_Location.X, m_Location.Y, z, height, true, true, false))
                {
                    m_From.SendLocalizedMessage(
                        503024
                    ); // You stop digging because something is directly on top of the treasure chest.
                    Terminate();
                    return;
                }

                m_Count++;

                m_From.RevealingAction();
                m_From.Direction = m_From.GetDirectionTo(m_Location);

                if (m_Count > 1 && m_Dirt1 == null)
                {
                    m_Dirt1 = new TreasureChestDirt();
                    m_Dirt1.MoveToWorld(m_Location, m_Map);

                    m_Dirt2 = new TreasureChestDirt();
                    m_Dirt2.MoveToWorld(new Point3D(m_Location.X, m_Location.Y - 1, m_Location.Z), m_Map);
                }

                if (m_Count == 5)
                {
                    m_Dirt1.Turn1();
                }
                else if (m_Count == 10)
                {
                    m_Dirt1.Turn2();
                    m_Dirt2.Turn2();
                }
                else if (m_Count > 10)
                {
                    if (m_Chest == null)
                    {
                        m_Chest = new TreasureMapChest(m_From, m_TreasureMap.Level, true);
                        m_Chest.MoveToWorld(new Point3D(m_Location.X, m_Location.Y, m_Location.Z - 15), m_Map);
                    }
                    else
                    {
                        m_Chest.Z++;
                    }

                    Effects.PlaySound(m_Chest.Location, m_Map, 0x33B);
                }

                if (m_Chest?.Location.Z >= m_Location.Z)
                {
                    Stop();
                    m_From.EndAction<TreasureMap>();

                    m_Chest.Temporary = false;
                    m_TreasureMap.Completed = true;
                    m_TreasureMap.CompletedBy = m_From;

                    var spawns = m_TreasureMap.Level switch
                    {
                        0 => 3,
                        1 => 0,
                        _ => 4
                    };

                    for (var i = 0; i < spawns; ++i)
                    {
                        var bc = Spawn(m_TreasureMap.Level, m_Chest.Location, m_Chest.Map, null, true);

                        if (bc != null)
                        {
                            m_Chest.Guardians.Add(bc);
                        }
                    }
                }
                else
                {
                    if (m_From.Body.IsHuman && !m_From.Mounted)
                    {
                        m_From.Animate(11, 5, 1, true, false, 0);
                    }

                    new SoundTimer(m_From, 0x125 + m_Count % 2).Start();
                }
            }

            private class SoundTimer : Timer
            {
                private readonly Mobile m_From;
                private readonly int m_SoundID;

                public SoundTimer(Mobile from, int soundID) : base(TimeSpan.FromSeconds(0.9))
                {
                    m_From = from;
                    m_SoundID = soundID;
                }

                protected override void OnTick()
                {
                    m_From.PlaySound(m_SoundID);
                }
            }
        }

        private class DecodeMapEntry : ContextMenuEntry
        {
            private readonly TreasureMap m_Map;

            public DecodeMapEntry(TreasureMap map) : base(6147, 2) => m_Map = map;

            public override void OnClick()
            {
                if (!m_Map.Deleted)
                {
                    m_Map.Decode(Owner.From);
                }
            }
        }

        private class OpenMapEntry : ContextMenuEntry
        {
            private readonly TreasureMap m_Map;

            public OpenMapEntry(TreasureMap map) : base(6150, 2) => m_Map = map;

            public override void OnClick()
            {
                if (!m_Map.Deleted)
                {
                    m_Map.DisplayTo(Owner.From);
                }
            }
        }

        private class DigEntry : ContextMenuEntry
        {
            private readonly TreasureMap m_Map;

            public DigEntry(TreasureMap map, bool enabled) : base(6148, 2)
            {
                m_Map = map;

                if (!enabled)
                {
                    Flags |= CMEFlags.Disabled;
                }
            }

            public override void OnClick()
            {
                if (m_Map.Deleted)
                {
                    return;
                }

                var from = Owner.From;

                if (HasDiggingTool(from))
                {
                    m_Map.OnBeginDig(from);
                }
                else
                {
                    from.SendMessage("You must have a digging tool to dig for treasure.");
                }
            }
        }
    }

    public class TreasureChestDirt : Item
    {
        public TreasureChestDirt() : base(0x912)
        {
            Movable = false;

            Timer.StartTimer(TimeSpan.FromMinutes(2.0), Delete);
        }

        public TreasureChestDirt(Serial serial) : base(serial)
        {
        }

        public void Turn1()
        {
            ItemID = 0x913;
        }

        public void Turn2()
        {
            ItemID = 0x914;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            Delete();
        }
    }
}
