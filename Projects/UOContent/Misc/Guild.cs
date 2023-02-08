using System;
using System.Collections.Generic;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Guilds
{
    [Flags]
    public enum RankFlags
    {
        None = 0x00000000,
        CanInvitePlayer = 0x00000001,
        AccessGuildItems = 0x00000002,
        RemoveLowestRank = 0x00000004,
        RemovePlayers = 0x00000008,
        CanPromoteDemote = 0x00000010,
        ControlWarStatus = 0x00000020,
        AllianceControl = 0x00000040,
        CanSetGuildTitle = 0x00000080,
        CanVote = 0x00000100,

        All = Member | CanInvitePlayer | RemovePlayers | CanPromoteDemote | ControlWarStatus | AllianceControl |
              CanSetGuildTitle,
        Member = RemoveLowestRank | AccessGuildItems | CanVote
    }

    public class RankDefinition
    {
        public static RankDefinition[] Ranks =
        {
            new(1062963, 0, RankFlags.None),   // Ronin
            new(1062962, 1, RankFlags.Member), // Member
            new(
                1062961, // Emmissary
                2,
                RankFlags.Member | RankFlags.RemovePlayers | RankFlags.CanInvitePlayer | RankFlags.CanSetGuildTitle |
                RankFlags.CanPromoteDemote
            ),
            new(1062960, 3, RankFlags.Member | RankFlags.ControlWarStatus), // Warlord
            new(1062959, 4, RankFlags.All)                                  // Leader
        };

        public RankDefinition(TextDefinition name, int rank, RankFlags flags)
        {
            Name = name;
            Rank = rank;
            Flags = flags;
        }

        public static RankDefinition Leader => Ranks[4];
        public static RankDefinition Member => Ranks[1];
        public static RankDefinition Lowest => Ranks[0];

        public TextDefinition Name { get; }

        public int Rank { get; }

        public RankFlags Flags { get; private set; }

        public bool GetFlag(RankFlags flag) => (Flags & flag) != 0;

        public void SetFlag(RankFlags flag, bool value)
        {
            if (value)
            {
                Flags |= flag;
            }
            else
            {
                Flags &= ~flag;
            }
        }
    }

    public class AllianceInfo
    {
        private readonly List<Guild> m_Members;
        private readonly List<Guild> m_PendingMembers;
        private Guild m_Leader;

        public AllianceInfo(Guild leader, string name, Guild partner)
        {
            m_Leader = leader;
            Name = name;

            m_Members = new List<Guild>();
            m_PendingMembers = new List<Guild>();

            leader.Alliance = this;
            partner.Alliance = this;
        }

        public AllianceInfo(IGenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Name = reader.ReadString();
                        m_Leader = reader.ReadEntity<Guild>();

                        m_Members = reader.ReadEntityList<Guild>();
                        m_PendingMembers = reader.ReadEntityList<Guild>();

                        break;
                    }
            }

            Timer.DelayCall((alliances, alliance) => alliances.TryAdd(alliance.Name.ToLower(), alliance), Alliances, this);
        }

        public static Dictionary<string, AllianceInfo> Alliances { get; } = new();

        public string Name { get; }

        public Guild Leader
        {
            get
            {
                CheckLeader();
                return m_Leader;
            }
            set
            {
                if (m_Leader != value && value != null)
                {
                    AllianceMessage(1070765, value.Name); // Your Alliance is now led by ~1_GUILDNAME~
                }

                m_Leader = value;

                if (m_Leader == null)
                {
                    CalculateAllianceLeader();
                }
            }
        }

        public void CalculateAllianceLeader()
        {
            m_Leader = m_Members.Count >= 2 ? m_Members.RandomElement() : null;
        }

        public void CheckLeader()
        {
            if (m_Leader?.Disbanded != false)
            {
                CalculateAllianceLeader();

                if (m_Leader == null)
                {
                    Disband();
                }
            }
        }

        public bool IsPendingMember(Guild g) => g.Alliance == this && m_PendingMembers.Contains(g);

        public bool IsMember(Guild g) => g.Alliance == this && m_Members.Contains(g);

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // Version

            writer.Write(Name);
            writer.Write(m_Leader);

            Guild.Tidy(m_Members);
            writer.Write(m_Members);

            Guild.Tidy(m_PendingMembers);
            writer.Write(m_PendingMembers);
        }

        public void AddPendingGuild(Guild g)
        {
            if (g.Alliance != this || m_PendingMembers.Contains(g) || m_Members.Contains(g))
            {
                return;
            }

            m_PendingMembers.Add(g);
        }

        public void TurnToMember(Guild g)
        {
            if (g.Alliance != this || !m_PendingMembers.Contains(g) || m_Members.Contains(g))
            {
                return;
            }

            g.GuildMessage(1070760, Name);    // Your Guild has joined the ~1_ALLIANCENAME~ Alliance.
            AllianceMessage(1070761, g.Name); // A new Guild has joined your Alliance: ~1_GUILDNAME~

            m_PendingMembers.Remove(g);
            m_Members.Add(g);
            g.Alliance.InvalidateMemberProperties();
        }

        public void RemoveGuild(Guild g)
        {
            if (m_PendingMembers.Contains(g))
            {
                m_PendingMembers.Remove(g);
            }

            if (m_Members.Contains(g)) // Sanity, just incase someone with a custom script adds a character to BOTH arrays
            {
                m_Members.Remove(g);
                g.InvalidateMemberProperties();

                g.GuildMessage(1070763, Name);    // Your Guild has been removed from the ~1_ALLIANCENAME~ Alliance.
                AllianceMessage(1070764, g.Name); // A Guild has left your Alliance: ~1_GUILDNAME~
            }

            // g.Alliance = null;	//NO G.Alliance call here.  Set the Guild's Alliance to null, if you JUST use RemoveGuild, it removes it from the alliance, but doesn't remove the link from the guild to the alliance.  setting g.Alliance will call this method.
            // to check on OSI: have 3 guilds, make 2 of them a member, one pending.  remove one of the memebers.  alliance still exist?
            // ANSWER: NO

            if (g == m_Leader)
            {
                CalculateAllianceLeader();
            }

            if (m_Members.Count < 2)
            {
                Disband();
            }
        }

        public void Disband()
        {
            AllianceMessage(1070762); // Your Alliance has dissolved.

            for (var i = 0; i < m_PendingMembers.Count; i++)
            {
                m_PendingMembers[i].Alliance = null;
            }

            for (var i = 0; i < m_Members.Count; i++)
            {
                m_Members[i].Alliance = null;
            }

            var lowerName = Name.ToLower();

            if (Alliances.TryGetValue(lowerName, out var aInfo) && aInfo == this)
            {
                Alliances.Remove(lowerName);
            }
        }

        public void InvalidateMemberProperties(bool onlyOPL = false)
        {
            for (var i = 0; i < m_Members.Count; i++)
            {
                var g = m_Members[i];

                g.InvalidateMemberProperties(onlyOPL);
            }
        }

        public void InvalidateMemberNotoriety()
        {
            for (var i = 0; i < m_Members.Count; i++)
            {
                m_Members[i].InvalidateMemberNotoriety();
            }
        }

        public void AllianceMessage(int number)
        {
            for (var i = 0; i < m_Members.Count; ++i)
            {
                m_Members[i].GuildMessage(number);
            }
        }

        public void AllianceMessage(int number, string args, int hue = 0x3B2)
        {
            for (var i = 0; i < m_Members.Count; ++i)
            {
                m_Members[i].GuildMessage(number, args, hue);
            }
        }

        public void AllianceMessage(int number, bool append, string affix, string args = "", int hue = 0x3B2)
        {
            for (var i = 0; i < m_Members.Count; ++i)
            {
                m_Members[i].GuildMessage(number, append, affix, args, hue);
            }
        }

        public void AllianceTextMessage(string text) => AllianceTextMessage(0x3B2, text);

        public void AllianceTextMessage(int hue, string text)
        {
            for (var i = 0; i < m_Members.Count; ++i)
            {
                m_Members[i].GuildTextMessage(hue, text);
            }
        }

        public void AllianceChat(Mobile from, int hue, string text)
        {
            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text)].InitializePacket();

            for (var i = 0; i < m_Members.Count; i++)
            {
                var g = m_Members[i];

                for (var j = 0; j < g.Members.Count; j++)
                {
                    var length = OutgoingMessagePackets.CreateMessage(
                        buffer,
                        from.Serial,
                        from.Body,
                        MessageType.Alliance,
                        hue,
                        3,
                        false,
                        from.Language,
                        from.Name,
                        text
                    );

                    if (length != buffer.Length)
                    {
                        buffer = buffer[..length]; // Adjust to the actual size
                    }

                    g.Members[j].NetState?.Send(buffer);
                }
            }
        }

        public void AllianceChat(Mobile from, string text)
        {
            var pm = from as PlayerMobile;

            AllianceChat(from, pm?.AllianceMessageHue ?? 0x3B2, text);
        }

        public class AllianceRosterGump : GuildDiplomacyGump
        {
            private readonly AllianceInfo m_Alliance;

            public AllianceRosterGump(PlayerMobile pm, Guild g, AllianceInfo alliance) : base(
                pm,
                g,
                true,
                "",
                0,
                alliance.m_Members,
                alliance.Name
            ) =>
                m_Alliance = alliance;

            public AllianceRosterGump(
                PlayerMobile pm, Guild g, AllianceInfo alliance, IComparer<Guild> currentComparer,
                bool ascending, string filter, int startNumber
            ) : base(
                pm,
                g,
                currentComparer,
                ascending,
                filter,
                startNumber,
                alliance.m_Members,
                alliance.Name
            ) =>
                m_Alliance = alliance;

            protected override bool AllowAdvancedSearch => false;

            public override Gump GetResentGump(
                PlayerMobile pm, Guild g, IComparer<Guild> comparer, bool ascending,
                string filter, int startNumber
            ) =>
                new AllianceRosterGump(pm, g, m_Alliance, comparer, ascending, filter, startNumber);

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (info.ButtonID != 8) // So that they can't get to the AdvancedSearch button
                {
                    base.OnResponse(sender, info);
                }
            }
        }
    }

    public enum WarStatus
    {
        InProgress = -1,
        Win,
        Lose,
        Draw,
        Pending
    }

    public class WarDeclaration
    {
        public WarDeclaration(Guild g, Guild opponent, int maxKills, TimeSpan warLength, bool warRequester)
        {
            Guild = g;
            MaxKills = maxKills;
            Opponent = opponent;
            WarLength = warLength;
            WarRequester = warRequester;
        }

        public WarDeclaration(IGenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Kills = reader.ReadInt();
                        MaxKills = reader.ReadInt();

                        WarLength = reader.ReadTimeSpan();
                        WarBeginning = reader.ReadDateTime();

                        Guild = reader.ReadEntity<Guild>();
                        Opponent = reader.ReadEntity<Guild>();

                        WarRequester = reader.ReadBool();

                        break;
                    }
            }
        }

        public int Kills { get; set; }

        public int MaxKills { get; set; }

        public TimeSpan WarLength { get; set; }

        public Guild Opponent { get; }

        public Guild Guild { get; }

        public DateTime WarBeginning { get; set; }

        public bool WarRequester { get; set; }

        public WarStatus Status
        {
            get
            {
                if (Opponent?.Disbanded != false)
                {
                    return WarStatus.Win;
                }

                if (Guild?.Disbanded != false)
                {
                    return WarStatus.Lose;
                }

                var w = Opponent.FindActiveWar(Guild);

                if (Opponent.FindPendingWar(Guild) != null && Guild.FindPendingWar(Opponent) != null)
                {
                    return WarStatus.Pending;
                }

                if (w == null)
                {
                    return WarStatus.Win;
                }

                if (WarLength != TimeSpan.Zero && WarBeginning + WarLength < Core.Now)
                {
                    if (Kills > w.Kills)
                    {
                        return WarStatus.Win;
                    }

                    return Kills < w.Kills ? WarStatus.Lose : WarStatus.Draw;
                }

                if (MaxKills > 0)
                {
                    if (Kills >= MaxKills)
                    {
                        return WarStatus.Win;
                    }

                    if (w.Kills >= w.MaxKills)
                    {
                        return WarStatus.Lose;
                    }
                }

                return WarStatus.InProgress;
            }
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version

            writer.Write(Kills);
            writer.Write(MaxKills);

            writer.Write(WarLength);
            writer.Write(WarBeginning);

            writer.Write(Guild);
            writer.Write(Opponent);

            writer.Write(WarRequester);
        }
    }

    public class WarTimer : Timer
    {
        public WarTimer() : base(TimeSpan.FromMinutes(1.0), TimeSpan.FromMinutes(1.0))
        {
        }

        public static void Initialize()
        {
            if (Guild.NewGuildSystem)
            {
                new WarTimer().Start();
            }
        }

        protected override void OnTick()
        {
            foreach (var g in World.Guilds.Values)
            {
                (g as Guild)?.CheckExpiredWars();
            }
        }
    }

    public class Guild : BaseGuild
    {
        public static readonly int RegistrationFee = 25000;
        public static readonly int AbbrevLimit = 4;
        public static readonly int NameLimit = 40;
        public static readonly int MajorityPercentage = 66;
        public static readonly TimeSpan InactiveTime = TimeSpan.FromDays(30);
        private string m_Abbreviation;

        private AllianceInfo m_AllianceInfo;
        private Guild m_AllianceLeader;

        private Mobile m_Leader;

        private string m_Name;

        private GuildType m_Type;

        public Guild(Mobile leader, string name, string abbreviation)
        {
            m_Leader = leader;

            Members = new List<Mobile>();
            Allies = new List<Guild>();
            Enemies = new List<Guild>();
            WarDeclarations = new List<Guild>();
            WarInvitations = new List<Guild>();
            AllyDeclarations = new List<Guild>();
            AllyInvitations = new List<Guild>();
            Candidates = new List<Mobile>();
            Accepted = new List<Mobile>();

            LastFealty = Core.Now;

            m_Name = name;
            m_Abbreviation = abbreviation;

            TypeLastChange = DateTime.MinValue;

            AddMember(m_Leader);

            if (m_Leader is PlayerMobile mobile)
            {
                mobile.GuildRank = RankDefinition.Leader;
            }

            AcceptedWars = new List<WarDeclaration>();
            PendingWars = new List<WarDeclaration>();
        }

        public Guild(Serial serial) : base(serial)
        {
        }

        public static bool NewGuildSystem => Core.SE;
        public static bool OrderChaos => !Core.SE;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Leader
        {
            get
            {
                if (Disbanded || m_Leader.Guild != this)
                {
                    CalculateGuildmaster();
                }

                return m_Leader;
            }
            set
            {
                if (value != null)
                {
                    AddMember(value); // Also removes from old guild.
                }

                if (m_Leader is PlayerMobile leader && leader.Guild == this)
                {
                    leader.GuildRank = RankDefinition.Member;
                }

                m_Leader = value;

                if (m_Leader is PlayerMobile mobile)
                {
                    mobile.GuildRank = RankDefinition.Leader;
                }
            }
        }

        public override bool Disbanded => m_Leader?.Deleted != false;

        public AllianceInfo Alliance
        {
            get => m_AllianceInfo ?? m_AllianceLeader?.m_AllianceInfo;
            set
            {
                var current = Alliance;

                if (value == current)
                {
                    return;
                }

                current?.RemoveGuild(this);

                if (value != null)
                {
                    if (value.Leader == this)
                    {
                        m_AllianceInfo = value;
                    }
                    else
                    {
                        m_AllianceLeader = value.Leader;
                    }

                    value.AddPendingGuild(this);
                }
                else
                {
                    m_AllianceInfo = null;
                    m_AllianceLeader = null;
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public string AllianceName => Alliance?.Name;

        [CommandProperty(AccessLevel.Counselor)]
        public Guild AllianceLeader => Alliance?.Leader;

        [CommandProperty(AccessLevel.Counselor)]
        public bool IsAllianceMember => Alliance?.IsMember(this) == true;

        [CommandProperty(AccessLevel.Counselor)]
        public bool IsAlliancePendingMember => Alliance?.IsPendingMember(this) == true;

        public List<WarDeclaration> PendingWars { get; private set; }

        public List<WarDeclaration> AcceptedWars { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Guildstone { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Teleporter { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public override string Name
        {
            get => m_Name;
            set
            {
                m_Name = value;

                InvalidateMemberProperties(true);

                Guildstone?.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Website { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public override string Abbreviation
        {
            get => m_Abbreviation;
            set
            {
                m_Abbreviation = value;

                InvalidateMemberProperties(true);

                Guildstone?.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Charter { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public override GuildType Type
        {
            get => OrderChaos ? m_Type : GuildType.Regular;
            set
            {
                if (m_Type != value)
                {
                    m_Type = value;
                    TypeLastChange = Core.Now;

                    InvalidateMemberProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastFealty { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime TypeLastChange { get; private set; }

        public List<Guild> Allies { get; private set; }

        public List<Guild> Enemies { get; private set; }

        public List<Guild> AllyDeclarations { get; private set; }

        public List<Guild> AllyInvitations { get; private set; }

        public List<Guild> WarDeclarations { get; private set; }

        public List<Guild> WarInvitations { get; private set; }

        public List<Mobile> Candidates { get; private set; }

        public List<Mobile> Accepted { get; private set; }

        public List<Mobile> Members { get; private set; }

        public static void Configure()
        {
            EventSink.GuildGumpRequest += EventSink_GuildGumpRequest;

            CommandSystem.Register("GuildProps", AccessLevel.Counselor, GuildProps_OnCommand);
        }

        public void InvalidateMemberProperties(bool onlyOPL = false)
        {
            for (var i = 0; i < Members?.Count; i++)
            {
                var m = Members[i];
                m.InvalidateProperties();

                if (!onlyOPL)
                {
                    m.Delta(MobileDelta.Noto);
                }
            }
        }

        public void InvalidateMemberNotoriety()
        {
            for (var i = 0; i < Members?.Count; i++)
            {
                Members[i].Delta(MobileDelta.Noto);
            }
        }

        public void InvalidateWarNotoriety()
        {
            var g = GetAllianceLeader(this);

            if (g.Alliance != null)
            {
                g.Alliance.InvalidateMemberNotoriety();
            }
            else
            {
                g.InvalidateMemberNotoriety();
            }

            if (g.AcceptedWars == null)
            {
                return;
            }

            foreach (var warDec in g.AcceptedWars)
            {
                var opponent = warDec.Opponent;

                if (opponent.Alliance != null)
                {
                    opponent.Alliance.InvalidateMemberNotoriety();
                }
                else
                {
                    opponent.InvalidateMemberNotoriety();
                }
            }
        }

        public override void OnDelete(Mobile mob)
        {
            RemoveMember(mob);
        }

        public void Disband()
        {
            m_Leader = null;

            World.RemoveGuild(this);

            foreach (var m in Members)
            {
                m.SendLocalizedMessage(502131); // Your guild has disbanded.

                if (m is PlayerMobile mobile)
                {
                    mobile.GuildRank = RankDefinition.Lowest;
                }

                m.Guild = null;
            }

            Members.Clear();

            for (var i = Allies.Count - 1; i >= 0; --i)
            {
                if (i < Allies.Count)
                {
                    RemoveAlly(Allies[i]);
                }
            }

            for (var i = Enemies.Count - 1; i >= 0; --i)
            {
                if (i < Enemies.Count)
                {
                    RemoveEnemy(Enemies[i]);
                }
            }

            if (!NewGuildSystem)
            {
                Guildstone?.Delete();
            }

            Guildstone = null;

            CheckExpiredWars();

            Alliance = null;
        }

        [Usage("GuildProps"), Description(
             "Opens a menu where you can view and edit guild properties of a targeted player or guild stone.  If the new Guild system is active, also brings up the guild gump."
         )]
        private static void GuildProps_OnCommand(CommandEventArgs e)
        {
            var arg = e.ArgString.Trim();
            var from = e.Mobile;

            if (arg.Length == 0)
            {
                e.Mobile.Target = new GuildPropsTarget();
            }
            else
            {
                var g = uint.TryParse(arg, out var id)
                    ? World.FindGuild((Serial)id) as Guild
                    : FindByAbbrev(arg) as Guild ?? FindByName(arg) as Guild;

                if (g != null)
                {
                    from.SendGump(new PropertiesGump(from, g));

                    if (NewGuildSystem && from.AccessLevel >= AccessLevel.GameMaster && from is PlayerMobile mobile)
                    {
                        mobile.SendGump(new GuildInfoGump(mobile, g));
                    }
                }
            }
        }

        public static void EventSink_GuildGumpRequest(Mobile m)
        {
            if (!NewGuildSystem || m is not PlayerMobile pm)
            {
                return;
            }

            if (pm.Guild == null)
            {
                pm.SendGump(new CreateGuildGump(pm));
            }
            else
            {
                pm.SendGump(new GuildInfoGump(pm, pm.Guild as Guild));
            }
        }

        public static Guild GetAllianceLeader(Guild g)
        {
            var alliance = g.Alliance;

            if (alliance?.Leader != null && alliance.IsMember(g))
            {
                return alliance.Leader;
            }

            return g;
        }

        public WarDeclaration FindPendingWar(Guild g)
        {
            for (var i = 0; i < PendingWars.Count; i++)
            {
                var w = PendingWars[i];

                if (w.Opponent == g)
                {
                    return w;
                }
            }

            return null;
        }

        public WarDeclaration FindActiveWar(Guild g)
        {
            for (var i = 0; i < AcceptedWars.Count; i++)
            {
                var w = AcceptedWars[i];

                if (w.Opponent == g)
                {
                    return w;
                }
            }

            return null;
        }

        public void CheckExpiredWars()
        {
            for (var i = 0; i < AcceptedWars.Count; i++)
            {
                var w = AcceptedWars[i];
                var g = w.Opponent;

                var status = w.Status;

                if (status != WarStatus.InProgress)
                {
                    var myAlliance = Alliance;
                    var inAlliance = myAlliance?.IsMember(this) == true;

                    var otherAlliance = g?.Alliance;
                    var otherInAlliance = otherAlliance?.IsMember(this) == true;

                    if (inAlliance)
                    {
                        myAlliance.AllianceMessage(
                            1070739 + (int)status,
                            g == null ? "a deleted opponent" :
                            otherInAlliance ? otherAlliance.Name : g.Name
                        );
                        myAlliance.InvalidateMemberProperties();
                    }
                    else
                    {
                        GuildMessage(
                            1070739 + (int)status,
                            g == null ? "a deleted opponent" :
                            otherInAlliance ? otherAlliance.Name : g.Name
                        );
                        InvalidateMemberProperties();
                    }

                    AcceptedWars.Remove(w);

                    if (g == null)
                    {
                        continue;
                    }

                    if (status != WarStatus.Draw)
                    {
                        status = (WarStatus)((int)status + 1 % 2);
                    }

                    if (otherInAlliance)
                    {
                        otherAlliance.AllianceMessage(1070739 + (int)status, inAlliance ? Alliance.Name : Name);
                        otherAlliance.InvalidateMemberProperties();
                    }
                    else
                    {
                        g.GuildMessage(1070739 + (int)status, inAlliance ? Alliance.Name : Name);
                        g.InvalidateMemberProperties();
                    }

                    g.AcceptedWars.Remove(g.FindActiveWar(this));
                }
            }

            for (var i = 0; i < PendingWars.Count; i++)
            {
                var w = PendingWars[i];
                var g = w.Opponent;

                if (w.Status != WarStatus.Pending)
                {
                    // All sanity in here
                    PendingWars.Remove(w);

                    g?.PendingWars.Remove(g.FindPendingWar(this));
                }
            }
        }

        public static void HandleDeath(Mobile victim, Mobile killer = null)
        {
            if (!NewGuildSystem)
            {
                return;
            }

            killer ??= victim.FindMostRecentDamager(false);

            if (killer?.Guild == null || victim.Guild == null)
            {
                return;
            }

            var victimGuild = GetAllianceLeader(victim.Guild as Guild);
            var killerGuild = GetAllianceLeader(killer.Guild as Guild);

            var war = killerGuild.FindActiveWar(victimGuild);

            if (war == null)
            {
                return;
            }

            war.Kills++;

            if (war.Opponent == victimGuild)
            {
                killerGuild.CheckExpiredWars();
            }
            else
            {
                victimGuild.CheckExpiredWars();
            }
        }

        public bool IsMember(Mobile m) => Members.Contains(m);

        public bool IsAlly(Guild g) =>
            NewGuildSystem ? Alliance?.IsMember(this) == true && Alliance.IsMember(g) : Allies.Contains(g);

        public bool IsEnemy(Guild g) =>
            Type != GuildType.Regular && g.Type != GuildType.Regular && Type != g.Type || IsWar(g);

        public bool IsWar(Guild g)
        {
            if (g == null)
            {
                return false;
            }

            if (!NewGuildSystem)
            {
                return Enemies.Contains(g);
            }

            var guild = GetAllianceLeader(this);
            var otherGuild = GetAllianceLeader(g);

            return guild.FindActiveWar(otherGuild) != null;
        }

        public static void Tidy(List<Guild> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var entry = list[i];
                if (entry?.Disbanded != false)
                {
                    list.RemoveAt(i);
                }
            }

            list.TrimExcess();
        }

        public override void Serialize(IGenericWriter writer)
        {
            if (LastFealty + TimeSpan.FromDays(1.0) < Core.Now)
            {
                CalculateGuildmaster();
            }

            CheckExpiredWars();

            Alliance?.CheckLeader();

            writer.Write(5); // version

            writer.Write(PendingWars.Count);

            for (var i = 0; i < PendingWars.Count; i++)
            {
                PendingWars[i].Serialize(writer);
            }

            writer.Write(AcceptedWars.Count);

            for (var i = 0; i < AcceptedWars.Count; i++)
            {
                AcceptedWars[i].Serialize(writer);
            }

            var isAllianceLeader = m_AllianceLeader == null && m_AllianceInfo != null;
            writer.Write(isAllianceLeader);

            if (isAllianceLeader)
            {
                m_AllianceInfo.Serialize(writer);
            }
            else
            {
                writer.Write(m_AllianceLeader);
            }

            //

            Tidy(AllyDeclarations);
            writer.Write(AllyDeclarations);
            Tidy(AllyInvitations);
            writer.Write(AllyInvitations);

            writer.Write(TypeLastChange);

            writer.Write((int)m_Type);

            writer.Write(LastFealty);

            writer.Write(m_Leader);
            writer.Write(m_Name);
            writer.Write(m_Abbreviation);

            Tidy(Allies);
            writer.Write(Allies);
            Tidy(Enemies);
            writer.Write(Enemies);
            Tidy(WarDeclarations);
            writer.Write(WarDeclarations);
            Tidy(WarInvitations);
            writer.Write(WarInvitations);

            Members.Tidy();
            writer.Write(Members);
            Candidates.Tidy();
            writer.Write(Candidates);
            Accepted.Tidy();
            writer.Write(Accepted);

            writer.Write(Guildstone);
            writer.Write(Teleporter);

            writer.Write(Charter);
            writer.Write(Website);
        }

        public override bool ShouldExecuteAfterSerialize => false;

        public override void AfterSerialize()
        {
        }

        public override void Delete()
        {
            World.RemoveGuild(this);
        }

        public override void Deserialize(IGenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 5:
                    {
                        var count = reader.ReadInt();

                        PendingWars = new List<WarDeclaration>();
                        for (var i = 0; i < count; i++)
                        {
                            PendingWars.Add(new WarDeclaration(reader));
                        }

                        count = reader.ReadInt();
                        AcceptedWars = new List<WarDeclaration>();
                        for (var i = 0; i < count; i++)
                        {
                            AcceptedWars.Add(new WarDeclaration(reader));
                        }

                        var isAllianceLeader = reader.ReadBool();

                        if (isAllianceLeader)
                        {
                            m_AllianceInfo = new AllianceInfo(reader);
                        }
                        else
                        {
                            m_AllianceLeader = reader.ReadEntity<Guild>();
                        }

                        goto case 4;
                    }
                case 4:
                    {
                        AllyDeclarations = reader.ReadEntityList<Guild>();
                        AllyInvitations = reader.ReadEntityList<Guild>();

                        goto case 3;
                    }
                case 3:
                    {
                        TypeLastChange = reader.ReadDateTime();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Type = (GuildType)reader.ReadInt();

                        goto case 1;
                    }
                case 1:
                    {
                        LastFealty = reader.ReadDateTime();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Leader = reader.ReadEntity<Mobile>();

                        if (m_Leader is PlayerMobile mobile)
                        {
                            mobile.GuildRank = RankDefinition.Leader;
                        }

                        m_Name = reader.ReadString();
                        m_Abbreviation = reader.ReadString();

                        Allies = reader.ReadEntityList<Guild>();
                        Enemies = reader.ReadEntityList<Guild>();
                        WarDeclarations = reader.ReadEntityList<Guild>();
                        WarInvitations = reader.ReadEntityList<Guild>();

                        Members = reader.ReadEntityList<Mobile>();
                        Candidates = reader.ReadEntityList<Mobile>();
                        Accepted = reader.ReadEntityList<Mobile>();

                        Guildstone = reader.ReadEntity<Item>();
                        Teleporter = reader.ReadEntity<Item>();

                        Charter = reader.ReadString();
                        Website = reader.ReadString();

                        break;
                    }
            }

            AllyDeclarations ??= new List<Guild>();
            AllyInvitations ??= new List<Guild>();
            AcceptedWars ??= new List<WarDeclaration>();
            PendingWars ??= new List<WarDeclaration>();

            Timer.StartTimer(VerifyGuild_Callback);
        }

        private void VerifyGuild_Callback()
        {
            if (!NewGuildSystem && Guildstone == null || Members.Count == 0)
            {
                Disband();
            }

            CheckExpiredWars();

            var alliance = Alliance;

            alliance?.CheckLeader();

            alliance = Alliance; // CheckLeader could possibly change the value of this.Alliance

            // This block is there to fix a bug in the code in an older version.
            if (alliance?.IsMember(this) == false && !alliance.IsPendingMember(this))
            {
                Alliance = null; // Will call Alliance.RemoveGuild which will set it null & perform all the pertinent checks as far as alliance disbanding
            }
        }

        public void AddMember(Mobile m)
        {
            if (!Members.Contains(m))
            {
                if (m.Guild != null && m.Guild != this)
                {
                    ((Guild)m.Guild).RemoveMember(m);
                }

                Members.Add(m);
                m.Guild = this;

                m.GuildFealty = !NewGuildSystem ? m_Leader : null;

                if (m is PlayerMobile mobile)
                {
                    mobile.GuildRank = RankDefinition.Lowest;
                }

                ((Guild)m.Guild).InvalidateWarNotoriety();
            }
        }

        public void RemoveMember(Mobile m, int message = 1018028) // You have been dismissed from your guild.
        {
            if (Members.Contains(m))
            {
                Members.Remove(m);

                var guild = m.Guild as Guild;

                m.Guild = null;

                if (m is PlayerMobile mobile)
                {
                    mobile.GuildRank = RankDefinition.Lowest;
                }

                if (message > 0)
                {
                    m.SendLocalizedMessage(message);
                }

                if (m == m_Leader)
                {
                    CalculateGuildmaster();

                    if (m_Leader == null)
                    {
                        Disband();
                    }
                }

                if (Members.Count == 0)
                {
                    Disband();
                }

                guild?.InvalidateWarNotoriety();

                m.Delta(MobileDelta.Noto);
            }
        }

        public void AddAlly(Guild g)
        {
            if (!Allies.Contains(g))
            {
                Allies.Add(g);

                g.AddAlly(this);
            }
        }

        public void RemoveAlly(Guild g)
        {
            if (Allies.Contains(g))
            {
                Allies.Remove(g);

                g.RemoveAlly(this);
            }
        }

        public void AddEnemy(Guild g)
        {
            if (!Enemies.Contains(g))
            {
                Enemies.Add(g);

                g.AddEnemy(this);
            }
        }

        public void RemoveEnemy(Guild g)
        {
            if (Enemies.Contains(g))
            {
                Enemies.Remove(g);

                g.RemoveEnemy(this);
            }
        }

        public void GuildMessage(int number)
        {
            for (var i = 0; i < Members.Count; ++i)
            {
                Members[i].SendLocalizedMessage(number);
            }
        }

        public void GuildMessage(int number, string args, int hue = 0x3B2)
        {
            for (var i = 0; i < Members.Count; ++i)
            {
                Members[i].SendLocalizedMessage(number, args, hue);
            }
        }

        public void GuildMessage(int number, bool append, string affix, string args = "", int hue = 0x3B2)
        {
            for (var i = 0; i < Members.Count; ++i)
            {
                Members[i].SendLocalizedMessage(number, append, affix, args, hue);
            }
        }

        public void GuildTextMessage(string text) => GuildTextMessage(0x3B2, text);

        public void GuildTextMessage(int hue, string text)
        {
            for (var i = 0; i < Members.Count; ++i)
            {
                Members[i].SendMessage(hue, text);
            }
        }

        public void GuildChat(Mobile from, int hue, string text)
        {
            Span<byte> buffer = stackalloc byte[OutgoingMessagePackets.GetMaxMessageLength(text)].InitializePacket();

            for (var i = 0; i < Members.Count; i++)
            {
                var length = OutgoingMessagePackets.CreateMessage(
                    buffer, from.Serial, from.Body, MessageType.Guild, hue, 3, false, from.Language,
                    from.Name, text
                );

                if (length != buffer.Length)
                {
                    buffer = buffer[..length]; // Adjust to the actual size
                }

                Members[i].NetState?.Send(buffer);
            }
        }

        public void GuildChat(Mobile from, string text)
        {
            GuildChat(from, (from as PlayerMobile)?.GuildMessageHue ?? 0x3B2, text);
        }

        public bool CanVote(Mobile m) =>
            (!NewGuildSystem || m is PlayerMobile pm && pm.GuildRank.GetFlag(RankFlags.CanVote)) &&
            m?.Deleted == false && m.Guild == this;

        public bool CanBeVotedFor(Mobile m) =>
            (!NewGuildSystem || m is PlayerMobile pm && pm.LastOnline + InactiveTime >= Core.Now) &&
            m?.Deleted == false && m.Guild == this;

        public void CalculateGuildmaster()
        {
            var votes = new Dictionary<Mobile, int>();

            var votingMembers = 0;

            for (var i = 0; i < Members?.Count; ++i)
            {
                var memb = Members[i];

                if (!CanVote(memb))
                {
                    continue;
                }

                var m = memb.GuildFealty;

                if (!CanBeVotedFor(m))
                {
                    if (!Disbanded && m_Leader.Guild == this)
                    {
                        m = m_Leader;
                    }
                    else
                    {
                        m = memb;
                    }
                }

                if (m == null)
                {
                    continue;
                }

                votes[m] = 1 + (votes.TryGetValue(m, out var v) ? v : 0);
                votingMembers++;
            }

            Mobile winner = null;
            var highVotes = 0;

            foreach (var kvp in votes)
            {
                var m = kvp.Key;
                var val = kvp.Value;

                if (winner == null || val > highVotes)
                {
                    winner = m;
                    highVotes = val;
                }
            }

            if (NewGuildSystem && highVotes * 100 / Math.Max(votingMembers, 1) < MajorityPercentage && !Disbanded &&
                winner != m_Leader && m_Leader.Guild == this)
            {
                winner = m_Leader;
            }

            if (m_Leader != winner && winner != null)
            {
                Leader = winner;
                GuildMessage(1018015, true, winner.RawName); // Guild Message: Guildmaster changed to:
            }

            LastFealty = Core.Now;
        }

        private class GuildPropsTarget : Target
        {
            public GuildPropsTarget() : base(-1, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (!BaseCommand.IsAccessible(from, o))
                {
                    from.SendLocalizedMessage(500447); // That is not accessible.
                    return;
                }

                Guild g = null;

                if (o is Guildstone stone)
                {
                    if (stone.Guild.Disbanded)
                    {
                        from.SendMessage("The guild associated with that Guildstone no longer exists");
                        return;
                    }

                    g = stone.Guild;
                }
                else if (o is Mobile mobile)
                {
                    g = mobile.Guild as Guild;
                }

                if (g == null)
                {
                    from.SendMessage("That is not in a guild!");
                    return;
                }

                from.SendGump(new PropertiesGump(from, g));

                if (NewGuildSystem && from.AccessLevel >= AccessLevel.GameMaster && from is PlayerMobile pm)
                {
                    pm.SendGump(new GuildInfoGump(pm, g));
                }
            }
        }
    }
}
