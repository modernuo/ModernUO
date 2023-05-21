using System;
using System.Collections.Generic;
using System.Text;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.ConPVP
{
    public sealed class CTFBoard : Item
    {
        public CTFTeamInfo m_TeamInfo;

        [Constructible]
        public CTFBoard()
            : base(7774) =>
            Movable = false;

        public CTFBoard(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "scoreboard";

        public override void OnDoubleClick(Mobile from)
        {
            if (m_TeamInfo?.Game != null)
            {
                from.CloseGump<CTFBoardGump>();
                from.SendGump(new CTFBoardGump(from, m_TeamInfo.Game));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class CTFBoardGump : Gump
    {
        private const int LabelColor32 = 0xFFFFFF;
        private const int BlackColor32 = 0x000000;

        private CTFGame m_Game;

        public CTFBoardGump(Mobile mob, CTFGame game, CTFTeamInfo section = null)
            : base(60, 60)
        {
            m_Game = game;

            var ourTeam = game.GetTeamInfo(mob);

            var entries = new List<IRankedCTF>();

            if (section == null)
            {
                for (var i = 0; i < game.Context.Participants.Count; ++i)
                {
                    var teamInfo = game.Controller.TeamInfo[i % 8];

                    if (teamInfo?.Flag == null)
                    {
                        continue;
                    }

                    entries.Add(teamInfo);
                }
            }
            else
            {
                foreach (var player in section.Players.Values)
                {
                    if (player.Score > 0)
                    {
                        entries.Add(player);
                    }
                }
            }

            entries.Sort((a, b) => b.Score - a.Score);

            var height = 0;

            if (section == null)
            {
                height = 73 + entries.Count * 75 + 28;
            }

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

            AddBorderedText(22, 22, 294, 20, Center("CTF Scoreboard"), LabelColor32, BlackColor32);

            AddImageTiled(32, 50, 264, 1, 9107);
            AddImageTiled(42, 52, 264, 1, 9157);

            if (section == null)
            {
                for (var i = 0; i < entries.Count; ++i)
                {
                    var teamInfo = entries[i] as CTFTeamInfo;

                    if (teamInfo == null)
                    {
                        continue;
                    }

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
                        $"{LadderGump.Rank(1 + i)}: {teamInfo.Name} Team",
                        nameColor,
                        borderColor
                    );

                    AddBorderedText(50 + 10, 85 + i * 75, 100, 20, "Score:", 0xFFC000, BlackColor32);
                    AddBorderedText(50 + 15, 105 + i * 75, 100, 20, teamInfo.Score.ToString("N0"), 0xFFC000, BlackColor32);

                    AddBorderedText(110 + 10, 85 + i * 75, 100, 20, "Kills:", 0xFFC000, BlackColor32);
                    AddBorderedText(110 + 15, 105 + i * 75, 100, 20, teamInfo.Kills.ToString("N0"), 0xFFC000, BlackColor32);

                    AddBorderedText(160 + 10, 85 + i * 75, 100, 20, "Captures:", 0xFFC000, BlackColor32);
                    AddBorderedText(
                        160 + 15,
                        105 + i * 75,
                        100,
                        20,
                        teamInfo.Captures.ToString("N0"),
                        0xFFC000,
                        BlackColor32
                    );

                    var pl = teamInfo.Leader;

                    AddBorderedText(235 + 10, 85 + i * 75, 250, 20, "Leader:", 0xFFC000, BlackColor32);

                    if (pl != null)
                    {
                        AddBorderedText(235 + 15, 105 + i * 75, 250, 20, pl.Player.Name, 0xFFC000, BlackColor32);
                    }
                }
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

    public sealed class CTFFlag : Item
    {
        public Mobile m_Fragger;
        public DateTime m_FragTime;

        private int m_ReturnCount;

        public Mobile m_Returner;
        public DateTime m_ReturnTime;
        private TimerExecutionToken _returnTimerToken;
        public CTFTeamInfo m_TeamInfo;

        [Constructible]
        public CTFFlag()
            : base(5643) =>
            Movable = false;

        public CTFFlag(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "old people cookies";

        public override void OnDoubleClick(Mobile from)
        {
            if (m_TeamInfo?.Game != null)
            {
                var ourTeam = m_TeamInfo;
                var useTeam = m_TeamInfo.Game.GetTeamInfo(from);

                if (ourTeam == null || useTeam == null)
                {
                    return;
                }

                if (IsChildOf(from.Backpack))
                {
                    from.BeginTarget(1, false, TargetFlags.None, Flag_OnTarget);
                }
                else if (!from.InRange(this, 1) || !from.InLOS(this))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x26, 1019045); // I can't reach that
                }
                else if (ourTeam == useTeam)
                {
                    if (Location == m_TeamInfo.Origin && Map == m_TeamInfo.Game.Facet)
                    {
                        from.NetState.SendMessage(
                            Serial,
                            ItemID,
                            MessageType.Regular,
                            0x3B2,
                            3,
                            false,
                            "ENU",
                            Name,
                            "Touch me not for I am chaste."
                        );
                    }
                    else
                    {
                        var playerInfo = useTeam[from];

                        if (playerInfo != null)
                        {
                            playerInfo.Score += 4; // return
                        }

                        m_Returner = from;
                        m_ReturnTime = Core.Now;

                        SendHome();

                        from.LocalOverheadMessage(MessageType.Regular, 0x59, false, "You returned the cookies!");
                        m_TeamInfo.Game.Alert($"The {from.Name} cookies have been returned by {ourTeam.Name}.");
                    }
                }
                else if (!from.PlaceInBackpack(this))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x26, false, "I can't hold that.");
                }
                else
                {
                    from.RevealingAction();

                    from.LocalOverheadMessage(MessageType.Regular, 0x59, false, "You stole the cookies!");
                    m_TeamInfo.Game.Alert($"The {ourTeam.Name} cookies have been stolen by {from.Name} ({useTeam.Name}).");

                    BeginCountdown(120);
                }
            }
        }

        public override void Delete()
        {
            if (Parent != null)
            {
                SendHome();
                return;
            }

            base.Delete();
        }

        public void DropTo(Mobile mob, Mobile killer)
        {
            m_Fragger = killer;
            m_FragTime = Core.Now;

            if (mob != null)
            {
                MoveToWorld(new Point3D(mob.X, mob.Y, mob.Z + 2), mob.Map);

                m_ReturnCount = Math.Min(m_ReturnCount, 10);
            }
            else
            {
                SendHome();
            }
        }

        private void BeginCountdown(int returnCount)
        {
            _returnTimerToken.Cancel();

            Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), Countdown_OnTick, out _returnTimerToken);
            m_ReturnCount = returnCount;
        }

        private void Countdown_OnTick()
        {
            var owner = RootParent as Mobile;

            switch (m_ReturnCount)
            {
                case 60:
                case 30:
                case 15:
                case 10:
                case 5:
                case 4:
                case 3:
                case 2:
                case 1:
                    {
                        if (m_ReturnCount == 1)
                        {
                            owner?.SendMessage(0x26, $"You have {m_ReturnCount} second to capture the cookies!");
                        }
                        else
                        {
                            owner?.SendMessage(0x26, $"You have {m_ReturnCount} seconds to capture the cookies!");
                        }

                        break;
                    }

                case 0:
                    {
                        if (owner != null)
                        {
                            owner.SendMessage(0x26, "You have taken too long to capture the cookies!");
                            owner.Kill();
                        }

                        SendHome();

                        m_TeamInfo?.Game?.Alert($"The {m_TeamInfo.Name} cookies have been returned.");

                        return;
                    }
            }

            --m_ReturnCount;
        }

        private void Flag_OnTarget(Mobile from, object obj)
        {
            if (m_TeamInfo == null)
            {
                return;
            }

            if (!IsChildOf(from.Backpack))
            {
                return;
            }

            var ourTeam = m_TeamInfo;
            var useTeam = m_TeamInfo.Game.GetTeamInfo(from);

            if (obj is CTFFlag)
            {
                if (obj == useTeam.Flag)
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x59, false, "You captured the cookies!");
                    m_TeamInfo.Game.Alert($"{from.Name} captured the {ourTeam.Name} cookies!");

                    SendHome();

                    var playerInfo = useTeam[from];

                    if (playerInfo != null)
                    {
                        playerInfo.Captures += 1;
                        playerInfo.Score += 50; // capture

                        var teamFlag = useTeam.Flag;

                        if (teamFlag.m_Fragger != null &&
                            Core.Now < teamFlag.m_FragTime + TimeSpan.FromSeconds(5.0) &&
                            m_TeamInfo.Game.GetTeamInfo(teamFlag.m_Fragger) == useTeam)
                        {
                            var assistInfo = useTeam[teamFlag.m_Fragger];

                            if (assistInfo != null)
                            {
                                assistInfo.Score += 6; // frag assist
                            }
                        }

                        if (teamFlag.m_Returner != null &&
                            Core.Now < teamFlag.m_ReturnTime + TimeSpan.FromSeconds(5.0))
                        {
                            var assistInfo = useTeam[teamFlag.m_Returner];

                            if (assistInfo != null)
                            {
                                assistInfo.Score += 4; // return assist
                            }
                        }
                    }
                }
                else
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x26, false, "Those are not my cookies.");
                }
            }
            else if (obj is Mobile passTo)
            {
                var passTeam = m_TeamInfo.Game.GetTeamInfo(passTo);

                if (passTo == from)
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x26, false, "I can't pass to them.");
                }
                else if (passTeam == useTeam && passTo.PlaceInBackpack(this))
                {
                    passTo.LocalOverheadMessage(
                        MessageType.Regular,
                        0x59,
                        false,
                        $"{from.Name} has passed you the cookies!"
                    );
                }
                else
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x26, false, "I can't pass to them.");
                }
            }
        }

        public void SendHome()
        {
            _returnTimerToken.Cancel();

            if (m_TeamInfo == null)
            {
                return;
            }

            MoveToWorld(m_TeamInfo.Origin, m_TeamInfo.Game.Facet);
        }

        private Mobile FindOwner(IEntity parent)
        {
            if (parent is Item item)
            {
                return item.RootParent as Mobile;
            }

            if (parent is Mobile mobile)
            {
                return mobile;
            }

            return null;
        }

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);

            var mob = FindOwner(parent);

            if (mob != null)
            {
                mob.SolidHueOverride = 0x4001;
            }
        }

        public override void OnRemoved(IEntity parent)
        {
            base.OnRemoved(parent);

            var mob = FindOwner(parent);

            if (mob != null)
            {
                mob.SolidHueOverride = m_TeamInfo?.Game.GetColor(mob) ?? -1;
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public interface IRankedCTF
    {
        int Kills { get; }
        int Captures { get; }
        int Score { get; }
        string Name { get; }
    }

    public sealed class CTFPlayerInfo : IRankedCTF
    {
        private readonly CTFTeamInfo m_TeamInfo;
        private int m_Captures;

        private int m_Kills;

        private int m_Score;

        public CTFPlayerInfo(CTFTeamInfo teamInfo, Mobile player)
        {
            m_TeamInfo = teamInfo;
            Player = player;
        }

        public Mobile Player { get; }

        string IRankedCTF.Name => Player.Name;

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
    public sealed class CTFTeamInfo : IRankedCTF
    {
        public CTFTeamInfo(int teamID)
        {
            TeamID = teamID;
            Players = new Dictionary<Mobile, CTFPlayerInfo>();
        }

        public CTFTeamInfo(int teamID, IGenericReader ip)
        {
            TeamID = teamID;
            Players = new Dictionary<Mobile, CTFPlayerInfo>();

            var version = ip.ReadEncodedInt();

            switch (version)
            {
                case 2:
                    {
                        Board = ip.ReadEntity<CTFBoard>();

                        goto case 1;
                    }
                case 1:
                    {
                        Name = ip.ReadString();

                        goto case 0;
                    }
                case 0:
                    {
                        Color = ip.ReadEncodedInt();

                        Flag = ip.ReadEntity<CTFFlag>();
                        Origin = ip.ReadPoint3D();
                        break;
                    }
            }
        }

        public CTFGame Game { get; set; }

        public int TeamID { get; }

        public CTFPlayerInfo Leader { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public CTFBoard Board { get; set; }

        public Dictionary<Mobile, CTFPlayerInfo> Players { get; }

        public CTFPlayerInfo this[Mobile mob]
        {
            get
            {
                if (mob == null)
                {
                    return null;
                }

                if (!Players.TryGetValue(mob, out var val))
                {
                    Players[mob] = val = new CTFPlayerInfo(this, mob);
                }

                return val;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Color { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Name { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public CTFFlag Flag { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Origin { get; set; }

        string IRankedCTF.Name => $"{Name} Team";

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

            if (Flag != null)
            {
                Flag.m_TeamInfo = this;
                Flag.Hue = Color;
                Flag.SendHome();
            }

            if (Board != null)
            {
                Board.m_TeamInfo = this;
            }
        }

        public void Serialize(IGenericWriter op)
        {
            op.WriteEncodedInt(2); // version

            op.Write(Board);

            op.Write(Name);

            op.WriteEncodedInt(Color);

            op.Write(Flag);
            op.Write(Origin);
        }

        public override string ToString() => "...";
    }

    public sealed class CTFController : EventController
    {
        [Constructible]
        public CTFController()
        {
            Visible = false;
            Movable = false;

            Duration = TimeSpan.FromMinutes(30.0);

            TeamInfo = new CTFTeamInfo[8];

            for (var i = 0; i < TeamInfo.Length; ++i)
            {
                TeamInfo[i] = new CTFTeamInfo(i);
            }
        }

        public CTFController(Serial serial)
            : base(serial)
        {
        }

        public CTFTeamInfo[] TeamInfo { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public CTFTeamInfo Team1 => TeamInfo[0];

        [CommandProperty(AccessLevel.GameMaster)]
        public CTFTeamInfo Team2 => TeamInfo[1];

        [CommandProperty(AccessLevel.GameMaster)]
        public CTFTeamInfo Team3 => TeamInfo[2];

        [CommandProperty(AccessLevel.GameMaster)]
        public CTFTeamInfo Team4 => TeamInfo[3];

        [CommandProperty(AccessLevel.GameMaster)]
        public CTFTeamInfo Team5 => TeamInfo[4];

        [CommandProperty(AccessLevel.GameMaster)]
        public CTFTeamInfo Team6 => TeamInfo[5];

        [CommandProperty(AccessLevel.GameMaster)]
        public CTFTeamInfo Team7 => TeamInfo[6];

        [CommandProperty(AccessLevel.GameMaster)]
        public CTFTeamInfo Team8 => TeamInfo[7];

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Duration { get; set; }

        public override string Title => "CTF";

        public override string GetTeamName(int teamID) => TeamInfo[teamID % TeamInfo.Length].Name;

        public override EventGame Construct(DuelContext context) => new CTFGame(this, context);

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(2);

            writer.Write(Duration);

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
                case 2:
                    {
                        Duration = reader.ReadTimeSpan();

                        goto case 1;
                    }
                case 1:
                    {
                        TeamInfo = new CTFTeamInfo[reader.ReadEncodedInt()];

                        for (var i = 0; i < TeamInfo.Length; ++i)
                        {
                            TeamInfo[i] = new CTFTeamInfo(i, reader);
                        }

                        break;
                    }
                case 0:
                    {
                        TeamInfo = new CTFTeamInfo[8];

                        for (var i = 0; i < TeamInfo.Length; ++i)
                        {
                            TeamInfo[i] = new CTFTeamInfo(i);
                        }

                        break;
                    }
            }

            if (version < 2)
            {
                Duration = TimeSpan.FromMinutes(30.0);
            }
        }
    }

    public sealed class CTFGame : EventGame
    {
        private TimerExecutionToken _finishTimerToken;

        public CTFGame(CTFController controller, DuelContext context) : base(context) => Controller = controller;

        public CTFController Controller { get; }

        public Map Facet
        {
            get
            {
                if (m_Context.Arena != null)
                {
                    return m_Context.Arena.Facet;
                }

                return Controller.Map;
            }
        }

        public static void Initialize()
        {
            for (var i = 0x7C9; i <= 0x7D0; ++i)
            {
                TileData.ItemTable[i].Flags |= TileFlag.NoShoot;
            }
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

        public CTFTeamInfo GetTeamInfo(Mobile mob)
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
                return -1;
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

        public int GetColor(Mobile mob)
        {
            var teamInfo = GetTeamInfo(mob);

            if (teamInfo != null)
            {
                return teamInfo.Color;
            }

            return -1;
        }

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
        }

        public override bool OnDeath(Mobile mob, Container corpse)
        {
            var killer = mob.FindMostRecentDamager(false);

            var hadFlag = false;

            corpse.FindItemsByType<CTFFlag>(false)
                .ForEach(
                    flag =>
                    {
                        hadFlag = true;
                        flag.DropTo(mob, killer);
                    }
                );

            mob.Backpack?.FindItemsByType<CTFFlag>(false)
                .ForEach(
                    flag =>
                    {
                        hadFlag = true;
                        flag.DropTo(mob, killer);
                    }
                );

            if (killer?.Player == true)
            {
                var teamInfo = GetTeamInfo(killer);
                var victInfo = GetTeamInfo(mob);

                if (teamInfo != null && teamInfo != victInfo)
                {
                    var playerInfo = teamInfo[killer];

                    if (playerInfo != null)
                    {
                        playerInfo.Kills += 1;
                        playerInfo.Score += 1; // base frag

                        if (hadFlag)
                        {
                            playerInfo.Score += 4; // fragged flag carrier
                        }

                        if (mob.InRange(teamInfo.Origin, 24) && mob.Map == Facet)
                        {
                            playerInfo.Score += 1; // fragged in base -- guarding
                        }

                        for (var i = 0; i < Controller.TeamInfo.Length; ++i)
                        {
                            if (Controller.TeamInfo[i] == teamInfo)
                            {
                                continue;
                            }

                            Mobile ourFlagCarrier = null;

                            if (Controller.TeamInfo[i].Flag != null)
                            {
                                ourFlagCarrier = Controller.TeamInfo[i].Flag.RootParent as Mobile;
                            }

                            if (ourFlagCarrier != null && GetTeamInfo(ourFlagCarrier) == teamInfo)
                            {
                                foreach (var aggr in ourFlagCarrier.Aggressors)
                                {
                                    if (aggr.Defender == ourFlagCarrier && aggr.Attacker == mob)
                                    {
                                        playerInfo.Score += 2; // helped defend guy capturing enemy flag
                                        break;
                                    }
                                }

                                if (mob.Map == ourFlagCarrier.Map && ourFlagCarrier.InRange(mob, 12))
                                {
                                    playerInfo.Score += 1; // helped defend guy capturing enemy flag
                                }
                            }
                        }
                    }
                }
            }

            mob.CloseGump<CTFBoardGump>();
            mob.SendGump(new CTFBoardGump(mob, this));

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
                ApplyHues(m_Context.Participants[i], Controller.TeamInfo[i % 8].Color);
            }

            _finishTimerToken.Cancel();
            Timer.StartTimer(Controller.Duration, Finish_Callback, out _finishTimerToken);
        }

        private void Finish_Callback()
        {
            var teams = new List<CTFTeamInfo>();

            for (var i = 0; i < m_Context.Participants.Count; ++i)
            {
                var teamInfo = Controller.TeamInfo[i % 8];

                if (teamInfo?.Flag == null)
                {
                    continue;
                }

                teams.Add(teamInfo);
            }

            teams.Sort((a, b) => b.Score - a.Score);

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
                else if (tourney.TourneyType == TourneyType.Faction)
                {
                    sb.Append(tourney.ParticipantsPerMatch);
                    sb.Append("-team Faction");
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

                    // "Red v Blue CTF Champion"

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

                        if (pl.Kills > 0)
                        {
                            sb.Append(", ");
                            sb.Append(pl.Kills.ToString("N0"));
                            sb.Append(pl.Kills == 1 ? " kill" : " kills");
                        }

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

                    item.Name = $"{item.Name}, {teams[i].Name.ToLower()} team";

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
                            $"You have been awarded a {rank.ToString().ToLower()} trophy and {cash:N0}gp for your participation in this tournament."
                        );
                    }
                    else
                    {
                        mob.SendMessage(
                            $"You have been awarded a {rank.ToString().ToLower()} trophy for your participation in this tournament."
                        );
                    }
                }
            }

            for (var i = 0; i < m_Context.Participants.Count; ++i)
            {
                var p = m_Context.Participants[i];

                for (var j = 0; j < p.Players.Length; ++j)
                {
                    var dp = p.Players[j];

                    if (dp?.Mobile != null)
                    {
                        dp.Mobile.CloseGump<CTFBoardGump>();
                        dp.Mobile.SendGump(new CTFBoardGump(dp.Mobile, this));
                    }
                }

                if (i == winner?.TeamID)
                {
                    continue;
                }

                for (var j = 0; j < p.Players.Length; ++j)
                {
                    if (p.Players[j] != null)
                    {
                        p.Players[j].Eliminated = true;
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
                var teamInfo = Controller.TeamInfo[i];

                if (teamInfo.Flag != null)
                {
                    teamInfo.Flag.SendHome();
                    teamInfo.Flag.m_TeamInfo = null;
                }

                if (teamInfo.Board != null)
                {
                    teamInfo.Board.m_TeamInfo = null;
                }

                teamInfo.Game = null;
            }

            for (var i = 0; i < m_Context.Participants.Count; ++i)
            {
                ApplyHues(m_Context.Participants[i], -1);
            }

            _finishTimerToken.Cancel();
        }
    }
}
