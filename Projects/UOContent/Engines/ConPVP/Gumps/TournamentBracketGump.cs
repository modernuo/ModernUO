using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.ConPVP
{
    public enum TourneyBracketGumpType
    {
        Index,
        Rules_Info,
        Participant_List,
        Participant_Info,
        Round_List,
        Round_Info,
        Match_Info,
        Player_Info
    }

    public class TournamentBracketGump : Gump
    {
        private const int BlackColor32 = 0x000008;
        private readonly Mobile m_From;
        private List<object> m_List;
        private readonly object m_Object;
        private readonly int m_Page;
        private readonly Tournament m_Tournament;
        private readonly TourneyBracketGumpType m_Type;
        private int m_PerPage;

        public TournamentBracketGump(
            Mobile from, Tournament tourney, TourneyBracketGumpType type,
            List<object> list = null, int page = 0, object obj = null
        ) : base(50, 50)
        {
            m_From = from;
            m_Tournament = tourney;
            m_Type = type;
            m_List = list;
            m_Page = page;
            m_Object = obj;
            m_PerPage = 12;

            switch (type)
            {
                case TourneyBracketGumpType.Index:
                    {
                        AddPage(0);
                        AddBackground(0, 0, 300, 300, 9380);

                        var sb = new StringBuilder();

                        if (tourney.TourneyType == TourneyType.FreeForAll)
                        {
                            sb.Append("FFA");
                        }
                        else if (tourney.TourneyType == TourneyType.RandomTeam)
                        {
                            sb.Append(tourney.ParticipantsPerMatch);
                            sb.Append("-Team");
                        }
                        else if (tourney.TourneyType == TourneyType.RedVsBlue)
                        {
                            sb.Append("Red v Blue");
                        }
                        else if (tourney.TourneyType == TourneyType.Faction)
                        {
                            sb.Append(tourney.ParticipantsPerMatch);
                            sb.Append("-Team Faction");
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

                        if (tourney.EventController != null)
                        {
                            sb.Append(' ').Append(tourney.EventController.Title);
                        }

                        sb.Append(" Tournament Bracket");

                        AddHtml(25, 35, 250, 20, Center(sb.ToString()));

                        AddRightArrow(25, 53, ToButtonID(0, 4), "Rules");
                        AddRightArrow(25, 71, ToButtonID(0, 1), "Participants");

                        if (m_Tournament.Stage == TournamentStage.Signup)
                        {
                            var until = m_Tournament.SignupStart + m_Tournament.SignupPeriod - Core.Now;
                            string text;
                            var secs = (int)Math.Round(until.TotalSeconds);

                            if (secs > 0)
                            {
                                var mins = Math.DivRem(secs, 60, out secs);

                                if (mins > 0 && secs > 0)
                                {
                                    text =
                                        $"The tournament will begin in {mins} minute{(mins == 1 ? "" : "s")} and {secs} second{(secs == 1 ? "" : "s")}.";
                                }
                                else if (mins > 0)
                                {
                                    text = $"The tournament will begin in {mins} minute{(mins == 1 ? "" : "s")}.";
                                }
                                else if (secs > 0)
                                {
                                    text = $"The tournament will begin in {secs} second{(secs == 1 ? "" : "s")}.";
                                }
                                else
                                {
                                    text = "The tournament will begin shortly.";
                                }
                            }
                            else
                            {
                                text = "The tournament will begin shortly.";
                            }

                            AddHtml(25, 92, 250, 40, text);
                        }
                        else
                        {
                            AddRightArrow(25, 89, ToButtonID(0, 2), "Rounds");
                        }

                        break;
                    }
                case TourneyBracketGumpType.Rules_Info:
                    {
                        var ruleset = tourney.Ruleset;
                        var basedef = ruleset.Base;

                        BitArray defs;

                        if (ruleset.Flavors.Count > 0)
                        {
                            defs = new BitArray(basedef.Options);

                            for (var i = 0; i < ruleset.Flavors.Count; ++i)
                            {
                                defs.Or(ruleset.Flavors[i].Options);
                            }
                        }
                        else
                        {
                            defs = basedef.Options;
                        }

                        var changes = 0;

                        var opts = ruleset.Options;

                        for (var i = 0; i < opts.Length; ++i)
                        {
                            if (defs[i] != opts[i])
                            {
                                ++changes;
                            }
                        }

                        AddPage(0);
                        AddBackground(
                            0,
                            0,
                            300,
                            60 + 18 + 20 + 20 + 20 + 8 + 20 + ruleset.Flavors.Count * 18 + 4 + 20 + changes * 22 + 6,
                            9380
                        );

                        AddLeftArrow(25, 11, ToButtonID(0, 0));
                        AddHtml(25, 35, 250, 20, Center("Rules"));

                        var y = 53;

                        var groupText = tourney.GroupType switch
                        {
                            GroupingType.HighVsLow => "High vs Low",
                            GroupingType.Nearest   => "Closest opponent",
                            GroupingType.Random    => "Random",
                            _                      => null
                        };

                        AddHtml(35, y, 190, 20, $"Grouping: {groupText}");
                        y += 20;

                        var tieText = tourney.TieType switch
                        {
                            TieType.Random  => "Random",
                            TieType.Highest => "Highest advances",
                            TieType.Lowest  => "Lowest advances",
                            TieType.FullAdvancement => tourney.ParticipantsPerMatch == 2
                                ? "Both advance"
                                : "Everyone advances",
                            TieType.FullElimination => tourney.ParticipantsPerMatch == 2
                                ? "Both eliminated"
                                : "Everyone eliminated",
                            _ => null
                        };

                        AddHtml(35, y, 190, 20, $"Tiebreaker: {tieText}");
                        y += 20;

                        string sdText;

                        if (tourney.SuddenDeath > TimeSpan.Zero)
                        {
                            sdText = tourney.SuddenDeathRounds > 0 ?
                                $"Sudden Death: {(int)tourney.SuddenDeath.TotalMinutes}:{tourney.SuddenDeath.Seconds:D2} (first {tourney.SuddenDeathRounds} rounds)" :
                                $"Sudden Death: {(int)tourney.SuddenDeath.TotalMinutes}:{tourney.SuddenDeath.Seconds:D2} (all rounds)";
                        }
                        else
                        {
                            sdText = "Sudden Death: Off";
                        }

                        AddHtml(35, y, 240, 20, sdText);
                        y += 20;

                        y += 8;

                        AddHtml(35, y, 190, 20, $"Ruleset: {basedef.Title}");
                        y += 20;

                        for (var i = 0; i < ruleset.Flavors.Count; ++i, y += 18)
                        {
                            AddHtml(35, y, 190, 20, $" + {ruleset.Flavors[i].Title}");
                        }

                        y += 4;

                        if (changes > 0)
                        {
                            AddHtml(35, y, 190, 20, "Modifications:");
                            y += 20;

                            for (var i = 0; i < opts.Length; ++i)
                            {
                                if (defs[i] != opts[i])
                                {
                                    var name = ruleset.Layout.FindByIndex(i);

                                    if (name != null) // sanity
                                    {
                                        AddImage(35, y, opts[i] ? 0xD3 : 0xD2);
                                        AddHtml(60, y, 165, 22, name);
                                    }

                                    y += 22;
                                }
                            }
                        }
                        else
                        {
                            AddHtml(35, y, 190, 20, "Modifications: None");
                        }

                        break;
                    }
                case TourneyBracketGumpType.Participant_List:
                    {
                        AddPage(0);
                        AddBackground(0, 0, 300, 300, 9380);

                        var pList = m_List?.SafeConvertList<object, TourneyParticipant>()
                                    ?? new List<TourneyParticipant>(tourney.Participants);

                        AddLeftArrow(25, 11, ToButtonID(0, 0));
                        AddHtml(25, 35, 250, 20, Center($"{pList.Count} Participant{(pList.Count == 1 ? "" : "s")}"));

                        StartPage(out var index, out var count, out var y, 12);

                        for (var i = 0; i < count; ++i, y += 18)
                        {
                            var part = pList[index + i];
                            var name = part.NameList;

                            if (m_Tournament.TourneyType != TourneyType.Standard && part.Players.Count == 1)
                            {
                                if (part.Players[0] is PlayerMobile pm && pm.DuelPlayer != null)
                                {
                                    name = Color(name, pm.DuelPlayer.Eliminated ? 0x6633333 : 0x336666);
                                }
                            }

                            AddRightArrow(25, y, ToButtonID(2, index + i), name);
                        }

                        break;
                    }
                case TourneyBracketGumpType.Participant_Info:
                    {
                        if (obj is not TourneyParticipant part)
                        {
                            break;
                        }

                        AddPage(0);
                        AddBackground(0, 0, 300, 60 + 18 + 20 + part.Players.Count * 18 + 20 + 20 + 160, 9380);

                        AddLeftArrow(25, 11, ToButtonID(0, 1));
                        AddHtml(25, 35, 250, 20, Center("Participants"));

                        var y = 53;

                        AddHtml(25, y, 200, 20, part.Players.Count == 1 ? "Players" : "Team");
                        y += 20;

                        for (var i = 0; i < part.Players.Count; ++i)
                        {
                            var mob = part.Players[i];
                            var name = mob.Name;

                            if (m_Tournament.TourneyType != TourneyType.Standard)
                            {
                                if (mob is PlayerMobile pm && pm.DuelPlayer != null)
                                {
                                    name = Color(name, pm.DuelPlayer.Eliminated ? 0x6633333 : 0x336666);
                                }
                            }

                            AddRightArrow(35, y, ToButtonID(4, i), name);
                            y += 18;
                        }

                        AddHtml(
                            25,
                            y,
                            200,
                            20,
                            $"Free Advances: {(part.FreeAdvances == 0 ? "None" : part.FreeAdvances.ToString())}"
                        );
                        y += 20;

                        AddHtml(25, y, 200, 20, "Log:");
                        y += 20;

                        var sb = new StringBuilder();

                        for (var i = 0; i < part.Log.Count; ++i)
                        {
                            if (sb.Length > 0)
                            {
                                sb.Append("<br>");
                            }

                            sb.Append(part.Log[i]);
                        }

                        if (sb.Length == 0)
                        {
                            sb.Append("Nothing logged yet.");
                        }

                        AddHtml(25, y, 250, 150, Color(sb.ToString(), BlackColor32), false, true);

                        break;
                    }
                case TourneyBracketGumpType.Player_Info:
                    {
                        AddPage(0);
                        AddBackground(0, 0, 300, 300, 9380);

                        AddLeftArrow(25, 11, ToButtonID(0, 3));
                        AddHtml(25, 35, 250, 20, Center("Participants"));

                        if (obj is not Mobile mob)
                        {
                            break;
                        }

                        var ladder = Ladder.Instance;
                        var entry = ladder?.Find(mob);

                        AddHtml(25, 53, 250, 20, $"Name: {mob.Name}");
                        AddHtml(
                            25,
                            73,
                            250,
                            20,
                            $"Guild: {(mob.Guild == null ? "None" : $"{mob.Guild.Name} [{mob.Guild.Abbreviation}]")}"
                        );
                        AddHtml(25, 93, 250, 20, $"Rank: {(entry == null ? "N/A" : LadderGump.Rank(entry.Index + 1))}");
                        AddHtml(25, 113, 250, 20, $"Level: {(entry == null ? 0 : Ladder.GetLevel(entry.Experience))}");
                        AddHtml(25, 133, 250, 20, $"Wins: {entry?.Wins ?? 0:N0}");
                        AddHtml(25, 153, 250, 20, $"Losses: {entry?.Losses ?? 0:N0}");

                        break;
                    }
                case TourneyBracketGumpType.Round_List:
                    {
                        AddPage(0);
                        AddBackground(0, 0, 300, 300, 9380);

                        AddLeftArrow(25, 11, ToButtonID(0, 0));
                        AddHtml(25, 35, 250, 20, Center("Rounds"));

                        StartPage(out var index, out var count, out var y, 12);

                        for (var i = 0; i < count; ++i, y += 18)
                        {
                            AddRightArrow(25, y, ToButtonID(3, index + i), $"Round #{index + i + 1}");
                        }

                        break;
                    }
                case TourneyBracketGumpType.Round_Info:
                    {
                        AddPage(0);
                        AddBackground(0, 0, 300, 300, 9380);

                        AddLeftArrow(25, 11, ToButtonID(0, 2));
                        AddHtml(25, 35, 250, 20, Center("Rounds"));

                        if (m_Object is not PyramidLevel level)
                        {
                            break;
                        }

                        var matchesList = m_List != null
                            ? m_List.SafeConvertList<object, TourneyMatch>()
                            : new List<TourneyMatch>(level.Matches);

                        AddRightArrow(
                            25,
                            53,
                            ToButtonID(5, 0),
                            $"Free Advance: {(level.FreeAdvance == null ? "None" : level.FreeAdvance.NameList)}"
                        );

                        AddHtml(25, 73, 200, 20, $"{matchesList.Count} Match{(matchesList.Count == 1 ? "" : "es")}");

                        StartPage(out var index, out var count, out var y, 10);

                        for (var i = 0; i < count; ++i, y += 18)
                        {
                            var match = matchesList[index + i];

                            var color = -1;

                            if (match.InProgress)
                            {
                                color = 0x336666;
                            }
                            else if (match.Context != null && match.Winner == null)
                            {
                                color = 0x666666;
                            }

                            var sb = new StringBuilder();

                            if (m_Tournament.TourneyType == TourneyType.Standard)
                            {
                                for (var j = 0; j < match.Participants.Count; ++j)
                                {
                                    if (sb.Length > 0)
                                    {
                                        sb.Append(" vs ");
                                    }

                                    var part = match.Participants[j];
                                    var txt = part.NameList;

                                    if (color == -1 && match.Context != null && match.Winner == part)
                                    {
                                        txt = Color(txt, 0x336633);
                                    }
                                    else if (color == -1 && match.Context != null)
                                    {
                                        txt = Color(txt, 0x663333);
                                    }

                                    sb.Append(txt);
                                }
                            }
                            else if (m_Tournament.EventController != null ||
                                     m_Tournament.TourneyType is TourneyType.RandomTeam or TourneyType.RedVsBlue or TourneyType.Faction)
                            {
                                for (var j = 0; j < match.Participants.Count; ++j)
                                {
                                    if (sb.Length > 0)
                                    {
                                        sb.Append(" vs ");
                                    }

                                    var part = match.Participants[j];
                                    string txt;

                                    if (m_Tournament.EventController != null)
                                    {
                                        txt = $"Team {m_Tournament.EventController.GetTeamName(j)} ({part.Players.Count})";
                                    }
                                    else if (m_Tournament.TourneyType == TourneyType.RandomTeam)
                                    {
                                        txt = $"Team {j + 1} ({part.Players.Count})";
                                    }
                                    else if (m_Tournament.TourneyType == TourneyType.Faction)
                                    {
                                        if (m_Tournament.ParticipantsPerMatch == 4)
                                        {
                                            var name = j switch
                                            {
                                                0 => "Minax",
                                                1 => "Council of Mages",
                                                2 => "True Britannians",
                                                3 => "Shadowlords",
                                                _ => "(null)"
                                            };

                                            txt = $"{name} ({part.Players.Count})";
                                        }
                                        else if (m_Tournament.ParticipantsPerMatch == 2)
                                        {
                                            txt = $"{(j == 0 ? "Evil" : "Hero")} Team ({part.Players.Count})";
                                        }
                                        else
                                        {
                                            txt = $"Team {j + 1} ({part.Players.Count})";
                                        }
                                    }
                                    else
                                    {
                                        txt = $"Team {(j == 0 ? "Red" : "Blue")} ({part.Players.Count})";
                                    }

                                    if (color == -1 && match.Context != null && match.Winner == part)
                                    {
                                        txt = Color(txt, 0x336633);
                                    }
                                    else if (color == -1 && match.Context != null)
                                    {
                                        txt = Color(txt, 0x663333);
                                    }

                                    sb.Append(txt);
                                }
                            }
                            else if (m_Tournament.TourneyType == TourneyType.FreeForAll)
                            {
                                sb.Append("Free For All");
                            }

                            var str = sb.ToString();

                            if (color >= 0)
                            {
                                str = Color(str, color);
                            }

                            AddRightArrow(25, y, ToButtonID(5, index + i + 1), str);
                        }

                        break;
                    }
                case TourneyBracketGumpType.Match_Info:
                    {
                        if (obj is not TourneyMatch match)
                        {
                            break;
                        }

                        var ct = m_Tournament.TourneyType == TourneyType.FreeForAll ? 2 : match.Participants.Count;

                        AddPage(0);
                        AddBackground(0, 0, 300, 60 + 18 + 20 + 20 + 20 + ct * 18 + 6, 9380);

                        AddLeftArrow(25, 11, ToButtonID(0, 5));
                        AddHtml(25, 35, 250, 20, Center("Rounds"));

                        AddHtml(25, 53, 250, 20, $"Winner: {(match.Winner == null ? "N/A" : match.Winner.NameList)}");
                        AddHtml(
                            25,
                            73,
                            250,
                            20,
                            $"State: {(match.InProgress ? "In progress" : match.Context != null ? "Complete" : "Waiting")}"
                        );
                        AddHtml(25, 93, 250, 20, "Participants:");

                        if (m_Tournament.TourneyType == TourneyType.Standard)
                        {
                            for (var i = 0; i < match.Participants.Count; ++i)
                            {
                                var part = match.Participants[i];

                                AddRightArrow(25, 113 + i * 18, ToButtonID(6, i), part.NameList);
                            }
                        }
                        else if (m_Tournament.EventController != null ||
                                 m_Tournament.TourneyType is TourneyType.RandomTeam or TourneyType.RedVsBlue or TourneyType.Faction)
                        {
                            for (var i = 0; i < match.Participants.Count; ++i)
                            {
                                var part = match.Participants[i];

                                if (m_Tournament.EventController != null)
                                {
                                    AddRightArrow(
                                        25,
                                        113 + i * 18,
                                        ToButtonID(6, i),
                                        $"Team {m_Tournament.EventController.GetTeamName(i)} ({part.Players.Count})"
                                    );
                                }
                                else if (m_Tournament.TourneyType == TourneyType.RandomTeam)
                                {
                                    AddRightArrow(
                                        25,
                                        113 + i * 18,
                                        ToButtonID(6, i),
                                        $"Team {i + 1} ({part.Players.Count})"
                                    );
                                }
                                else if (m_Tournament.TourneyType == TourneyType.Faction)
                                {
                                    if (m_Tournament.ParticipantsPerMatch == 4)
                                    {
                                        var name = i switch
                                        {
                                            0 => "Minax",
                                            1 => "Council of Mages",
                                            2 => "True Britannians",
                                            3 => "Shadowlords",
                                            _ => "(null)"
                                        };

                                        AddRightArrow(
                                            25,
                                            113 + i * 18,
                                            ToButtonID(6, i),
                                            $"{name} ({part.Players.Count})"
                                        );
                                    }
                                    else if (m_Tournament.ParticipantsPerMatch == 2)
                                    {
                                        AddRightArrow(
                                            25,
                                            113 + i * 18,
                                            ToButtonID(6, i),
                                            $"{(i == 0 ? "Evil" : "Hero")} Team ({part.Players.Count})"
                                        );
                                    }
                                    else
                                    {
                                        AddRightArrow(
                                            25,
                                            113 + i * 18,
                                            ToButtonID(6, i),
                                            $"Team {i + 1} ({part.Players.Count})"
                                        );
                                    }
                                }
                                else
                                {
                                    AddRightArrow(
                                        25,
                                        113 + i * 18,
                                        ToButtonID(6, i),
                                        $"Team {(i == 0 ? "Red" : "Blue")} ({part.Players.Count})"
                                    );
                                }
                            }
                        }
                        else if (m_Tournament.TourneyType == TourneyType.FreeForAll)
                        {
                            AddHtml(25, 113, 250, 20, "Free For All");
                        }

                        break;
                    }
            }
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

        public void AddRightArrow(int x, int y, int bid, string text)
        {
            AddButton(x, y, 0x15E1, 0x15E5, bid);

            if (text != null)
            {
                AddHtml(x + 20, y - 1, 230, 20, text);
            }
        }

        public void AddRightArrow(int x, int y, int bid)
        {
            AddRightArrow(x, y, bid, null);
        }

        public void AddLeftArrow(int x, int y, int bid, string text)
        {
            AddButton(x, y, 0x15E3, 0x15E7, bid);

            if (text != null)
            {
                AddHtml(x + 20, y - 1, 230, 20, text);
            }
        }

        public void AddLeftArrow(int x, int y, int bid)
        {
            AddLeftArrow(x, y, bid, null);
        }

        public int ToButtonID(int type, int index) => 1 + index * 7 + type;

        public bool FromButtonID(int bid, out int type, out int index)
        {
            type = (bid - 1) % 7;
            index = (bid - 1) / 7;
            return bid >= 1;
        }

        public void StartPage(out int index, out int count, out int y, int perPage)
        {
            m_PerPage = perPage;

            index = Math.Max(m_Page * perPage, 0);
            count = Math.Clamp(m_List.Count - index, 0, perPage);

            y = 53 + (12 - perPage) * 18;

            if (m_Page > 0)
            {
                AddLeftArrow(242, 35, ToButtonID(1, 0));
            }

            if ((m_Page + 1) * perPage < m_List.Count)
            {
                AddRightArrow(260, 35, ToButtonID(1, 1));
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!FromButtonID(info.ButtonID, out var type, out var index))
            {
                return;
            }

            switch (type)
            {
                case 0:
                    {
                        switch (index)
                        {
                            case 0:
                                m_From.SendGump(
                                    new TournamentBracketGump(m_From, m_Tournament, TourneyBracketGumpType.Index)
                                );
                                break;
                            case 1:
                                m_From.SendGump(
                                    new TournamentBracketGump(
                                        m_From,
                                        m_Tournament,
                                        TourneyBracketGumpType.Participant_List
                                    )
                                );
                                break;
                            case 2:
                                m_From.SendGump(
                                    new TournamentBracketGump(m_From, m_Tournament, TourneyBracketGumpType.Round_List)
                                );
                                break;
                            case 4:
                                m_From.SendGump(
                                    new TournamentBracketGump(m_From, m_Tournament, TourneyBracketGumpType.Rules_Info)
                                );
                                break;
                            case 3:
                                {
                                    var mob = m_Object as Mobile;

                                    for (var i = 0; i < m_Tournament.Participants.Count; ++i)
                                    {
                                        var part = m_Tournament.Participants[i];

                                        if (part.Players.Contains(mob))
                                        {
                                            m_From.SendGump(
                                                new TournamentBracketGump(
                                                    m_From,
                                                    m_Tournament,
                                                    TourneyBracketGumpType.Participant_Info,
                                                    null,
                                                    0,
                                                    part
                                                )
                                            );
                                            break;
                                        }
                                    }

                                    break;
                                }
                            case 5:
                                {
                                    if (m_Object is not TourneyMatch match)
                                    {
                                        break;
                                    }

                                    for (var i = 0; i < m_Tournament.Pyramid.Levels.Count; ++i)
                                    {
                                        var level = m_Tournament.Pyramid.Levels[i];

                                        if (level.Matches.Contains(match))
                                        {
                                            m_From.SendGump(
                                                new TournamentBracketGump(
                                                    m_From,
                                                    m_Tournament,
                                                    TourneyBracketGumpType.Round_Info,
                                                    null,
                                                    0,
                                                    level
                                                )
                                            );
                                        }
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 1:
                    {
                        switch (index)
                        {
                            case 0:
                                {
                                    if (m_List != null && m_Page > 0)
                                    {
                                        m_From.SendGump(
                                            new TournamentBracketGump(
                                                m_From,
                                                m_Tournament,
                                                m_Type,
                                                m_List,
                                                m_Page - 1,
                                                m_Object
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 1:
                                {
                                    if (m_List != null && (m_Page + 1) * m_PerPage < m_List.Count)
                                    {
                                        m_From.SendGump(
                                            new TournamentBracketGump(
                                                m_From,
                                                m_Tournament,
                                                m_Type,
                                                m_List,
                                                m_Page + 1,
                                                m_Object
                                            )
                                        );
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 2:
                    {
                        if (m_Type != TourneyBracketGumpType.Participant_List)
                        {
                            break;
                        }

                        if (index >= 0 && index < m_List.Count)
                        {
                            m_From.SendGump(
                                new TournamentBracketGump(
                                    m_From,
                                    m_Tournament,
                                    TourneyBracketGumpType.Participant_Info,
                                    null,
                                    0,
                                    m_List[index]
                                )
                            );
                        }

                        break;
                    }
                case 3:
                    {
                        if (m_Type != TourneyBracketGumpType.Round_List)
                        {
                            break;
                        }

                        if (index >= 0 && index < m_List.Count)
                        {
                            m_From.SendGump(
                                new TournamentBracketGump(
                                    m_From,
                                    m_Tournament,
                                    TourneyBracketGumpType.Round_Info,
                                    null,
                                    0,
                                    m_List[index]
                                )
                            );
                        }

                        break;
                    }
                case 4:
                    {
                        if (m_Type != TourneyBracketGumpType.Participant_Info)
                        {
                            break;
                        }

                        if (m_Object is TourneyParticipant part && index >= 0 && index < part.Players.Count)
                        {
                            m_From.SendGump(
                                new TournamentBracketGump(
                                    m_From,
                                    m_Tournament,
                                    TourneyBracketGumpType.Player_Info,
                                    null,
                                    0,
                                    part.Players[index]
                                )
                            );
                        }

                        break;
                    }
                case 5:
                    {
                        if (m_Type != TourneyBracketGumpType.Round_Info)
                        {
                            break;
                        }

                        if (m_Object is not PyramidLevel level)
                        {
                            break;
                        }

                        if (index == 0)
                        {
                            if (level.FreeAdvance != null)
                            {
                                m_From.SendGump(
                                    new TournamentBracketGump(
                                        m_From,
                                        m_Tournament,
                                        TourneyBracketGumpType.Participant_Info,
                                        null,
                                        0,
                                        level.FreeAdvance
                                    )
                                );
                            }
                            else
                            {
                                m_From.SendGump(
                                    new TournamentBracketGump(m_From, m_Tournament, m_Type, m_List, m_Page, m_Object)
                                );
                            }
                        }
                        else if (index >= 1 && index <= level.Matches.Count)
                        {
                            m_From.SendGump(
                                new TournamentBracketGump(
                                    m_From,
                                    m_Tournament,
                                    TourneyBracketGumpType.Match_Info,
                                    null,
                                    0,
                                    level.Matches[index - 1]
                                )
                            );
                        }

                        break;
                    }
                case 6:
                    {
                        if (m_Type != TourneyBracketGumpType.Match_Info)
                        {
                            break;
                        }

                        if (m_Object is TourneyMatch match && index >= 0 && index < match.Participants.Count)
                        {
                            m_From.SendGump(
                                new TournamentBracketGump(
                                    m_From,
                                    m_Tournament,
                                    TourneyBracketGumpType.Participant_Info,
                                    null,
                                    0,
                                    match.Participants[index]
                                )
                            );
                        }

                        break;
                    }
            }
        }
    }
}
