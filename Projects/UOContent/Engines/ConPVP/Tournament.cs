using System;
using System.Collections.Generic;
using System.Text;
using Server.Factions;
using Server.Items;
using Server.Network;
using Server.Regions;

namespace Server.Engines.ConPVP
{
    public enum TournamentStage
    {
        Inactive,
        Signup,
        Fighting
    }

    public enum GroupingType
    {
        HighVsLow,
        Nearest,
        Random
    }

    public enum TieType
    {
        Random,
        Highest,
        Lowest,
        FullElimination,
        FullAdvancement
    }

    public enum TourneyType
    {
        Standard,
        FreeForAll,
        RandomTeam,
        RedVsBlue,
        Faction
    }

    [PropertyObject]
    public class Tournament
    {
        private static readonly TimeSpan SliceInterval = TimeSpan.FromSeconds(12.0);
        private int m_ParticipantsPerMatch;
        private int m_PlayersPerParticipant;

        public Tournament(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 5:
                    {
                        FactionRestricted = reader.ReadBool();

                        goto case 4;
                    }
                case 4:
                    {
                        EventController = reader.ReadEntity<EventController>();

                        goto case 3;
                    }
                case 3:
                    {
                        SuddenDeathRounds = reader.ReadEncodedInt();

                        goto case 2;
                    }
                case 2:
                    {
                        TourneyType = (TourneyType)reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        GroupType = (GroupingType)reader.ReadEncodedInt();
                        TieType = (TieType)reader.ReadEncodedInt();
                        SignupPeriod = reader.ReadTimeSpan();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 3)
                        {
                            SuddenDeathRounds = 3;
                        }

                        m_ParticipantsPerMatch = reader.ReadEncodedInt();
                        m_PlayersPerParticipant = reader.ReadEncodedInt();
                        SignupPeriod = reader.ReadTimeSpan();
                        CurrentStage = TournamentStage.Inactive;
                        Pyramid = new TourneyPyramid();
                        Ruleset = new Ruleset(RulesetLayout.Root);
                        Ruleset.ApplyDefault(Ruleset.Layout.Defaults[0]);
                        Participants = new List<TourneyParticipant>();
                        Undefeated = new List<TourneyParticipant>();
                        Arenas = new List<Arena>();

                        break;
                    }
            }

            Timer.StartTimer(SliceInterval, SliceInterval, Slice);
        }

        public Tournament()
        {
            m_ParticipantsPerMatch = 2;
            m_PlayersPerParticipant = 1;
            Pyramid = new TourneyPyramid();
            Ruleset = new Ruleset(RulesetLayout.Root);
            Ruleset.ApplyDefault(Ruleset.Layout.Defaults[0]);
            Participants = new List<TourneyParticipant>();
            Undefeated = new List<TourneyParticipant>();
            Arenas = new List<Arena>();
            SignupPeriod = TimeSpan.FromMinutes(10.0);

            Timer.StartTimer(SliceInterval, SliceInterval, Slice);
        }

        public bool IsNotoRestricted => TourneyType != TourneyType.Standard;

        [CommandProperty(AccessLevel.GameMaster)]
        public EventController EventController { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SuddenDeathRounds { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TourneyType TourneyType { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public GroupingType GroupType { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TieType TieType { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan SuddenDeath { get; set; }

        public Ruleset Ruleset { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ParticipantsPerMatch
        {
            get => m_ParticipantsPerMatch;
            set => m_ParticipantsPerMatch = Math.Clamp(value, 2, 10);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PlayersPerParticipant
        {
            get => m_PlayersPerParticipant;
            set => m_PlayersPerParticipant = Math.Clamp(value, 1, 10);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LevelRequirement { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool FactionRestricted { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan SignupPeriod { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime SignupStart { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TournamentStage CurrentStage { get; private set; }

        public TournamentStage Stage
        {
            get => CurrentStage;
            set => CurrentStage = value;
        }

        public TourneyPyramid Pyramid { get; set; }

        public List<Arena> Arenas { get; set; }

        public List<TourneyParticipant> Participants { get; set; }

        public List<TourneyParticipant> Undefeated { get; set; }

        public bool IsFactionRestricted => FactionRestricted || TourneyType == TourneyType.Faction;

        public bool HasParticipant(Mobile mob)
        {
            for (var i = 0; i < Participants.Count; ++i)
            {
                if (Participants[i].Players.Contains(mob))
                {
                    return true;
                }
            }

            return false;
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(5); // version

            writer.Write(FactionRestricted);

            writer.Write(EventController);

            writer.WriteEncodedInt(SuddenDeathRounds);

            writer.WriteEncodedInt((int)TourneyType);

            writer.WriteEncodedInt((int)GroupType);
            writer.WriteEncodedInt((int)TieType);
            writer.Write(SuddenDeath);

            writer.WriteEncodedInt(m_ParticipantsPerMatch);
            writer.WriteEncodedInt(m_PlayersPerParticipant);
            writer.Write(SignupPeriod);
        }

        public void HandleTie(Arena arena, TourneyMatch match, List<TourneyParticipant> remaining)
        {
            if (remaining.Count == 1)
            {
                HandleWon(arena, match, remaining[0]);
            }

            if (remaining.Count < 2)
            {
                return;
            }

            var sb = new StringBuilder();

            sb.Append("The match has ended in a tie ");

            sb.Append(remaining.Count == 2 ? "between " : "among ");

            sb.Append(remaining.Count);

            sb.Append(remaining[0].Players.Count == 1 ? " players: " : " teams: ");

            var hasAppended = false;

            for (var j = 0; j < match.Participants.Count; ++j)
            {
                var part = match.Participants[j];

                if (remaining.Contains(part))
                {
                    if (hasAppended)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(part.NameList);
                    hasAppended = true;
                }
                else
                {
                    Undefeated.Remove(part);
                }
            }

            sb.Append(". ");

            var whole = remaining.Count == 2 ? "both" : "all";

            var tieType = TieType;

            if (tieType == TieType.FullElimination && remaining.Count >= Undefeated.Count)
            {
                tieType = TieType.FullAdvancement;
            }

            switch (tieType)
            {
                case TieType.FullAdvancement:
                    {
                        sb.AppendFormat("In accordance with the rules, {0} parties are advanced.", whole);
                        break;
                    }
                case TieType.FullElimination:
                    {
                        for (var j = 0; j < remaining.Count; ++j)
                        {
                            Undefeated.Remove(remaining[j]);
                        }

                        sb.AppendFormat("In accordance with the rules, {0} parties are eliminated.", whole);
                        break;
                    }
                case TieType.Random:
                    {
                        var advanced = remaining.RandomElement();

                        for (var i = 0; i < remaining.Count; ++i)
                        {
                            if (remaining[i] != advanced)
                            {
                                Undefeated.Remove(remaining[i]);
                            }
                        }

                        if (advanced != null)
                        {
                            sb.AppendFormat(
                                "In accordance with the rules, {0} {1} advanced.",
                                advanced.NameList,
                                advanced.Players.Count == 1 ? "is" : "are"
                            );
                        }

                        break;
                    }
                case TieType.Highest:
                    {
                        TourneyParticipant advanced = null;

                        for (var i = 0; i < remaining.Count; ++i)
                        {
                            var part = remaining[i];

                            if (advanced == null || part.TotalLadderXP > advanced.TotalLadderXP)
                            {
                                advanced = part;
                            }
                        }

                        for (var i = 0; i < remaining.Count; ++i)
                        {
                            if (remaining[i] != advanced)
                            {
                                Undefeated.Remove(remaining[i]);
                            }
                        }

                        if (advanced != null)
                        {
                            sb.AppendFormat(
                                "In accordance with the rules, {0} {1} advanced.",
                                advanced.NameList,
                                advanced.Players.Count == 1 ? "is" : "are"
                            );
                        }

                        break;
                    }
                case TieType.Lowest:
                    {
                        TourneyParticipant advanced = null;

                        for (var i = 0; i < remaining.Count; ++i)
                        {
                            var part = remaining[i];

                            if (advanced == null || part.TotalLadderXP < advanced.TotalLadderXP)
                            {
                                advanced = part;
                            }
                        }

                        for (var i = 0; i < remaining.Count; ++i)
                        {
                            if (remaining[i] != advanced)
                            {
                                Undefeated.Remove(remaining[i]);
                            }
                        }

                        if (advanced != null)
                        {
                            sb.AppendFormat(
                                "In accordance with the rules, {0} {1} advanced.",
                                advanced.NameList,
                                advanced.Players.Count == 1 ? "is" : "are"
                            );
                        }

                        break;
                    }
            }

            Alert(arena, sb.ToString());
        }

        public void OnEliminated(DuelPlayer player)
        {
            var part = player.Participant;

            if (!part.Eliminated)
            {
                return;
            }

            if (TourneyType == TourneyType.FreeForAll)
            {
                var rem = 0;

                for (var i = 0; i < part.Context.Participants.Count; ++i)
                {
                    if (part.Context.Participants[i]?.Eliminated == false)
                    {
                        ++rem;
                    }
                }

                var tp = part.TourneyPart;

                if (tp == null)
                {
                    return;
                }

                if (rem == 1)
                {
                    GiveAwards(tp.Players, TrophyRank.Silver, ComputeCashAward() / 2);
                }
                else if (rem == 2)
                {
                    GiveAwards(tp.Players, TrophyRank.Bronze, ComputeCashAward() / 4);
                }
            }
        }

        public void HandleWon(Arena arena, TourneyMatch match, TourneyParticipant winner)
        {
            var sb = new StringBuilder();

            sb.Append("The match is complete. ");
            sb.Append(winner.NameList);

            if (winner.Players.Count > 1)
            {
                sb.Append(" have bested ");
            }
            else
            {
                sb.Append(" has bested ");
            }

            if (match.Participants.Count > 2)
            {
                sb.AppendFormat(
                    "{0} other {1}: ",
                    match.Participants.Count - 1,
                    winner.Players.Count == 1 ? "players" : "teams"
                );
            }

            var hasAppended = false;

            for (var j = 0; j < match.Participants.Count; ++j)
            {
                var part = match.Participants[j];

                if (part == winner)
                {
                    continue;
                }

                Undefeated.Remove(part);

                if (hasAppended)
                {
                    sb.Append(", ");
                }

                sb.Append(part.NameList);
                hasAppended = true;
            }

            sb.Append('.');

            if (TourneyType == TourneyType.Standard)
            {
                Alert(arena, sb.ToString());
            }
        }

        private int ComputeCashAward() => Participants.Count * m_PlayersPerParticipant * 2500;

        private void GiveAwards()
        {
            switch (TourneyType)
            {
                case TourneyType.FreeForAll:
                    {
                        if (Pyramid.Levels.Count < 1)
                        {
                            break;
                        }

                        var top = Pyramid.Levels[^1];

                        if (top.FreeAdvance != null || top.Matches.Count != 1)
                        {
                            break;
                        }

                        var match = top.Matches[0];
                        var winner = match.Winner;

                        if (winner != null)
                        {
                            GiveAwards(winner.Players, TrophyRank.Gold, ComputeCashAward());
                        }

                        break;
                    }
                case TourneyType.Standard:
                    {
                        if (Pyramid.Levels.Count < 2)
                        {
                            break;
                        }

                        var top = Pyramid.Levels[^1];

                        if (top.FreeAdvance != null || top.Matches.Count != 1)
                        {
                            break;
                        }

                        var cash = ComputeCashAward();

                        var match = top.Matches[0];
                        var winner = match.Winner;

                        for (var i = 0; i < match.Participants.Count; ++i)
                        {
                            var part = match.Participants[i];

                            if (part == winner)
                            {
                                GiveAwards(part.Players, TrophyRank.Gold, cash);
                            }
                            else
                            {
                                GiveAwards(part.Players, TrophyRank.Silver, cash / 2);
                            }
                        }

                        var next = Pyramid.Levels[^2];

                        if (next.Matches.Count > 2)
                        {
                            break;
                        }

                        for (var i = 0; i < next.Matches.Count; ++i)
                        {
                            match = next.Matches[i];
                            winner = match.Winner;

                            for (var j = 0; j < match.Participants.Count; ++j)
                            {
                                var part = match.Participants[j];

                                if (part != winner)
                                {
                                    GiveAwards(part.Players, TrophyRank.Bronze, cash / 4);
                                }
                            }
                        }

                        break;
                    }
            }
        }

        private void GiveAwards(List<Mobile> players, TrophyRank rank, int cash)
        {
            if (players.Count == 0)
            {
                return;
            }

            if (players.Count > 1)
            {
                cash /= players.Count - 1;
            }

            cash += 500;
            cash /= 1000;
            cash *= 1000;

            var sb = new StringBuilder();

            if (TourneyType == TourneyType.FreeForAll)
            {
                sb.Append(Participants.Count * m_PlayersPerParticipant);
                sb.Append("-man FFA");
            }
            else if (TourneyType == TourneyType.RandomTeam)
            {
                sb.Append(m_ParticipantsPerMatch);
                sb.Append("-Team");
            }
            else if (TourneyType == TourneyType.Faction)
            {
                sb.Append(m_ParticipantsPerMatch);
                sb.Append("-Team Faction");
            }
            else if (TourneyType == TourneyType.RedVsBlue)
            {
                sb.Append("Red v Blue");
            }
            else
            {
                for (var i = 0; i < m_ParticipantsPerMatch; ++i)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append('v');
                    }

                    sb.Append(m_PlayersPerParticipant);
                }
            }

            if (EventController != null)
            {
                sb.Append(' ').Append(EventController.Title);
            }

            sb.Append(" Champion");

            var title = sb.ToString();

            for (var i = 0; i < players.Count; ++i)
            {
                var mob = players[i];

                if (mob?.Deleted != false)
                {
                    continue;
                }

                Item item = new Trophy(title, rank);

                if (!mob.PlaceInBackpack(item))
                {
                    mob.BankBox.DropItem(item);
                }

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

        public void Slice()
        {
            if (CurrentStage == TournamentStage.Signup)
            {
                var until = SignupStart + SignupPeriod - Core.Now;

                if (until <= TimeSpan.Zero)
                {
                    for (var i = Participants.Count - 1; i >= 0; --i)
                    {
                        var part = Participants[i];
                        var bad = false;

                        for (var j = 0; j < part.Players.Count; ++j)
                        {
                            var check = part.Players[j];

                            if (check.Deleted || check.Map == null || check.Map == Map.Internal || !check.Alive ||
                                Sigil.ExistsOn(check) || check.Region.IsPartOf<JailRegion>())
                            {
                                bad = true;
                                break;
                            }
                        }

                        if (bad)
                        {
                            for (var j = 0; j < part.Players.Count; ++j)
                            {
                                part.Players[j].SendMessage("You have been disqualified from the tournament.");
                            }

                            Participants.RemoveAt(i);
                        }
                    }

                    if (Participants.Count >= 2)
                    {
                        CurrentStage = TournamentStage.Fighting;

                        Undefeated.Clear();

                        Pyramid.Levels.Clear();
                        Pyramid.AddLevel(m_ParticipantsPerMatch, Participants, GroupType, TourneyType);

                        var level = Pyramid.Levels[0];

                        if (level.FreeAdvance != null)
                        {
                            Undefeated.Add(level.FreeAdvance);
                        }

                        for (var i = 0; i < level.Matches.Count; ++i)
                        {
                            var match = level.Matches[i];

                            Undefeated.AddRange(match.Participants);
                        }

                        Alert("Hear ye! Hear ye!", "The tournament will begin shortly.");
                    }
                    else
                    {
                        /*Alert( "Is this all?", "Pitiful. Signup extended." );
                        m_SignupStart = Core.Now;*/

                        Alert("Is this all?", "Pitiful. Tournament cancelled.");
                        CurrentStage = TournamentStage.Inactive;
                    }
                }
                else if (Math.Abs(until.TotalSeconds - TimeSpan.FromMinutes(1.0).TotalSeconds) <
                         SliceInterval.TotalSeconds / 2)
                {
                    Alert("Last call!", "If you wish to enter the tournament, sign up with the registrar now.");
                }
                else if (Math.Abs(until.TotalSeconds - TimeSpan.FromMinutes(5.0).TotalSeconds) <
                         SliceInterval.TotalSeconds / 2)
                {
                    Alert("The tournament will begin in 5 minutes.", "Sign up now before it's too late.");
                }
            }
            else if (CurrentStage == TournamentStage.Fighting)
            {
                if (Undefeated.Count == 1)
                {
                    var winner = Undefeated[0];

                    try
                    {
                        if (EventController != null)
                        {
                            Alert(
                                "The tournament has completed!",
                                $"Team {EventController.GetTeamName(Pyramid.Levels[0].Matches[0].Participants.IndexOf(winner))} has won!"
                            );
                        }
                        else if (TourneyType == TourneyType.RandomTeam)
                        {
                            Alert(
                                "The tournament has completed!",
                                $"Team {Pyramid.Levels[0].Matches[0].Participants.IndexOf(winner) + 1} has won!"
                            );
                        }
                        else if (TourneyType == TourneyType.Faction)
                        {
                            if (m_ParticipantsPerMatch == 4)
                            {
                                var name = Pyramid.Levels[0].Matches[0].Participants.IndexOf(winner) switch
                                {
                                    0 => "Minax",
                                    1 => "Council of Mages",
                                    2 => "True Britannians",
                                    3 => "Shadowlords",
                                    _ => "(null)"
                                };

                                Alert("The tournament has completed!", $"The {name} team has won!");
                            }
                            else if (m_ParticipantsPerMatch == 2)
                            {
                                Alert(
                                    "The tournament has completed!",
                                    $"The {(Pyramid.Levels[0].Matches[0].Participants.IndexOf(winner) == 0 ? "Evil" : "Hero")} team has won!"
                                );
                            }
                            else
                            {
                                Alert(
                                    "The tournament has completed!",
                                    $"Team {Pyramid.Levels[0].Matches[0].Participants.IndexOf(winner) + 1} has won!"
                                );
                            }
                        }
                        else if (TourneyType == TourneyType.RedVsBlue)
                        {
                            Alert(
                                "The tournament has completed!",
                                $"Team {(Pyramid.Levels[0].Matches[0].Participants.IndexOf(winner) == 0 ? "Red" : "Blue")} has won!"
                            );
                        }
                        else
                        {
                            Alert(
                                "The tournament has completed!",
                                $"{winner.NameList} {(winner.Players.Count > 1 ? "are" : "is")} the champion{(winner.Players.Count == 1 ? "" : "s")}."
                            );
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    GiveAwards();

                    CurrentStage = TournamentStage.Inactive;
                    Undefeated.Clear();
                }
                else if (Pyramid.Levels.Count > 0)
                {
                    var activeLevel = Pyramid.Levels[^1];
                    var stillGoing = false;

                    for (var i = 0; i < activeLevel.Matches.Count; ++i)
                    {
                        var match = activeLevel.Matches[i];

                        if (match.Winner == null)
                        {
                            stillGoing = true;

                            if (!match.InProgress)
                            {
                                for (var j = 0; j < Arenas.Count; ++j)
                                {
                                    var arena = Arenas[j];

                                    if (!arena.IsOccupied)
                                    {
                                        match.Start(arena, this);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (!stillGoing)
                    {
                        for (var i = Undefeated.Count - 1; i >= 0; --i)
                        {
                            var part = Undefeated[i];
                            var bad = false;

                            for (var j = 0; j < part.Players.Count; ++j)
                            {
                                var check = part.Players[j];

                                if (check.Deleted || check.Map == null || check.Map == Map.Internal || !check.Alive ||
                                    Sigil.ExistsOn(check) || check.Region.IsPartOf<JailRegion>())
                                {
                                    bad = true;
                                    break;
                                }
                            }

                            if (!bad)
                            {
                                continue;
                            }

                            for (var j = 0; j < part.Players.Count; ++j)
                            {
                                part.Players[j].SendMessage("You have been disqualified from the tournament.");
                            }

                            Undefeated.RemoveAt(i);

                            if (Undefeated.Count == 1)
                            {
                                var winner = Undefeated[0];

                                try
                                {
                                    if (EventController != null)
                                    {
                                        Alert(
                                            "The tournament has completed!",
                                            $"Team {EventController.GetTeamName(Pyramid.Levels[0].Matches[0].Participants.IndexOf(winner))} has won"
                                        );
                                    }
                                    else if (TourneyType == TourneyType.RandomTeam)
                                    {
                                        Alert(
                                            "The tournament has completed!",
                                            $"Team {Pyramid.Levels[0].Matches[0].Participants.IndexOf(winner) + 1} has won!"
                                        );
                                    }
                                    else if (TourneyType == TourneyType.Faction)
                                    {
                                        if (m_ParticipantsPerMatch == 4)
                                        {
                                            var name = Pyramid.Levels[0].Matches[0].Participants.IndexOf(winner) switch
                                            {
                                                0 => "Minax",
                                                1 => "Council of Mages",
                                                2 => "True Britannians",
                                                3 => "Shadowlords",
                                                _ => "(null)"
                                            };

                                            Alert("The tournament has completed!", $"The {name} team has won!");
                                        }
                                        else if (m_ParticipantsPerMatch == 2)
                                        {
                                            Alert(
                                                "The tournament has completed!",
                                                $"The {(Pyramid.Levels[0].Matches[0].Participants.IndexOf(winner) == 0 ? "Evil" : "Hero")} team has won!"
                                            );
                                        }
                                        else
                                        {
                                            Alert(
                                                "The tournament has completed!",
                                                $"Team {Pyramid.Levels[0].Matches[0].Participants.IndexOf(winner) + 1} has won!"
                                            );
                                        }
                                    }
                                    else if (TourneyType == TourneyType.RedVsBlue)
                                    {
                                        Alert(
                                            "The tournament has completed!",
                                            $"Team {(Pyramid.Levels[0].Matches[0].Participants.IndexOf(winner) == 0 ? "Red" : "Blue")} has won!"
                                        );
                                    }
                                    else
                                    {
                                        Alert(
                                            "The tournament has completed!",
                                            $"{winner.NameList} {(winner.Players.Count > 1 ? "are" : "is")} the champion{(winner.Players.Count == 1 ? "" : "s")}."
                                        );
                                    }
                                }
                                catch
                                {
                                    // ignored
                                }

                                GiveAwards();

                                CurrentStage = TournamentStage.Inactive;
                                Undefeated.Clear();
                                break;
                            }
                        }

                        if (Undefeated.Count > 1)
                        {
                            Pyramid.AddLevel(m_ParticipantsPerMatch, Undefeated, GroupType, TourneyType);
                        }
                    }
                }
            }
        }

        public void Alert(params string[] alerts)
        {
            for (var i = 0; i < Arenas.Count; ++i)
            {
                Alert(Arenas[i], alerts);
            }
        }

        public void Alert(Arena arena, params string[] alerts)
        {
            if (arena?.Announcer == null)
            {
                return;
            }

            var count = 0;

            Timer.StartTimer(TimeSpan.FromSeconds(0.5), alerts.Length,
                () =>
                {
                    arena.Announcer.PublicOverheadMessage(MessageType.Regular, 0x35, false, alerts[count++]);
                }
            );
        }
    }
}
