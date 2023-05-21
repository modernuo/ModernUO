using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;

namespace Server.Engines.ConPVP
{
    public class ArenaController : Item
    {
        [Constructible]
        public ArenaController() : base(0x1B7A)
        {
            Visible = false;
            Movable = false;

            Arena = new Arena();

            Instances.Add(this);
        }

        public ArenaController(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public Arena Arena { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsPrivate { get; set; }

        public override string DefaultName => "arena controller";

        public static List<ArenaController> Instances { get; set; } = new();

        public override void OnDelete()
        {
            base.OnDelete();

            Instances.Remove(this);
            Arena.Delete();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                from.SendGump(new PropertiesGump(from, Arena));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);

            writer.Write(IsPrivate);

            Arena.Serialize(writer);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        IsPrivate = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        Arena = new Arena(reader);
                        break;
                    }
            }

            Instances.Add(this);
        }
    }

    [PropertyObject]
    public class ArenaStartPoints
    {
        public ArenaStartPoints(Point3D[] points = null) => Points = points ?? new Point3D[8];

        public ArenaStartPoints(IGenericReader reader)
        {
            Points = new Point3D[reader.ReadEncodedInt()];

            for (var i = 0; i < Points.Length; ++i)
            {
                Points[i] = reader.ReadPoint3D();
            }
        }

        public Point3D[] Points { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D EdgeWest
        {
            get => Points[0];
            set => Points[0] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D EdgeEast
        {
            get => Points[1];
            set => Points[1] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D EdgeNorth
        {
            get => Points[2];
            set => Points[2] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D EdgeSouth
        {
            get => Points[3];
            set => Points[3] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D CornerNW
        {
            get => Points[4];
            set => Points[4] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D CornerSE
        {
            get => Points[5];
            set => Points[5] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D CornerSW
        {
            get => Points[6];
            set => Points[6] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D CornerNE
        {
            get => Points[7];
            set => Points[7] = value;
        }

        public override string ToString() => "...";

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(Points.Length);

            for (var i = 0; i < Points.Length; ++i)
            {
                writer.Write(Points[i]);
            }
        }
    }

    [PropertyObject]
    public class Arena : IComparable<Arena>
    {
        private static readonly Point2D[] m_EdgeOffsets =
        {
            /*
             *        /\
             *       /\/\
             *      /\/\/\
             *      \/\/\/
             *       \/\/\
             *        \/\/
             */
            new(0, 0),
            new(0, -1),
            new(0, +1),
            new(1, 0),
            new(1, -1),
            new(1, +1),
            new(2, 0),
            new(2, -1),
            new(2, +1),
            new(3, 0)
        };

        // nw corner
        private static readonly Point2D[] m_CornerOffsets =
        {
            /*
             *         /\
             *        /\/\
             *       /\/\/\
             *      /\/\/\/\
             *      \/\/\/\/
             */
            new(0, 0),
            new(0, 1),
            new(1, 0),
            new(1, 1),
            new(0, 2),
            new(2, 0),
            new(2, 1),
            new(1, 2),
            new(0, 3),
            new(3, 0)
        };

        private static readonly int[][,] m_Rotate =
        {
            new[,] { { +1, 0 }, { 0, +1 } }, // west
            new[,] { { -1, 0 }, { 0, -1 } }, // east
            new[,] { { 0, +1 }, { +1, 0 } }, // north
            new[,] { { 0, -1 }, { -1, 0 } }, // south
            new[,] { { +1, 0 }, { 0, +1 } }, // nw
            new[,] { { -1, 0 }, { 0, -1 } }, // se
            new[,] { { 0, +1 }, { +1, 0 } }, // sw
            new[,] { { 0, -1 }, { -1, 0 } }  // ne
        };

        private bool m_Active;
        private Rectangle2D m_Bounds;
        private Map m_Facet;
        private Point3D m_GateOut;

        private bool m_IsGuarded;
        private string m_Name;

        private SafeZone m_Region;

        private TournamentController m_Tournament;
        private Rectangle2D m_Zone;

        public Arena()
        {
            Points = new ArenaStartPoints();
            Players = new List<Mobile>();
        }

        public Arena(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 7:
                    {
                        m_IsGuarded = reader.ReadBool();

                        goto case 6;
                    }
                case 6:
                    {
                        Ladder = reader.ReadEntity<LadderController>();

                        goto case 5;
                    }
                case 5:
                    {
                        m_Tournament = reader.ReadEntity<TournamentController>();
                        Announcer = reader.ReadEntity<Mobile>();

                        goto case 4;
                    }
                case 4:
                    {
                        m_Name = reader.ReadString();

                        goto case 3;
                    }
                case 3:
                    {
                        m_Zone = reader.ReadRect2D();

                        goto case 2;
                    }
                case 2:
                    {
                        GateIn = reader.ReadPoint3D();
                        m_GateOut = reader.ReadPoint3D();
                        Teleporter = reader.ReadEntity<Item>();

                        goto case 1;
                    }
                case 1:
                    {
                        Players = reader.ReadEntityList<Mobile>();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Facet = reader.ReadMap();
                        m_Bounds = reader.ReadRect2D();
                        Outside = reader.ReadPoint3D();
                        Wall = reader.ReadPoint3D();

                        if (version == 0)
                        {
                            reader.ReadBool();
                            Players = new List<Mobile>();
                        }

                        m_Active = reader.ReadBool();
                        Points = new ArenaStartPoints(reader);

                        if (m_Active)
                        {
                            Arenas.Add(this);
                            Arenas.Sort();
                        }

                        break;
                    }
            }

            if (m_Zone.Start != Point2D.Zero && m_Zone.End != Point2D.Zero && m_Facet != null)
            {
                m_Region = new SafeZone(m_Zone, Outside, m_Facet, m_IsGuarded);
            }

            if (IsOccupied)
            {
                Timer.StartTimer(TimeSpan.FromSeconds(2.0), Evict);
            }

            if (m_Tournament != null)
            {
                Timer.StartTimer(AttachToTournament_Sandbox);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public LadderController Ladder { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsGuarded
        {
            get => m_IsGuarded;
            set
            {
                m_IsGuarded = value;

                if (m_Region != null)
                {
                    m_Region.GuardsDisabled = !m_IsGuarded;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TournamentController Tournament
        {
            get => m_Tournament;
            set
            {
                m_Tournament?.Tournament.Arenas.Remove(this);

                m_Tournament = value;

                m_Tournament?.Tournament.Arenas.Add(this);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Announcer { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Name
        {
            get => m_Name;
            set
            {
                m_Name = value;
                if (m_Active)
                {
                    Arenas.Sort();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map Facet
        {
            get => m_Facet;
            set
            {
                m_Facet = value;

                if (Teleporter != null)
                {
                    Teleporter.Map = value;
                }

                m_Region?.Unregister();

                if (m_Zone.Start != Point2D.Zero && m_Zone.End != Point2D.Zero && m_Facet != null)
                {
                    m_Region = new SafeZone(m_Zone, Outside, m_Facet, m_IsGuarded);
                }
                else
                {
                    m_Region = null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D Bounds
        {
            get => m_Bounds;
            set => m_Bounds = value;
        }

        public int Spectators => m_Region == null ? 0 : Math.Max(m_Region.GetPlayerCount() - Players.Count, 0);

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D Zone
        {
            get => m_Zone;
            set
            {
                m_Zone = value;

                if (m_Zone.Start != Point2D.Zero && m_Zone.End != Point2D.Zero && m_Facet != null)
                {
                    m_Region?.Unregister();

                    m_Region = new SafeZone(m_Zone, Outside, m_Facet, m_IsGuarded);
                }
                else
                {
                    m_Region?.Unregister();

                    m_Region = null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Outside { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D GateIn { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D GateOut
        {
            get => m_GateOut;
            set
            {
                m_GateOut = value;
                if (Teleporter != null)
                {
                    Teleporter.Location = m_GateOut;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Wall { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsOccupied => Players.Count > 0;

        [CommandProperty(AccessLevel.GameMaster, canModify: true)]
        public ArenaStartPoints Points { get; }

        public Item Teleporter { get; set; }

        public List<Mobile> Players { get; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get => m_Active;
            set
            {
                if (m_Active == value)
                {
                    return;
                }

                m_Active = value;

                if (m_Active)
                {
                    Arenas.Add(this);
                    Arenas.Sort();
                }
                else
                {
                    Arenas.Remove(this);
                }
            }
        }

        [CommandProperty(AccessLevel.Administrator, AccessLevel.Administrator)]
        public bool ForceEvict
        {
            get => false;
            set
            {
                if (value)
                {
                    Evict();
                }
            }
        }

        public static List<Arena> Arenas { get; } = new();

        public int CompareTo(Arena c) => string.CompareOrdinal(m_Name, c?.m_Name);

        public Ladder AcquireLadder() => Ladder?.Ladder ?? ConPVP.Ladder.Instance;

        public void Delete()
        {
            Active = false;
            m_Region?.Unregister();
            m_Region = null;
        }

        public override string ToString() => "...";

        public Point3D GetBaseStartPoint(int index) => Points.Points[Math.Max(index, 0) % Points.Points.Length];

        public void MoveInside(DuelPlayer[] players, int index)
        {
            index = Math.Min(index, 0) % Points.Points.Length;

            var start = Points.Points[index];

            var offset = 0;

            var offsets = index < 4 ? m_EdgeOffsets : m_CornerOffsets;
            var matrix = m_Rotate[index];

            for (var i = 0; i < players.Length; ++i)
            {
                var pl = players[i];

                if (pl == null)
                {
                    continue;
                }

                var mob = pl.Mobile;

                Point2D p;

                if (offset < offsets.Length)
                {
                    p = offsets[offset++];
                }
                else
                {
                    p = offsets[^1];
                }

                p.X = p.X * matrix[0, 0] + p.Y * matrix[0, 1];
                p.Y = p.X * matrix[1, 0] + p.Y * matrix[1, 1];

                mob.MoveToWorld(new Point3D(start.X + p.X, start.Y + p.Y, start.Z), m_Facet);
                mob.Direction = mob.GetDirectionTo(Wall);

                Players.Add(mob);
            }
        }

        private void AttachToTournament_Sandbox()
        {
            m_Tournament?.Tournament.Arenas.Add(this);
        }

        public void Evict()
        {
            Point3D loc;
            Map facet;

            if (m_Facet == null)
            {
                loc = new Point3D(2715, 2165, 0);
                facet = Map.Felucca;
            }
            else
            {
                loc = Outside;
                facet = m_Facet;
            }

            var hasBounds = m_Bounds.Start != Point2D.Zero && m_Bounds.End != Point2D.Zero;

            for (var i = 0; i < Players.Count; ++i)
            {
                var mob = Players[i];

                if (mob == null)
                {
                    continue;
                }

                if (mob.Map == Map.Internal)
                {
                    if ((m_Facet == null || mob.LogoutMap == m_Facet) &&
                        (!hasBounds || m_Bounds.Contains(mob.LogoutLocation)))
                    {
                        mob.LogoutLocation = loc;
                    }
                }
                else if ((m_Facet == null || mob.Map == m_Facet) && (!hasBounds || m_Bounds.Contains(mob.Location)))
                {
                    mob.MoveToWorld(loc, facet);
                }

                mob.Combatant = null;
                mob.Frozen = false;
                DuelContext.Debuff(mob);
                DuelContext.CancelSpell(mob);
            }

            if (hasBounds)
            {
                var pets = new List<Mobile>();

                foreach (var mob in facet.GetMobilesInBounds(m_Bounds))
                {
                    if (mob is BaseCreature pet && pet.Controlled && pet.ControlMaster != null &&
                        Players.Contains(pet.ControlMaster))
                    {
                        pets.Add(pet);
                    }
                }

                foreach (var pet in pets)
                {
                    pet.Combatant = null;
                    pet.Frozen = false;

                    pet.MoveToWorld(loc, facet);
                }
            }

            Players.Clear();
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(7);

            writer.Write(m_IsGuarded);

            writer.Write(Ladder);

            writer.Write(m_Tournament);
            writer.Write(Announcer);

            writer.Write(m_Name);

            writer.Write(m_Zone);

            writer.Write(GateIn);
            writer.Write(m_GateOut);
            writer.Write(Teleporter);

            writer.Write(Players);

            writer.Write(m_Facet);
            writer.Write(m_Bounds);
            writer.Write(Outside);
            writer.Write(Wall);
            writer.Write(m_Active);

            Points.Serialize(writer);
        }

        public static Arena FindArena(List<Mobile> players)
        {
            var prefs = Preferences.Instance;

            if (prefs == null)
            {
                return FindArena();
            }

            if (Arenas.Count == 0)
            {
                return null;
            }

            if (players.Count > 0)
            {
                var first = players[0];

                var allControllers = ArenaController.Instances;

                for (var i = 0; i < allControllers.Count; ++i)
                {
                    var controller = allControllers[i];

                    if (controller?.Deleted == false && controller.Arena != null && controller.IsPrivate &&
                        controller.Map == first.Map && first.InRange(controller, 24))
                    {
                        var house = BaseHouse.FindHouseAt(controller);
                        var allNear = true;

                        for (var j = 0; j < players.Count; ++j)
                        {
                            var check = players[j];
                            bool isNear;

                            if (house == null)
                            {
                                isNear = controller.Map == check.Map && check.InRange(controller, 24);
                            }
                            else
                            {
                                isNear = BaseHouse.FindHouseAt(check) == house;
                            }

                            if (!isNear)
                            {
                                allNear = false;
                                break;
                            }
                        }

                        if (allNear)
                        {
                            return controller.Arena;
                        }
                    }
                }
            }

            var arenas = new List<ArenaEntry>();

            for (var i = 0; i < Arenas.Count; ++i)
            {
                var arena = Arenas[i];

                if (!arena.IsOccupied)
                {
                    arenas.Add(new ArenaEntry(arena));
                }
            }

            if (arenas.Count == 0)
            {
                return Arenas[0];
            }

            var tc = 0;

            for (var i = 0; i < arenas.Count; ++i)
            {
                var ae = arenas[i];

                for (var j = 0; j < players.Count; ++j)
                {
                    var pe = prefs.Find(players[j]);

                    if (!pe.Disliked.Contains(ae.m_Arena.Name))
                    {
                        ++ae.m_VotesFor;
                    }
                }

                tc += ae.Value;
            }

            var rn = Utility.Random(tc);

            for (var i = 0; i < arenas.Count; ++i)
            {
                var ae = arenas[i];

                if (rn < ae.Value)
                {
                    return ae.m_Arena;
                }

                rn -= ae.Value;
            }

            return arenas.RandomElement().m_Arena;
        }

        public static Arena FindArena()
        {
            if (Arenas.Count == 0)
            {
                return null;
            }

            var offset = Utility.Random(Arenas.Count);

            for (var i = 0; i < Arenas.Count; ++i)
            {
                var arena = Arenas[(i + offset) % Arenas.Count];

                if (!arena.IsOccupied)
                {
                    return arena;
                }
            }

            return Arenas[offset];
        }

        private class ArenaEntry
        {
            public readonly Arena m_Arena;
            public int m_VotesFor;

            public ArenaEntry(Arena arena) => m_Arena = arena;

            public int Value => m_VotesFor;
        }
    }
}
