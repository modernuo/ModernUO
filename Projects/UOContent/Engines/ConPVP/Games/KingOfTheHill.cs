using System;
using System.Collections.Generic;
using System.Text;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.ConPVP
{
    public class HillOfTheKing : Item
    {
        private KHGame m_Game;
        private KingTimer m_KingTimer;

        [Constructible]
        public HillOfTheKing()
            : base(0x520)
        {
            ScoreInterval = 10;
            m_Game = null;
            King = null;
            Movable = false;

            Name = "the hill";
        }

        public HillOfTheKing(Serial s) : base(s)
        {
        }

        public Mobile King { get; private set; }

        public KHGame Game
        {
            get => m_Game;
            set
            {
                if (m_Game != value)
                {
                    m_KingTimer?.Stop();
                    m_Game = value;
                    King = null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScoreInterval { get; set; }

        public int CapturesSoFar => m_KingTimer?.Captures ?? 0;

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        ScoreInterval = reader.ReadEncodedInt();
                        break;
                    }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.WriteEncodedInt(ScoreInterval);
        }

        private bool CanBeKing(Mobile m)
        {
            // Game running?
            if (m_Game == null)
            {
                return false;
            }

            // Mobile exists and is alive and is a player?
            if (m?.Deleted != false || !m.Alive || !m.Player)
            {
                return false;
            }

            // Not current king (or they are the current king)
            if (King != null && King != m)
            {
                return false;
            }

            // They are on a team
            return m_Game.GetTeamInfo(m) != null;
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m_Game == null || m?.Alive != true)
            {
                return base.OnMoveOver(m);
            }

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
                {
                    m.Stam -= 5;
                }
            }

            return false;
        }

        public override bool OnMoveOff(Mobile m)
        {
            if (base.OnMoveOff(m))
            {
                if (King == m)
                {
                    DeKingify();
                }

                return true;
            }

            return false;
        }

        public virtual void OnKingDied(Mobile king, KHTeamInfo kingTeam, Mobile killer, KHTeamInfo killerTeam)
        {
            if (m_Game != null && CapturesSoFar > 0 && killer != null && king != null && kingTeam != null &&
                killerTeam != null)
            {
                var kingName = king.Name ?? "";
                var killerName = killer.Name ?? "";

                m_Game.Alert($"{kingName} ({kingTeam.Name}) was dethroned by {killerName} ({killerTeam.Name})!");
            }

            DeKingify();
        }

        private void DeKingify()
        {
            PublicOverheadMessage(MessageType.Regular, 0x0481, false, "Free!");

            m_KingTimer?.Stop();

            King = null;
        }

        private void ReKingify(Mobile m)
        {
            if (m_Game == null || m == null)
            {
                return;
            }

            if (m_Game.GetTeamInfo(m) == null)
            {
                return;
            }

            King = m;

            m_KingTimer ??= new KingTimer(this);
            m_KingTimer.Stop();
            m_KingTimer.StartHillTicker();

            if (King.Name != null)
            {
                PublicOverheadMessage(MessageType.Regular, 0x0481, false, $"Taken by {King.Name}!");
            }
        }

        private class KingTimer : Timer
        {
            private readonly HillOfTheKing m_Hill;
            private int m_Counter;

            public KingTimer(HillOfTheKing hill)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Hill = hill;
                Captures = 0;
                m_Counter = 0;
            }

            public int Captures { get; private set; }

            public void StartHillTicker()
            {
                Captures = 0;
                m_Counter = 0;

                Start();
            }

            protected override void OnTick()
            {
                KHPlayerInfo pi = null;

                if (m_Hill?.Deleted != false || m_Hill.Game == null)
                {
                    Stop();
                    return;
                }

                if (m_Hill.King?.Deleted != false || !m_Hill.King.Alive)
                {
                    m_Hill.DeKingify();
                    Stop();
                    return;
                }

                var ti = m_Hill.Game.GetTeamInfo(m_Hill.King);
                if (ti != null)
                {
                    pi = ti[m_Hill.King];
                }

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
                    var hill = m_Hill.Name.DefaultIfNullOrEmpty("the hill");
                    var king = m_Hill.King.Name ?? "";

                    m_Hill.Game.Alert($"{king} ({ti.Name}) is king of {hill}!");

                    m_Hill.PublicOverheadMessage(MessageType.Regular, 0x0481, false, "Capture!");

                    pi.Captures++;
                    Captures++;

                    pi.Score += m_Counter;

                    m_Counter = 0;
                }
                else
                {
                    m_Hill.PublicOverheadMessage(
                        MessageType.Regular,
                        0x0481,
                        false,
                        (m_Hill.ScoreInterval - m_Counter).ToString()
                    );
                }
            }
        }
    }

    public class KHBoard : Item
    {
        private KHController m_Controller;
        public KHGame m_Game;

        [Constructible]
        public KHBoard()
            : base(7774)
        {
            Name = "King of the Hill Scoreboard";
            Movable = false;
        }

        public KHBoard(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public KHController Controller
        {
            get => m_Controller;
            set
            {
                if (m_Controller != value)
                {
                    m_Controller?.RemoveBoard(this);
                    m_Controller = value;
                    m_Controller?.AddBoard(this);
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Game != null)
            {
                from.CloseGump<KHBoardGump>();
                from.SendGump(new KHBoardGump(from, m_Game));
            }
            else
            {
                from.SendMessage("There is no King of the Hill game in progress.");
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            writer.Write(m_Controller);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Controller = reader.ReadEntity<KHController>();
                        break;
                    }
            }
        }
    }

    public sealed class KHBoardGump : Gump
    {
        private const int LabelColor32 = 0xFFFFFF;
        private const int BlackColor32 = 0x000000;

        private KHGame m_Game;

        public KHBoardGump(Mobile mob, KHGame game)
            : base(60, 60)
        {
            m_Game = game;

            var ourTeam = game.GetTeamInfo(mob);

            var entries = new List<KHTeamInfo>();

            for (var i = 0; i < game.Context.Participants.Count; ++i)
            {
                var teamInfo = game.Controller.TeamInfo[i % game.Controller.TeamInfo.Length];

                if (teamInfo != null)
                {
                    entries.Add(teamInfo);
                }
            }

            entries.Sort();
            /*
                delegate( IRankedCTF a, IRankedCTF b )
            {
                return b.Score - a.Score;
            } );*/

            var height = 73 + entries.Count * 75 + 28;

            Closable = false;

            AddPage(0);

            AddBackground(1, 1, 398, height, 3600);

            AddImageTiled(16, 15, 369, height - 29, 3604);

            for (var i = 0; i < entries.Count; i += 1)
            {
                AddImageTiled(22, 58 + i * 75, 357, 70, 0x2430);
            }

            AddAlphaRegion(16, 15, 369, height - 29);

            AddImage(215, -45, 0xEE40);
            // AddImage( 330, 141, 0x8BA );

            AddBorderedText(22, 22, 294, 20, Center("King of the Hill Scoreboard"), LabelColor32, BlackColor32);

            AddImageTiled(32, 50, 264, 1, 9107);
            AddImageTiled(42, 52, 264, 1, 9157);

            for (var i = 0; i < entries.Count; ++i)
            {
                var teamInfo = entries[i];

                AddImage(30, 70 + i * 75, 10152);
                AddImage(30, 85 + i * 75, 10151);
                AddImage(30, 100 + i * 75, 10151);
                AddImage(30, 106 + i * 75, 10154);

                AddImage(24, 60 + i * 75, teamInfo == ourTeam ? 9730 : 9727, teamInfo.Color - 1);

                var nameColor = LabelColor32;
                var borderColor = BlackColor32;

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

                AddBorderedText(
                    60,
                    65 + i * 75,
                    250,
                    20,
                    $"{LadderGump.Rank(1 + i)}: {teamInfo.Name}",
                    nameColor,
                    borderColor
                );

                AddBorderedText(50 + 10, 85 + i * 75, 100, 20, "Score:", 0xFFC000, BlackColor32);
                AddBorderedText(50 + 15, 105 + i * 75, 100, 20, teamInfo.Score.ToString("N0"), 0xFFC000, BlackColor32);

                AddBorderedText(110 + 10, 85 + i * 75, 100, 20, "Kills:", 0xFFC000, BlackColor32);
                AddBorderedText(110 + 15, 105 + i * 75, 100, 20, teamInfo.Kills.ToString("N0"), 0xFFC000, BlackColor32);

                AddBorderedText(160 + 10, 85 + i * 75, 100, 20, "Captures:", 0xFFC000, BlackColor32);
                AddBorderedText(160 + 15, 105 + i * 75, 100, 20, teamInfo.Captures.ToString("N0"), 0xFFC000, BlackColor32);

                var leader = teamInfo.Leader?.Name ?? "(none)";

                AddBorderedText(235 + 10, 85 + i * 75, 250, 20, "Leader:", 0xFFC000, BlackColor32);
                AddBorderedText(235 + 15, 105 + i * 75, 250, 20, leader, 0xFFC000, BlackColor32);
            }

            AddButton(314, height - 42, 247, 248, 1);
        }

        public string Center(string text) => $"<CENTER>{text}</CENTER>";

        public string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

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
            {
                AddHtml(x, y, width, height, text);
            }
            else
            {
                AddHtml(x, y, width, height, Color(text, color));
            }
        }
    }

    public sealed class KHPlayerInfo : IRankedCTF, IComparable<KHPlayerInfo>
    {
        private readonly KHTeamInfo m_TeamInfo;
        private int m_Captures;

        private int m_Kills;
        private int m_Score;

        public KHPlayerInfo(KHTeamInfo teamInfo, Mobile player)
        {
            m_TeamInfo = teamInfo;
            Player = player;
        }

        public Mobile Player { get; }

        public int CompareTo(KHPlayerInfo pi)
        {
            var res = pi.Score.CompareTo(Score);
            if (res != 0)
            {
                return res;
            }

            res = pi.Captures.CompareTo(Captures);

            return res != 0 ? res : pi.Kills.CompareTo(Kills);
        }

        public string Name => Player.Name ?? "";

        public int Kills
        {
            get => m_Kills;
            set
            {
                m_TeamInfo.Kills += value - m_Kills;
                m_Kills = value;
            }
        }

        public int Captures
        {
            get => m_Captures;
            set
            {
                m_TeamInfo.Captures += value - m_Captures;
                m_Captures = value;
            }
        }

        public int Score
        {
            get => m_Score;
            set
            {
                m_TeamInfo.Score += value - m_Score;
                m_Score = value;

                if (m_TeamInfo.Leader == null || m_Score > m_TeamInfo.Leader.Score)
                {
                    m_TeamInfo.Leader = this;
                }
            }
        }
    }

    [PropertyObject]
    public sealed class KHTeamInfo : IRankedCTF, IComparable<KHTeamInfo>
    {
        public KHTeamInfo(int teamID)
        {
            TeamID = teamID;
            Players = new Dictionary<Mobile, KHPlayerInfo>();
        }

        public KHTeamInfo(int teamID, IGenericReader ip)
        {
            TeamID = teamID;
            Players = new Dictionary<Mobile, KHPlayerInfo>();

            var version = ip.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        TeamName = ip.ReadString();
                        Color = ip.ReadEncodedInt();
                        break;
                    }
            }
        }

        public KHGame Game { get; set; }

        public int TeamID { get; }

        public KHPlayerInfo Leader { get; set; }

        public Dictionary<Mobile, KHPlayerInfo> Players { get; }

        public KHPlayerInfo this[Mobile mob]
        {
            get
            {
                if (mob == null)
                {
                    return null;
                }

                if (!Players.TryGetValue(mob, out var val))
                {
                    Players[mob] = val = new KHPlayerInfo(this, mob);
                }

                return val;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Color { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TeamName { get; set; }

        public int CompareTo(KHTeamInfo ti)
        {
            var res = ti.Score.CompareTo(Score);
            if (res != 0)
            {
                return res;
            }

            res = ti.Captures.CompareTo(Captures);

            if (res == 0)
            {
                res = ti.Kills.CompareTo(Kills);
            }

            return res;
        }

        public string Name => $"{TeamName ?? "(none)"} Team";

        public int Kills { get; set; }

        public int Captures { get; set; }

        public int Score { get; set; }

        public void Reset()
        {
            Kills = 0;
            Captures = 0;
            Score = 0;

            Leader = null;

            Players.Clear();
        }

        public void Serialize(IGenericWriter op)
        {
            op.WriteEncodedInt(0); // version

            op.Write(TeamName);
            op.WriteEncodedInt(Color);
        }

        public override string ToString() => TeamName != null ? $"({Name}) ..." : "...";
    }

    public sealed class KHController : EventController
    {
        private int m_ScoreInterval;

        [Constructible]
        public KHController()
        {
            Visible = false;
            Movable = false;

            Name = "King of the Hill Controller";

            Duration = TimeSpan.FromMinutes(30.0);
            Boards = new List<KHBoard>();
            Hills = new HillOfTheKing[4];
            TeamInfo = new KHTeamInfo[8];

            for (var i = 0; i < TeamInfo.Length; ++i)
            {
                TeamInfo[i] = new KHTeamInfo(i);
            }
        }

        public KHController(Serial serial)
            : base(serial)
        {
        }

        public KHTeamInfo[] TeamInfo { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team1_W => TeamInfo[0];

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team2_E => TeamInfo[1];

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team3_N => TeamInfo[2];

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team4_S => TeamInfo[3];

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team5_NW => TeamInfo[4];

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team6_SE => TeamInfo[5];

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team7_SW => TeamInfo[6];

        [CommandProperty(AccessLevel.GameMaster)]
        public KHTeamInfo Team8_NE => TeamInfo[7];

        public HillOfTheKing[] Hills { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public HillOfTheKing Hill1
        {
            get => Hills[0];
            set => Hills[0] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HillOfTheKing Hill2
        {
            get => Hills[1];
            set => Hills[1] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HillOfTheKing Hill3
        {
            get => Hills[2];
            set => Hills[2] = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public HillOfTheKing Hill4
        {
            get => Hills[3];
            set => Hills[3] = value;
        }

        public List<KHBoard> Boards { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Duration { get; set; }

        public override string Title => "King of the Hill";

        public override string GetTeamName(int teamID) => TeamInfo[teamID % TeamInfo.Length].Name;

        public override EventGame Construct(DuelContext context) => new KHGame(this, context);

        public void RemoveBoard(KHBoard b)
        {
            if (b != null)
            {
                Boards.Remove(b);
                b.m_Game = null;
            }
        }

        public void AddBoard(KHBoard b)
        {
            if (b != null)
            {
                Boards.Add(b);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            writer.WriteEncodedInt(m_ScoreInterval);
            writer.Write(Duration);

            Boards.Tidy();
            writer.Write(Boards);

            writer.WriteEncodedInt(Hills.Length);
            for (var i = 0; i < Hills.Length; ++i)
            {
                writer.Write(Hills[i]);
            }

            writer.WriteEncodedInt(TeamInfo.Length);
            for (var i = 0; i < TeamInfo.Length; ++i)
            {
                TeamInfo[i].Serialize(writer);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_ScoreInterval = reader.ReadEncodedInt();

                        Duration = reader.ReadTimeSpan();

                        Boards = reader.ReadEntityList<KHBoard>();

                        Hills = new HillOfTheKing[reader.ReadEncodedInt()];
                        for (var i = 0; i < Hills.Length; ++i)
                        {
                            Hills[i] = reader.ReadEntity<HillOfTheKing>();
                        }

                        TeamInfo = new KHTeamInfo[reader.ReadEncodedInt()];
                        for (var i = 0; i < TeamInfo.Length; ++i)
                        {
                            TeamInfo[i] = new KHTeamInfo(i, reader);
                        }

                        break;
                    }
            }
        }
    }

    public sealed class KHGame : EventGame
    {
        private TimerExecutionToken _finishTimerToken;

        public KHGame(KHController controller, DuelContext context) : base(context) => Controller = controller;

        public KHController Controller { get; }

        public Map Facet
        {
            get
            {
                if (m_Context?.Arena != null)
                {
                    return m_Context.Arena.Facet;
                }

                return Controller.Map;
            }
        }

        public override bool CantDoAnything(Mobile mob)
        {
            if (mob != null && GetTeamInfo(mob) != null && Controller != null)
            {
                for (var i = 0; i < Controller.Hills.Length; i++)
                {
                    if (Controller.Hills[i]?.King == mob)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Alert(string text)
        {
            m_Context.m_Tournament?.Alert(text);

            for (var i = 0; i < m_Context.Participants.Count; ++i)
            {
                var p = m_Context.Participants[i];

                for (var j = 0; j < p.Players.Length; ++j)
                {
                    if (p.Players[j] != null)
                    {
                        p.Players[j].Mobile.SendMessage(0x35, text);
                    }
                }
            }
        }

        public KHTeamInfo GetTeamInfo(Mobile mob)
        {
            var teamID = GetTeamID(mob);

            if (teamID >= 0)
            {
                return Controller.TeamInfo[teamID % Controller.TeamInfo.Length];
            }

            return null;
        }

        public int GetTeamID(Mobile mob)
        {
            if (mob is not PlayerMobile pm)
            {
                return mob is BaseCreature creature ? creature.Team - 1 : -1;
            }

            if (pm.DuelContext == null || pm.DuelContext != m_Context)
            {
                return -1;
            }

            if (pm.DuelPlayer?.Eliminated != false)
            {
                return -1;
            }

            return pm.DuelContext.Participants.IndexOf(pm.DuelPlayer.Participant);
        }

        public int GetColor(Mobile mob) => GetTeamInfo(mob)?.Color ?? -1;

        private void ApplyHues(Participant p, int hueOverride)
        {
            for (var i = 0; i < p.Players.Length; ++i)
            {
                if (p.Players[i] != null)
                {
                    p.Players[i].Mobile.SolidHueOverride = hueOverride;
                }
            }
        }

        public void DelayBounce(TimeSpan ts, Mobile mob, Container corpse)
        {
            Timer.StartTimer(ts, () => DelayBounce_Callback(mob, corpse));
        }

        private void DelayBounce_Callback(Mobile mob, Container corpse)
        {
            var dp = (mob as PlayerMobile)?.DuelPlayer;

            m_Context.RemoveAggressions(mob);

            if (dp?.Eliminated == false)
            {
                mob.MoveToWorld(m_Context.Arena.GetBaseStartPoint(GetTeamID(mob)), Facet);
            }
            else
            {
                m_Context.SendOutside(mob);
            }

            m_Context.Refresh(mob, corpse);
            DuelContext.Debuff(mob);
            DuelContext.CancelSpell(mob);
            mob.Frozen = false;

            if (corpse?.Deleted == false)
            {
                Timer.StartTimer(TimeSpan.FromSeconds(30), corpse.Delete);
            }
        }

        public override bool OnDeath(Mobile mob, Container corpse)
        {
            var killer = mob.FindMostRecentDamager(false);
            KHTeamInfo teamInfo = null;
            var victInfo = GetTeamInfo(mob);
            var bonus = 0;

            if (killer?.Player == true)
            {
                teamInfo = GetTeamInfo(killer);
            }

            for (var i = 0; i < Controller.Hills.Length; i++)
            {
                if (Controller.Hills[i] == null)
                {
                    continue;
                }

                if (Controller.Hills[i].King == mob)
                {
                    bonus += Controller.Hills[i].CapturesSoFar;
                    Controller.Hills[i].OnKingDied(mob, victInfo, killer, teamInfo);
                }

                if (Controller.Hills[i].King == killer)
                {
                    bonus += 2;
                }
            }

            if (teamInfo != null && teamInfo != victInfo)
            {
                var playerInfo = teamInfo[killer];

                if (playerInfo != null)
                {
                    playerInfo.Kills += 1;
                    playerInfo.Score += 1 + bonus;
                }
            }

            mob.CloseGump<KHBoardGump>();
            mob.SendGump(new KHBoardGump(mob, this));

            m_Context.Requip(mob, corpse);
            DelayBounce(TimeSpan.FromSeconds(30.0), mob, corpse);

            return false;
        }

        public override void OnStart()
        {
            for (var i = 0; i < Controller.TeamInfo.Length; ++i)
            {
                var teamInfo = Controller.TeamInfo[i];

                teamInfo.Game = this;
                teamInfo.Reset();
            }

            for (var i = 0; i < m_Context.Participants.Count; ++i)
            {
                ApplyHues(
                    m_Context.Participants[i],
                    Controller.TeamInfo[i % Controller.TeamInfo.Length].Color
                );
            }

            for (var i = 0; i < Controller.Hills.Length; i++)
            {
                if (Controller.Hills[i] != null)
                {
                    Controller.Hills[i].Game = this;
                }
            }

            foreach (var board in Controller.Boards)
            {
                if (board?.Deleted == false)
                {
                    board.m_Game = this;
                }
            }

            _finishTimerToken.Cancel();
            Timer.StartTimer(Controller.Duration, Finish_Callback, out _finishTimerToken);
        }

        private void Finish_Callback()
        {
            var teams = new List<KHTeamInfo>();

            for (var i = 0; i < m_Context.Participants.Count; ++i)
            {
                var teamInfo = Controller.TeamInfo[i % Controller.TeamInfo.Length];

                if (teamInfo != null)
                {
                    teams.Add(teamInfo);
                }
            }

            teams.Sort();

            var tourney = m_Context.m_Tournament;

            var sb = new StringBuilder();

            if (tourney != null)
            {
                if (tourney.TourneyType == TourneyType.FreeForAll)
                {
                    sb.Append(m_Context.Participants.Count * tourney.PlayersPerParticipant);
                    sb.Append("-man FFA");
                }
                else if (tourney.TourneyType == TourneyType.RandomTeam)
                {
                    sb.Append(tourney.ParticipantsPerMatch);
                    sb.Append("-team");
                }
                else if (tourney.TourneyType == TourneyType.RedVsBlue)
                {
                    sb.Append("Red v Blue");
                }
                else
                {
                    for (var i = 0; i < tourney.ParticipantsPerMatch; ++i)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append('v');
                        }

                        sb.Append(tourney.PlayersPerParticipant);
                    }
                }
            }

            if (Controller != null)
            {
                sb.Append(' ').Append(Controller.Title);
            }

            var title = sb.ToString();

            var winner = teams.Count > 0 ? teams[0] : null;

            for (var i = 0; i < teams.Count; ++i)
            {
                var rank = TrophyRank.Bronze;

                if (i == 0)
                {
                    rank = TrophyRank.Gold;
                }
                else if (i == 1)
                {
                    rank = TrophyRank.Silver;
                }

                var leader = teams[i].Leader;

                foreach (var pl in teams[i].Players.Values)
                {
                    var mob = pl.Player;

                    if (mob == null)
                    {
                        continue;
                    }

                    sb = new StringBuilder();

                    sb.Append(title);

                    if (pl == leader)
                    {
                        sb.Append(" Leader");
                    }

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
                    {
                        item.ItemID = 4810;
                    }

                    item.Name = $"{item.Name}, {teams[i].Name.ToLower()}";

                    if (!mob.PlaceInBackpack(item))
                    {
                        mob.BankBox.DropItem(item);
                    }

                    var cash = pl.Score * 250;

                    if (cash > 0)
                    {
                        item = new BankCheck(cash);

                        if (!mob.PlaceInBackpack(item))
                        {
                            mob.BankBox.DropItem(item);
                        }

                        mob.SendMessage(
                            $"You have been awarded a {rank.ToString().ToLower()} trophy and {cash:N0}gp for your participation in this game."
                        );
                    }
                    else
                    {
                        mob.SendMessage(
                            $"You have been awarded a {rank.ToString().ToLower()} trophy for your participation in this game."
                        );
                    }
                }
            }

            for (var i = 0; i < m_Context.Participants.Count; ++i)
            {
                var p = m_Context.Participants[i];
                if (p.Players == null)
                {
                    continue;
                }

                for (var j = 0; j < p.Players.Length; ++j)
                {
                    var dp = p.Players[j];

                    if (dp?.Mobile != null)
                    {
                        dp.Mobile.CloseGump<KHBoardGump>();
                        dp.Mobile.SendGump(new KHBoardGump(dp.Mobile, this));
                    }
                }

                if (i == winner?.TeamID)
                {
                    continue;
                }

                if (p.Players != null)
                {
                    for (var j = 0; j < p.Players.Length; ++j)
                    {
                        if (p.Players[j] != null)
                        {
                            p.Players[j].Eliminated = true;
                        }
                    }
                }
            }

            if (winner != null)
            {
                m_Context.Finish(m_Context.Participants[winner.TeamID]);
            }
        }

        public override void OnStop()
        {
            for (var i = 0; i < Controller.TeamInfo.Length; ++i)
            {
                Controller.TeamInfo[i].Game = null;
            }

            for (var i = 0; i < Controller.Hills.Length; ++i)
            {
                if (Controller.Hills[i] != null)
                {
                    Controller.Hills[i].Game = null;
                }
            }

            foreach (var board in Controller.Boards)
            {
                if (board != null)
                {
                    board.m_Game = null;
                }
            }

            for (var i = 0; i < m_Context.Participants.Count; ++i)
            {
                ApplyHues(m_Context.Participants[i], -1);
            }

            _finishTimerToken.Cancel();
        }
    }
}
