
using System;
using System.Collections;
using System.Text;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using Server.Gumps;

namespace Server.Engines.ConPVP
{
    public class HillOfTheKing : Item
    {
        private int m_ScoreInterval;
        private KHGame m_Game;
        private Mobile m_King;
        private KingTimer m_KingTimer;

        [Constructable]
        public HillOfTheKing()
            : base(0x520)
        {
            m_ScoreInterval = 10;
            m_Game = null;
            m_King = null;
            Movable = false;

            Name = "the hill";
        }

        public HillOfTheKing(Serial s)
            : base(s)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_ScoreInterval = reader.ReadEncodedInt();
                        break;
                    }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteEncodedInt(m_ScoreInterval);
        }

        public Mobile King
        {
            get
            {
                return m_King;
            }
        }

        public KHGame Game
        {
            get { return m_Game; }
            set
            {
                if (m_Game != value)
                {
                    if (m_KingTimer != null)
                        m_KingTimer.Stop();
                    m_Game = value;
                    m_King = null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScoreInterval { get { return m_ScoreInterval; } set { m_ScoreInterval = value; } }

        public int CapturesSoFar
        {
            get
            {
                if (m_KingTimer != null)
                    return m_KingTimer.Captures;
                else
                    return 0;
            }
        }

        private bool CanBeKing(Mobile m)
        {
            // Game running?
            if (m_Game == null)
                return false;

            // Mobile exists and is alive and is a player?
            if (m == null || m.Deleted || !m.Alive || !m.Player)
                return false;

            // Not current king (or they are the current king)
            if (m_King != null && m_King != m)
                return false;

            // They are on a team
            if (m_Game.GetTeamInfo(m) == null)
                return false;

            return true;
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m_Game == null || m == null || !m.Alive)
                return base.OnMoveOver(m);

            if (CanBeKing(m))
            {
                if (base.OnMoveOver(m))
                {
                    ReKingify(m);
                    return true;
                }
            }
            else
            {
                // Decrease their stam a little so they don't keep pushing someone out of the way
                if (m.AccessLevel == AccessLevel.Player && m.Stam >= m.StamMax)
                    m.Stam -= 5;
            }

            return false;
        }

        public override bool OnMoveOff(Mobile m)
        {
            if (base.OnMoveOff(m))
            {
                if (m_King == m)
                    DeKingify();

                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual void OnKingDied(Mobile king, KHTeamInfo kingTeam, Mobile killer, KHTeamInfo killerTeam)
        {
            if (m_Game != null && CapturesSoFar > 0 && killer != null && king != null && kingTeam != null && killerTeam != null)
            {
                string kingName = king.Name;
                if (kingName == null)
                    kingName = "";
                string killerName = killer.Name;
                if (killerName == null)
                    killerName = "";

                m_Game.Alert("{0} ({1}) was dethroned by {2} ({3})!", kingName, kingTeam.Name, killerName, killerTeam.Name);
            }

            DeKingify();
        }

        private void DeKingify()
        {
            PublicOverheadMessage(MessageType.Regular, 0x0481, false, "Free!");

            if (m_KingTimer != null)
                m_KingTimer.Stop();

            m_King = null;
        }

        private void ReKingify(Mobile m)
        {
            KHTeamInfo ti = null;
            if (m_Game == null || m == null)
                return;

            ti = m_Game.GetTeamInfo(m);
            if (ti == null)
                return;

            m_King = m;

            if (m_KingTimer == null)
                m_KingTimer = new KingTimer(this);
            m_KingTimer.Stop();
            m_KingTimer.StartHillTicker();

            if (m_King.Name != null)
                PublicOverheadMessage(MessageType.Regular, 0x0481, false, String.Format("Taken by {0}!", m_King.Name));
        }

        private class KingTimer : Timer
        {
            private HillOfTheKing m_Hill;
            private int m_Total;
            private int m_Counter;

            public int Captures { get { return m_Total; } }

            public KingTimer(HillOfTheKing hill)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Hill = hill;
                m_Total = 0;
                m_Counter = 0;

                Priority = TimerPriority.FiftyMS;
            }

            public void StartHillTicker()
            {
                m_Total = 0;
                m_Counter = 0;

                Start();
            }

            protected override void OnTick()
            {
                KHTeamInfo ti = null;
                KHPlayerInfo pi = null;

                if (m_Hill == null || m_Hill.Deleted || m_Hill.Game == null)
                {
                    Stop();
                    return;
                }

                if (m_Hill.King == null || m_Hill.King.Deleted || !m_Hill.King.Alive)
                {
                    m_Hill.DeKingify();
                    Stop();
                    return;
                }

                ti = m_Hill.Game.GetTeamInfo(m_Hill.King);
                if (ti != null)
                    pi = ti[m_Hill.King];

                if (ti == null || pi == null)
                {
                    // error, bail
                    m_Hill.DeKingify();
                    Stop();
                    return;
                }

                m_Counter++;

                m_Hill.King.RevealingAction();

                if (m_Counter >= m_Hill.ScoreInterval)
                {
                    string hill = m_Hill.Name;
                    string king = m_Hill.King.Name;
                    if (king == null)
                        king = "";

                    if (hill == null || hill == "")
                        hill = "the hill";

                    m_Hill.Game.Alert("{0} ({1}) is king of {2}!", king, ti.Name, hill);

                    m_Hill.PublicOverheadMessage(MessageType.Regular, 0x0481, false, "Capture!");

                    pi.Captures++;
                    m_Total++;

                    pi.Score += m_Counter;

                    m_Counter = 0;
                }
                else
                {
                    m_Hill.PublicOverheadMessage(MessageType.Regular, 0x0481, false, (m_Hill.ScoreInterval - m_Counter).ToString());
                }
            }
        }
    }

    public class KHBoard : Item
    {
        public KHGame m_Game;
        private KHController m_Controller;

        [Constructable]
        public KHBoard()
            : base(7774)
        {
            Name = "King of the Hill Scoreboard";
            Movable = false;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public KHController Controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                        m_Controller.RemoveBoard(this);
                    m_Controller = value;
                    if (m_Controller != null)
                        m_Controller.AddBoard(this);
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Game != null)
            {
                from.CloseGump(typeof(KHBoardGump));
                from.SendGump(new KHBoardGump(from, m_Game));
            }
            else
            {
                from.SendMessage("There is no King of the Hill game in progress.");
            }
        }

        public KHBoard(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

            writer.Write(m_Controller);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Controller = reader.ReadItem() as KHController;
                        break;
                    }
            }
        }
    }

    public sealed class KHBoardGump : Gump
    {
        public string Center(string text)
        {
            return String.Format("<CENTER>{0}</CENTER>", text);
        }

        public string Color(string text, int color)
        {
            return String.Format("<BASEFONT COLOR=#{0:X6}>{1}</BASEFONT>", color, text);
        }

        private void AddBorderedText(int x, int y, int width, int height, string text, int color, int borderColor)
        {
            AddColoredText(x - 1, y - 1, width, height, text, borderColor);
            AddColoredText(x - 1, y + 1, width, height, text, borderColor);
            AddColoredText(x + 1, y - 1, width, height, text, borderColor);
            AddColoredText(x + 1, y + 1, width, height, text, borderColor);
            AddColoredText(x, y, width, height, text, color);
        }

        private void AddColoredText(int x, int y, int width, int height, string text, int color)
        {
            if (color == 0)
                AddHtml(x, y, width, height, text, false, false);
            else
                AddHtml(x, y, width, height, Color(text, color), false, false);
        }

        private const int LabelColor32 = 0xFFFFFF;
        private const int BlackColor32 = 0x000000;

        private KHGame m_Game;

        public KHBoardGump(Mobile mob, KHGame game)
            : base(60, 60)
        {
            m_Game = game;

            KHTeamInfo ourTeam = game.GetTeamInfo(mob);

            ArrayList entries = new ArrayList();

            for (int i = 0; i < game.Context.Participants.Count; ++i)
            {
                KHTeamInfo teamInfo = game.Controller.TeamInfo[i % game.Controller.TeamInfo.Length];

                if (teamInfo == null)
                    continue;

                entries.Add(teamInfo);
            }

            entries.Sort();
            /*
                delegate( IRankedCTF a, IRankedCTF b )
            {
                return b.Score - a.Score;
            } );*/

            int height = 73 + (entries.Count * 75) + 28;

            Closable = false;

            AddPage(0);

            AddBackground(1, 1, 398, height, 3600);

            AddImageTiled(16, 15, 369, height - 29, 3604);

            for (int i = 0; i < entries.Count; i += 1)
                AddImageTiled(22, 58 + (i * 75), 357, 70, 0x2430);

            AddAlphaRegion(16, 15, 369, height - 29);

            AddImage(215, -45, 0xEE40);
            //AddImage( 330, 141, 0x8BA );

            AddBorderedText(22, 22, 294, 20, Center("King of the Hill Scoreboard"), LabelColor32, BlackColor32);

            AddImageTiled(32, 50, 264, 1, 9107);
            AddImageTiled(42, 52, 264, 1, 9157);

            for (int i = 0; i < entries.Count; ++i)
            {
                KHTeamInfo teamInfo = entries[i] as KHTeamInfo;

                AddImage(30, 70 + (i * 75), 10152);
                AddImage(30, 85 + (i * 75), 10151);
                AddImage(30, 100 + (i * 75), 10151);
                AddImage(30, 106 + (i * 75), 10154);

                AddImage(24, 60 + (i * 75), teamInfo == ourTeam ? 9730 : 9727, teamInfo.Color - 1);

                int nameColor = LabelColor32;
                int borderColor = BlackColor32;

                switch (teamInfo.Color)
                {
                    case 0x47E:
                        nameColor = 0xFFFFFF;
                        break;

                    case 0x4F2:
                        nameColor = 0x3399FF;
                        break;

                    case 0x4F7:
                        nameColor = 0x33FF33;
                        break;

                    case 0x4FC:
                        nameColor = 0xFF00FF;
                        break;

                    case 0x021:
                        nameColor = 0xFF3333;
                        break;

                    case 0x01A:
                        nameColor = 0xFF66FF;
                        break;

                    case 0x455:
                        nameColor = 0x333333;
                        borderColor = 0xFFFFFF;
                        break;
                }

                AddBorderedText(60, 65 + (i * 75), 250, 20, String.Format("{0}: {1}", LadderGump.Rank(1 + i), teamInfo.Name), nameColor, borderColor);

                AddBorderedText(50 + 10, 85 + (i * 75), 100, 20, "Score:", 0xFFC000, BlackColor32);
                AddBorderedText(50 + 15, 105 + (i * 75), 100, 20, teamInfo.Score.ToString("N0"), 0xFFC000, BlackColor32);

                AddBorderedText(110 + 10, 85 + (i * 75), 100, 20, "Kills:", 0xFFC000, BlackColor32);
                AddBorderedText(110 + 15, 105 + (i * 75), 100, 20, teamInfo.Kills.ToString("N0"), 0xFFC000, BlackColor32);

                AddBorderedText(160 + 10, 85 + (i * 75), 100, 20, "Captures:", 0xFFC000, BlackColor32);
                AddBorderedText(160 + 15, 105 + (i * 75), 100, 20, teamInfo.Captures.ToString("N0"), 0xFFC000, BlackColor32);

                string leader = null;
                if (teamInfo.Leader != null)
                    leader = teamInfo.Leader.Name;
                if (leader == null)
                    leader = "(none)";

                AddBorderedText(235 + 10, 85 + (i * 75), 250, 20, "Leader:", 0xFFC000, BlackColor32);
                AddBorderedText(235 + 15, 105 + (i * 75), 250, 20, leader, 0xFFC000, BlackColor32);
            }

            AddButton(314, height - 42, 247, 248, 1, GumpButtonType.Reply, 0);
        }
    }

    public sealed class KHPlayerInfo : IRankedCTF, IComparable
    {
        private KHTeamInfo m_TeamInfo;

        private Mobile m_Player;

        private int m_Kills;
        private int m_Captures;
        private int m_Score;

        public KHPlayerInfo(KHTeamInfo teamInfo, Mobile player)
        {
            m_TeamInfo = teamInfo;
            m_Player = player;
        }

        public Mobile Player { get { return m_Player; } }

        public int CompareTo(object obj)
        {
            KHPlayerInfo pi = (KHPlayerInfo)obj;
            int res = pi.Score.CompareTo(this.Score);
            if (res == 0)
            {
                res = pi.Captures.CompareTo(this.Captures);

                if (res == 0)
                    res = pi.Kills.CompareTo(this.Kills);
            }
            return res;
        }

        public string Name
        {
            get
            {
                if (m_Player == null || m_Player.Name == null)
                    return "";
                return m_Player.Name;
            }
        }

        public int Kills
        {
            get
            {
                return m_Kills;
            }
            set
            {
                m_TeamInfo.Kills += (value - m_Kills);
                m_Kills = value;
            }
        }

        public int Captures
        {
            get
            {
                return m_Captures;
            }
            set
            {
                m_TeamInfo.Captures += (value - m_Captures);
                m_Captures = value;
            }
        }

        public int Score
        {
            get
            {
                return m_Score;
            }
            set
            {
                m_TeamInfo.Score += (value - m_Score);
                m_Score = value;

                if (m_TeamInfo.Leader == null || m_Score > m_TeamInfo.Leader.Score)
                    m_TeamInfo.Leader = this;
            }
        }
    }

    [PropertyObject]
    public sealed class KHTeamInfo : IRankedCTF, IComparable
    {
        private KHGame m_Game;
        private int m_TeamID;

        private int m_Color;
        private string m_Name;

        private int m_Kills;
        private int m_Captures;

        private int m_Score;

        private Hashtable m_Players;

        public int CompareTo(object obj)
        {
            KHTeamInfo ti = (KHTeamInfo)obj;
            int res = ti.Score.CompareTo(this.Score);
            if (res == 0)
            {
                res = ti.Captures.CompareTo(this.Captures);

                if (res == 0)
                    res = ti.Kills.CompareTo(this.Kills);
            }
            return res;
        }

        public string Name
        {
            get
            {
                if (m_Name == null)
                    return "(null) Team";
                return String.Format("{0} Team", m_Name);
            }
        }

        public KHGame Game { get { return m_Game; } set { m_Game = value; } }
        public int TeamID { get { return m_TeamID; } }

        public int Kills { get { return m_Kills; } set { m_Kills = value; } }
        public int Captures { get { return m_Captures; } set { m_Captures = value; } }
        public int Score { get { return m_Score; } set { m_Score = value; } }

        private KHPlayerInfo m_Leader;

        public KHPlayerInfo Leader
        {
            get { return m_Leader; }
            set { m_Leader = value; }
        }

        public Hashtable Players
        {
            get { return m_Players; }
        }

        public KHPlayerInfo this[Mobile mob]
        {
            get
            {
                if (mob == null)
                    return null;

                KHPlayerInfo val = m_Players[mob] as KHPlayerInfo;

                if (val == null)
                    m_Players[mob] = val = new KHPlayerInfo(this, mob);

                return val;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Color
        {
            get { return m_Color; }
            set { m_Color = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TeamName
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public KHTeamInfo(int teamID)
        {
            m_TeamID = teamID;
            m_Players = new Hashtable();
        }

        public void Reset()
        {
            m_Kills = 0;
            m_Captures = 0;
            m_Score = 0;

            m_Leader = null;

            m_Players.Clear();
        }

        public KHTeamInfo(int teamID, GenericReader ip)
        {
            m_TeamID = teamID;
            m_Players = new Hashtable();

            int version = ip.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        m_Name = ip.ReadString();
                        m_Color = ip.ReadEncodedInt();
                        break;
                    }
            }
        }

        public void Serialize(GenericWriter op)
        {
            op.WriteEncodedInt(0); // version

            op.Write(m_Name);
            op.WriteEncodedInt(m_Color);
        }

        public override string ToString()
        {
            if (m_Name != null)
                return String.Format("({0}) ...", this.Name);
            else
                return "...";
        }
    }

    public sealed class KHController : EventController
    {
        private KHTeamInfo[] m_TeamInfo;
        private HillOfTheKing[] m_Hills;
        private ArrayList m_Boards;
        private TimeSpan m_Duration;
        private int m_ScoreInterval;

        public KHTeamInfo[] TeamInfo { get { return m_TeamInfo; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team1_W { get { return m_TeamInfo[0]; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team2_E { get { return m_TeamInfo[1]; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team3_N { get { return m_TeamInfo[2]; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team4_S { get { return m_TeamInfo[3]; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team5_NW { get { return m_TeamInfo[4]; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team6_SE { get { return m_TeamInfo[5]; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team7_SW { get { return m_TeamInfo[6]; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team8_NE { get { return m_TeamInfo[7]; } set { } }

        public HillOfTheKing[] Hills { get { return m_Hills; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public HillOfTheKing Hill1 { get { return m_Hills[0]; } set { m_Hills[0] = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public HillOfTheKing Hill2 { get { return m_Hills[1]; } set { m_Hills[1] = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public HillOfTheKing Hill3 { get { return m_Hills[2]; } set { m_Hills[2] = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public HillOfTheKing Hill4 { get { return m_Hills[3]; } set { m_Hills[3] = value; } }

        public ArrayList Boards { get { return m_Boards; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Duration
        {
            get { return m_Duration; }
            set { m_Duration = value; }
        }

        public override string Title
        {
            get { return "King of the Hill"; }
        }

        public override string GetTeamName(int teamID)
        {
            return m_TeamInfo[teamID % m_TeamInfo.Length].Name;
        }

        public override EventGame Construct(DuelContext context)
        {
            return new KHGame(this, context);
        }

        public void RemoveBoard(KHBoard b)
        {
            if (b != null)
            {
                m_Boards.Remove(b);
                b.m_Game = null;
            }
        }

        public void AddBoard(KHBoard b)
        {
            if (b != null)
                m_Boards.Add(b);
        }

        [Constructable]
        public KHController()
        {
            Visible = false;
            Movable = false;

            Name = "King of the Hill Controller";

            m_Duration = TimeSpan.FromMinutes(30.0);
            m_Boards = new ArrayList();
            m_Hills = new HillOfTheKing[4];
            m_TeamInfo = new KHTeamInfo[8];

            for (int i = 0; i < m_TeamInfo.Length; ++i)
                m_TeamInfo[i] = new KHTeamInfo(i);
        }

        public KHController(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

            writer.WriteEncodedInt(m_ScoreInterval);
            writer.Write(m_Duration);

            writer.WriteItemList(m_Boards, true);

            writer.WriteEncodedInt(m_Hills.Length);
            for (int i = 0; i < m_Hills.Length; ++i)
                writer.Write(m_Hills[i]);

            writer.WriteEncodedInt(m_TeamInfo.Length);
            for (int i = 0; i < m_TeamInfo.Length; ++i)
                m_TeamInfo[i].Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_ScoreInterval = reader.ReadEncodedInt();

                        m_Duration = reader.ReadTimeSpan();

                        m_Boards = reader.ReadItemList();

                        m_Hills = new HillOfTheKing[reader.ReadEncodedInt()];
                        for (int i = 0; i < m_Hills.Length; ++i)
                            m_Hills[i] = reader.ReadItem() as HillOfTheKing;

                        m_TeamInfo = new KHTeamInfo[reader.ReadEncodedInt()];
                        for (int i = 0; i < m_TeamInfo.Length; ++i)
                            m_TeamInfo[i] = new KHTeamInfo(i, reader);

                        break;
                    }
            }
        }
    }

    public sealed class KHGame : EventGame
    {
        private KHController m_Controller;

        public KHController Controller { get { return m_Controller; } }

        public override bool CantDoAnything(Mobile mob)
        {
            if (mob != null && GetTeamInfo(mob) != null && m_Controller != null)
            {
                for (int i = 0; i < m_Controller.Hills.Length; i++)
                {
                    if (m_Controller.Hills[i] != null && m_Controller.Hills[i].King == mob)
                        return true;
                }
            }

            return false;
        }

        public void Alert(string text)
        {
            if (m_Context.m_Tournament != null)
                m_Context.m_Tournament.Alert(text);

            for (int i = 0; i < m_Context.Participants.Count; ++i)
            {
                Participant p = m_Context.Participants[i] as Participant;

                for (int j = 0; j < p.Players.Length; ++j)
                {
                    if (p.Players[j] != null)
                        p.Players[j].Mobile.SendMessage(0x35, text);
                }
            }
        }

        public void Alert(string format, params object[] args)
        {
            Alert(String.Format(format, args));
        }

        public KHGame(KHController controller, DuelContext context)
            : base(context)
        {
            m_Controller = controller;
        }

        public Map Facet
        {
            get
            {
                if (m_Context != null && m_Context.Arena != null)
                    return m_Context.Arena.Facet;

                return m_Controller.Map;
            }
        }

        public KHTeamInfo GetTeamInfo(Mobile mob)
        {
            int teamID = GetTeamID(mob);

            if (teamID >= 0)
                return m_Controller.TeamInfo[teamID % m_Controller.TeamInfo.Length];

            return null;
        }

        public int GetTeamID(Mobile mob)
        {
            PlayerMobile pm = mob as PlayerMobile;

            if (pm == null)
            {
                if (mob is BaseCreature)
                    return ((BaseCreature)mob).Team - 1;
                else
                    return -1;
            }

            if (pm.DuelContext == null || pm.DuelContext != m_Context)
                return -1;

            if (pm.DuelPlayer == null || pm.DuelPlayer.Eliminated)
                return -1;

            return pm.DuelContext.Participants.IndexOf(pm.DuelPlayer.Participant);
        }

        public int GetColor(Mobile mob)
        {
            KHTeamInfo teamInfo = GetTeamInfo(mob);

            if (teamInfo != null)
                return teamInfo.Color;

            return -1;
        }

        private void ApplyHues(Participant p, int hueOverride)
        {
            for (int i = 0; i < p.Players.Length; ++i)
            {
                if (p.Players[i] != null)
                    p.Players[i].Mobile.SolidHueOverride = hueOverride;
            }
        }

        public void DelayBounce(TimeSpan ts, Mobile mob, Container corpse)
        {
            Timer.DelayCall(ts, new TimerStateCallback(DelayBounce_Callback), new object[] { mob, corpse });
        }

        private void DelayBounce_Callback(object state)
        {
            object[] states = (object[])state;
            Mobile mob = (Mobile)states[0];
            Container corpse = (Container)states[1];

            DuelPlayer dp = null;

            if (mob is PlayerMobile)
                dp = (mob as PlayerMobile).DuelPlayer;

            m_Context.RemoveAggressions(mob);

            if (dp != null && !dp.Eliminated)
                mob.MoveToWorld(m_Context.Arena.GetBaseStartPoint(GetTeamID(mob)), Facet);
            else
                m_Context.SendOutside(mob);

            m_Context.Refresh(mob, corpse);
            DuelContext.Debuff(mob);
            DuelContext.CancelSpell(mob);
            mob.Frozen = false;

            if (corpse != null && !corpse.Deleted)
                Timer.DelayCall(TimeSpan.FromSeconds(30), new TimerCallback(corpse.Delete));
        }

        public override bool OnDeath(Mobile mob, Container corpse)
        {
            Mobile killer = mob.FindMostRecentDamager(false);
            KHTeamInfo teamInfo = null;
            KHTeamInfo victInfo = GetTeamInfo(mob);
            int bonus = 0;

            if (killer != null && killer.Player)
                teamInfo = GetTeamInfo(killer);

            for (int i = 0; i < m_Controller.Hills.Length; i++)
            {
                if (m_Controller.Hills[i] == null)
                    continue;

                if (m_Controller.Hills[i].King == mob)
                {
                    bonus += m_Controller.Hills[i].CapturesSoFar;
                    m_Controller.Hills[i].OnKingDied(mob, victInfo, killer, teamInfo);
                }

                if (m_Controller.Hills[i].King == killer)
                    bonus += 2;
            }

            if (teamInfo != null && teamInfo != victInfo)
            {
                KHPlayerInfo playerInfo = teamInfo[killer];

                if (playerInfo != null)
                {
                    playerInfo.Kills += 1;
                    playerInfo.Score += 1 + bonus;
                }
            }

            mob.CloseGump(typeof(KHBoardGump));
            mob.SendGump(new KHBoardGump(mob, this));

            m_Context.Requip(mob, corpse);
            DelayBounce(TimeSpan.FromSeconds(30.0), mob, corpse);

            return false;
        }

        private Timer m_FinishTimer;

        public override void OnStart()
        {
            for (int i = 0; i < m_Controller.TeamInfo.Length; ++i)
            {
                KHTeamInfo teamInfo = m_Controller.TeamInfo[i];

                teamInfo.Game = this;
                teamInfo.Reset();
            }

            for (int i = 0; i < m_Context.Participants.Count; ++i)
                ApplyHues(m_Context.Participants[i] as Participant, m_Controller.TeamInfo[i % m_Controller.TeamInfo.Length].Color);

            if (m_FinishTimer != null)
                m_FinishTimer.Stop();

            for (int i = 0; i < m_Controller.Hills.Length; i++)
            {
                if (m_Controller.Hills[i] != null)
                    m_Controller.Hills[i].Game = this;
            }

            foreach (KHBoard board in m_Controller.Boards)
            {
                if (board != null && !board.Deleted)
                    board.m_Game = this;
            }

            m_FinishTimer = Timer.DelayCall(m_Controller.Duration, new TimerCallback(Finish_Callback));
        }

        private void Finish_Callback()
        {
            ArrayList teams = new ArrayList();

            for (int i = 0; i < m_Context.Participants.Count; ++i)
            {
                KHTeamInfo teamInfo = m_Controller.TeamInfo[i % m_Controller.TeamInfo.Length];

                if (teamInfo == null)
                    continue;

                teams.Add(teamInfo);
            }

            teams.Sort();

            Tournament tourny = m_Context.m_Tournament;

            StringBuilder sb = new StringBuilder();

            if (tourny != null && tourny.TournyType == TournyType.FreeForAll)
            {
                sb.Append(m_Context.Participants.Count * tourny.PlayersPerParticipant);
                sb.Append("-man FFA");
            }
            else if (tourny != null && tourny.TournyType == TournyType.RandomTeam)
            {
                sb.Append(tourny.ParticipantsPerMatch);
                sb.Append("-team");
            }
            else if (tourny != null && tourny.TournyType == TournyType.RedVsBlue)
            {
                sb.Append("Red v Blue");
            }
            else if (tourny != null)
            {
                for (int i = 0; i < tourny.ParticipantsPerMatch; ++i)
                {
                    if (sb.Length > 0)
                        sb.Append('v');

                    sb.Append(tourny.PlayersPerParticipant);
                }
            }

            if (m_Controller != null)
                sb.Append(' ').Append(m_Controller.Title);

            string title = sb.ToString();

            KHTeamInfo winner = (KHTeamInfo)(teams.Count > 0 ? teams[0] : null);

            for (int i = 0; i < teams.Count; ++i)
            {
                TrophyRank rank = TrophyRank.Bronze;

                if (i == 0)
                    rank = TrophyRank.Gold;
                else if (i == 1)
                    rank = TrophyRank.Silver;

                KHPlayerInfo leader = ((KHTeamInfo)teams[i]).Leader;

                foreach (KHPlayerInfo pl in ((KHTeamInfo)teams[i]).Players.Values)
                {
                    Mobile mob = pl.Player;

                    if (mob == null)
                        continue;

                    sb = new StringBuilder();

                    sb.Append(title);

                    if (pl == leader)
                        sb.Append(" Leader");

                    if (pl.Score > 0)
                    {
                        sb.Append(": ");

                        sb.Append(pl.Score.ToString("N0"));
                        sb.Append(pl.Score == 1 ? " point" : " points");

                        sb.Append(", ");
                        sb.Append(pl.Kills.ToString("N0"));
                        sb.Append(pl.Kills == 1 ? " kill" : " kills");

                        if (pl.Captures > 0)
                        {
                            sb.Append(", ");
                            sb.Append(pl.Captures.ToString("N0"));
                            sb.Append(pl.Captures == 1 ? " capture" : " captures");
                        }
                    }

                    Item item = new Trophy(sb.ToString(), rank);

                    if (pl == leader)
                        item.ItemID = 4810;

                    item.Name = String.Format("{0}, {1}", item.Name, ((KHTeamInfo)teams[i]).Name.ToLower());

                    if (!mob.PlaceInBackpack(item))
                        mob.BankBox.DropItem(item);

                    int cash = pl.Score * 250;

                    if (cash > 0)
                    {
                        item = new BankCheck(cash);

                        if (!mob.PlaceInBackpack(item))
                            mob.BankBox.DropItem(item);

                        mob.SendMessage("You have been awarded a {0} trophy and {1:N0}gp for your participation in this game.", rank.ToString().ToLower(), cash);
                    }
                    else
                    {
                        mob.SendMessage("You have been awarded a {0} trophy for your participation in this game.", rank.ToString().ToLower());
                    }
                }
            }

            for (int i = 0; i < m_Context.Participants.Count; ++i)
            {
                Participant p = m_Context.Participants[i] as Participant;

                if (p == null || p.Players == null)
                    continue;

                for (int j = 0; j < p.Players.Length; ++j)
                {
                    DuelPlayer dp = p.Players[j];

                    if (dp != null && dp.Mobile != null)
                    {
                        dp.Mobile.CloseGump(typeof(KHBoardGump));
                        dp.Mobile.SendGump(new KHBoardGump(dp.Mobile, this));
                    }
                }

                if (i == winner.TeamID)
                    continue;

                if (p != null && p.Players != null)
                {
                    for (int j = 0; j < p.Players.Length; ++j)
                    {
                        if (p.Players[j] != null)
                            p.Players[j].Eliminated = true;
                    }
                }
            }

            m_Context.Finish(m_Context.Participants[winner.TeamID] as Participant);
        }

        public override void OnStop()
        {
            for (int i = 0; i < m_Controller.TeamInfo.Length; ++i)
                m_Controller.TeamInfo[i].Game = null;

            for (int i = 0; i < m_Controller.Hills.Length; ++i)
            {
                if (m_Controller.Hills[i] != null)
                    m_Controller.Hills[i].Game = null;
            }

            foreach (KHBoard board in m_Controller.Boards)
            {
                if (board != null)
                    board.m_Game = null;
            }

            for (int i = 0; i < m_Context.Participants.Count; ++i)
                ApplyHues(m_Context.Participants[i] as Participant, -1);

            if (m_FinishTimer != null)
                m_FinishTimer.Stop();
            m_FinishTimer = null;
        }
    }
}
