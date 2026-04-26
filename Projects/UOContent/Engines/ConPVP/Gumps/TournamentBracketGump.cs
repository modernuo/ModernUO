using System;
using System.Collections;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Text;

namespace Server.Engines.ConPVP;

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

public class TournamentBracketGump : DynamicGump
{
    private const int BlackColor32 = 0x000008;
    private readonly Mobile _from;
    private List<object> _list;
    private object _object;
    private int _page;
    private readonly Tournament _tournament;
    private TourneyBracketGumpType _type;
    private int _perPage;

    public override bool Singleton => true;

    private TournamentBracketGump(
        Mobile from, Tournament tourney, TourneyBracketGumpType type,
        List<object> list, int page, object obj
    ) : base(50, 50)
    {
        _from = from;
        _tournament = tourney;
        _type = type;
        _list = list;
        _page = page;
        _object = obj;
        _perPage = 12;
    }

    public static void DisplayTo(
        Mobile from, Tournament tourney, TourneyBracketGumpType type,
        List<object> list = null, int page = 0, object obj = null
    )
    {
        if (from?.NetState == null || tourney == null)
        {
            return;
        }

        from.SendGump(new TournamentBracketGump(from, tourney, type, list, page, obj), true);
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        _perPage = 12;

        switch (_type)
        {
            case TourneyBracketGumpType.Index:
                {
                    builder.AddPage();
                    builder.AddBackground(0, 0, 300, 300, 9380);

                    using var sb = ValueStringBuilder.Create(128);

                    if (_tournament.TourneyType == TourneyType.FreeForAll)
                    {
                        sb.Append("FFA");
                    }
                    else if (_tournament.TourneyType == TourneyType.RandomTeam)
                    {
                        sb.Append($"{_tournament.ParticipantsPerMatch}-Team");
                    }
                    else if (_tournament.TourneyType == TourneyType.RedVsBlue)
                    {
                        sb.Append("Red v Blue");
                    }
                    else if (_tournament.TourneyType == TourneyType.Faction)
                    {
                        sb.Append($"{_tournament.ParticipantsPerMatch}-Team Faction");
                    }
                    else
                    {
                        for (var i = 0; i < _tournament.ParticipantsPerMatch; ++i)
                        {
                            if (sb.Length > 0)
                            {
                                sb.Append('v');
                            }

                            sb.Append(_tournament.PlayersPerParticipant);
                        }
                    }

                    if (_tournament.EventController != null)
                    {
                        sb.Append($" {_tournament.EventController.Title}");
                    }

                    sb.Append(" Tournament Bracket");

                    builder.AddHtml(25, 35, 250, 20, sb.AsSpan().Center());

                    AddRightArrow(ref builder, 25, 53, ToButtonID(0, 4), "Rules");
                    AddRightArrow(ref builder, 25, 71, ToButtonID(0, 1), "Participants");

                    if (_tournament.Stage == TournamentStage.Signup)
                    {
                        var until = _tournament.SignupStart + _tournament.SignupPeriod - Core.Now;
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

                        builder.AddHtml(25, 92, 250, 40, text);
                    }
                    else
                    {
                        AddRightArrow(ref builder, 25, 89, ToButtonID(0, 2), "Rounds");
                    }

                    break;
                }
            case TourneyBracketGumpType.Rules_Info:
                {
                    var ruleset = _tournament.Ruleset;
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

                    builder.AddPage();
                    builder.AddBackground(
                        0,
                        0,
                        300,
                        60 + 18 + 20 + 20 + 20 + 8 + 20 + ruleset.Flavors.Count * 18 + 4 + 20 + changes * 22 + 6,
                        9380
                    );

                    AddLeftArrow(ref builder, 25, 11, ToButtonID(0, 0));
                    builder.AddHtml(25, 35, 250, 20, "Rules".Center());

                    var y = 53;

                    var groupText = _tournament.GroupType switch
                    {
                        GroupingType.HighVsLow => "High vs Low",
                        GroupingType.Nearest   => "Closest opponent",
                        GroupingType.Random    => "Random",
                        _                      => null
                    };

                    builder.AddHtml(35, y, 190, 20, $"Grouping: {groupText}");
                    y += 20;

                    var tieText = _tournament.TieType switch
                    {
                        TieType.Random  => "Random",
                        TieType.Highest => "Highest advances",
                        TieType.Lowest  => "Lowest advances",
                        TieType.FullAdvancement => _tournament.ParticipantsPerMatch == 2
                            ? "Both advance"
                            : "Everyone advances",
                        TieType.FullElimination => _tournament.ParticipantsPerMatch == 2
                            ? "Both eliminated"
                            : "Everyone eliminated",
                        _ => null
                    };

                    builder.AddHtml(35, y, 190, 20, $"Tiebreaker: {tieText}");
                    y += 20;

                    string sdText;

                    if (_tournament.SuddenDeath > TimeSpan.Zero)
                    {
                        sdText = _tournament.SuddenDeathRounds > 0 ?
                            $"Sudden Death: {(int)_tournament.SuddenDeath.TotalMinutes}:{_tournament.SuddenDeath.Seconds:D2} (first {_tournament.SuddenDeathRounds} rounds)" :
                            $"Sudden Death: {(int)_tournament.SuddenDeath.TotalMinutes}:{_tournament.SuddenDeath.Seconds:D2} (all rounds)";
                    }
                    else
                    {
                        sdText = "Sudden Death: Off";
                    }

                    builder.AddHtml(35, y, 240, 20, sdText);
                    y += 20;

                    y += 8;

                    builder.AddHtml(35, y, 190, 20, $"Ruleset: {basedef.Title}");
                    y += 20;

                    for (var i = 0; i < ruleset.Flavors.Count; ++i, y += 18)
                    {
                        builder.AddHtml(35, y, 190, 20, $" + {ruleset.Flavors[i].Title}");
                    }

                    y += 4;

                    if (changes > 0)
                    {
                        builder.AddHtml(35, y, 190, 20, "Modifications:");
                        y += 20;

                        for (var i = 0; i < opts.Length; ++i)
                        {
                            if (defs[i] != opts[i])
                            {
                                var name = ruleset.Layout.FindByIndex(i);

                                if (name != null) // sanity
                                {
                                    builder.AddImage(35, y, opts[i] ? 0xD3 : 0xD2);
                                    builder.AddHtml(60, y, 165, 22, name);
                                }

                                y += 22;
                            }
                        }
                    }
                    else
                    {
                        builder.AddHtml(35, y, 190, 20, "Modifications: None");
                    }

                    break;
                }
            case TourneyBracketGumpType.Participant_List:
                {
                    builder.AddPage();
                    builder.AddBackground(0, 0, 300, 300, 9380);

                    var pList = _list?.SafeConvertList<object, TourneyParticipant>()
                                ?? new List<TourneyParticipant>(_tournament.Participants);

                    AddLeftArrow(ref builder, 25, 11, ToButtonID(0, 0));
                    builder.AddHtml(25, 35, 250, 20, Html.Center($"{pList.Count} Participant{(pList.Count == 1 ? "" : "s")}"));

                    StartPage(ref builder, out var index, out var count, out var y, 12);

                    for (var i = 0; i < count; ++i, y += 18)
                    {
                        var part = pList[index + i];
                        var name = part.NameList;

                        if (_tournament.TourneyType != TourneyType.Standard && part.Players.Count == 1)
                        {
                            if (part.Players[0] is PlayerMobile pm && pm.DuelPlayer != null)
                            {
                                name = name.Color(pm.DuelPlayer.Eliminated ? 0x663333 : 0x336666);
                            }
                        }

                        AddRightArrow(ref builder, 25, y, ToButtonID(2, index + i), name);
                    }

                    break;
                }
            case TourneyBracketGumpType.Participant_Info:
                {
                    if (_object is not TourneyParticipant part)
                    {
                        break;
                    }

                    builder.AddPage();
                    builder.AddBackground(0, 0, 300, 60 + 18 + 20 + part.Players.Count * 18 + 20 + 20 + 160, 9380);

                    AddLeftArrow(ref builder, 25, 11, ToButtonID(0, 1));
                    builder.AddHtml(25, 35, 250, 20, "Participants".Center());

                    var y = 53;

                    builder.AddHtml(25, y, 200, 20, part.Players.Count == 1 ? "Players" : "Team");
                    y += 20;

                    for (var i = 0; i < part.Players.Count; ++i)
                    {
                        var mob = part.Players[i];
                        var name = mob.Name;

                        if (_tournament.TourneyType != TourneyType.Standard)
                        {
                            if (mob is PlayerMobile pm && pm.DuelPlayer != null)
                            {
                                name = name.Color(pm.DuelPlayer.Eliminated ? 0x663333 : 0x336666);
                            }
                        }

                        AddRightArrow(ref builder, 35, y, ToButtonID(4, i), name);
                        y += 18;
                    }

                    builder.AddHtml(
                        25,
                        y,
                        200,
                        20,
                        $"Free Advances: {(part.FreeAdvances == 0 ? "None" : part.FreeAdvances.ToString())}"
                    );
                    y += 20;

                    builder.AddHtml(25, y, 200, 20, "Log:");
                    y += 20;

                    using var sb = ValueStringBuilder.Create();

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

                    builder.AddHtml(25, y, 250, 150, sb.AsSpan().Color(BlackColor32), background: false, scrollbar: true);

                    break;
                }
            case TourneyBracketGumpType.Player_Info:
                {
                    builder.AddPage();
                    builder.AddBackground(0, 0, 300, 300, 9380);

                    AddLeftArrow(ref builder, 25, 11, ToButtonID(0, 3));
                    builder.AddHtml(25, 35, 250, 20, "Participants".Center());

                    if (_object is not Mobile mob)
                    {
                        break;
                    }

                    var ladder = Ladder.Instance;
                    var entry = ladder?.Find(mob);

                    builder.AddHtml(25, 53, 250, 20, $"Name: {mob.Name}");
                    builder.AddHtml(
                        25,
                        73,
                        250,
                        20,
                        $"Guild: {(mob.Guild == null ? "None" : $"{mob.Guild.Name} [{mob.Guild.Abbreviation}]")}"
                    );
                    builder.AddHtml(25, 93, 250, 20, $"Rank: {(entry == null ? "N/A" : LadderGump.Rank(entry.Index + 1))}");
                    builder.AddHtml(25, 113, 250, 20, $"Level: {(entry == null ? 0 : Ladder.GetLevel(entry.Experience))}");
                    builder.AddHtml(25, 133, 250, 20, $"Wins: {entry?.Wins ?? 0:N0}");
                    builder.AddHtml(25, 153, 250, 20, $"Losses: {entry?.Losses ?? 0:N0}");

                    break;
                }
            case TourneyBracketGumpType.Round_List:
                {
                    builder.AddPage();
                    builder.AddBackground(0, 0, 300, 300, 9380);

                    AddLeftArrow(ref builder, 25, 11, ToButtonID(0, 0));
                    builder.AddHtml(25, 35, 250, 20, "Rounds".Center());

                    StartPage(ref builder, out var index, out var count, out var y, 12);

                    for (var i = 0; i < count; ++i, y += 18)
                    {
                        AddRightArrow(ref builder, 25, y, ToButtonID(3, index + i), $"Round #{index + i + 1}");
                    }

                    break;
                }
            case TourneyBracketGumpType.Round_Info:
                {
                    builder.AddPage();
                    builder.AddBackground(0, 0, 300, 300, 9380);

                    AddLeftArrow(ref builder, 25, 11, ToButtonID(0, 2));
                    builder.AddHtml(25, 35, 250, 20, "Rounds".Center());

                    if (_object is not PyramidLevel level)
                    {
                        break;
                    }

                    var matchesList = _list != null
                        ? _list.SafeConvertList<object, TourneyMatch>()
                        : new List<TourneyMatch>(level.Matches);

                    AddRightArrow(
                        ref builder,
                        25,
                        53,
                        ToButtonID(5, 0),
                        $"Free Advance: {(level.FreeAdvance == null ? "None" : level.FreeAdvance.NameList)}"
                    );

                    builder.AddHtml(25, 73, 200, 20, $"{matchesList.Count} Match{(matchesList.Count == 1 ? "" : "es")}");

                    StartPage(ref builder, out var index, out var count, out var y, 10);

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

                        using var sbMatch = ValueStringBuilder.Create(512);

                        if (_tournament.TourneyType == TourneyType.Standard)
                        {
                            for (var j = 0; j < match.Participants.Count; ++j)
                            {
                                if (sbMatch.Length > 0)
                                {
                                    sbMatch.Append(" vs ");
                                }

                                var part = match.Participants[j];
                                var txt = part.NameList;

                                if (color == -1 && match.Context != null && match.Winner == part)
                                {
                                    txt = txt.Color(0x336633);
                                }
                                else if (color == -1 && match.Context != null)
                                {
                                    txt = txt.Color(0x663333);
                                }

                                sbMatch.Append(txt);
                            }
                        }
                        else if (_tournament.EventController != null ||
                                 _tournament.TourneyType is TourneyType.RandomTeam or TourneyType.RedVsBlue or TourneyType.Faction)
                        {
                            for (var j = 0; j < match.Participants.Count; ++j)
                            {
                                if (sbMatch.Length > 0)
                                {
                                    sbMatch.Append(" vs ");
                                }

                                var part = match.Participants[j];
                                string txt;

                                if (_tournament.EventController != null)
                                {
                                    txt = $"Team {_tournament.EventController.GetTeamName(j)} ({part.Players.Count})";
                                }
                                else if (_tournament.TourneyType == TourneyType.RandomTeam)
                                {
                                    txt = $"Team {j + 1} ({part.Players.Count})";
                                }
                                else if (_tournament.TourneyType == TourneyType.Faction)
                                {
                                    if (_tournament.ParticipantsPerMatch == 4)
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
                                    else if (_tournament.ParticipantsPerMatch == 2)
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
                                    txt = txt.Color(0x336633);
                                }
                                else if (color == -1 && match.Context != null)
                                {
                                    txt = txt.Color(0x663333);
                                }

                                sbMatch.Append(txt);
                            }
                        }
                        else if (_tournament.TourneyType == TourneyType.FreeForAll)
                        {
                            sbMatch.Append("Free For All");
                        }

                        var str = sbMatch.ToString();

                        if (color >= 0)
                        {
                            str = str.Color(color);
                        }

                        AddRightArrow(ref builder, 25, y, ToButtonID(5, index + i + 1), str);
                    }

                    break;
                }
            case TourneyBracketGumpType.Match_Info:
                {
                    if (_object is not TourneyMatch match)
                    {
                        break;
                    }

                    var ct = _tournament.TourneyType == TourneyType.FreeForAll ? 2 : match.Participants.Count;

                    builder.AddPage();
                    builder.AddBackground(0, 0, 300, 60 + 18 + 20 + 20 + 20 + ct * 18 + 6, 9380);

                    AddLeftArrow(ref builder, 25, 11, ToButtonID(0, 5));
                    builder.AddHtml(25, 35, 250, 20, "Rounds".Center());

                    builder.AddHtml(25, 53, 250, 20, $"Winner: {(match.Winner == null ? "N/A" : match.Winner.NameList)}");
                    builder.AddHtml(
                        25,
                        73,
                        250,
                        20,
                        $"State: {(match.InProgress ? "In progress" : match.Context != null ? "Complete" : "Waiting")}"
                    );
                    builder.AddHtml(25, 93, 250, 20, "Participants:");

                    if (_tournament.TourneyType == TourneyType.Standard)
                    {
                        for (var i = 0; i < match.Participants.Count; ++i)
                        {
                            var part = match.Participants[i];

                            AddRightArrow(ref builder, 25, 113 + i * 18, ToButtonID(6, i), part.NameList);
                        }
                    }
                    else if (_tournament.EventController != null ||
                             _tournament.TourneyType is TourneyType.RandomTeam or TourneyType.RedVsBlue or TourneyType.Faction)
                    {
                        for (var i = 0; i < match.Participants.Count; ++i)
                        {
                            var part = match.Participants[i];

                            if (_tournament.EventController != null)
                            {
                                AddRightArrow(
                                    ref builder,
                                    25,
                                    113 + i * 18,
                                    ToButtonID(6, i),
                                    $"Team {_tournament.EventController.GetTeamName(i)} ({part.Players.Count})"
                                );
                            }
                            else if (_tournament.TourneyType == TourneyType.RandomTeam)
                            {
                                AddRightArrow(
                                    ref builder,
                                    25,
                                    113 + i * 18,
                                    ToButtonID(6, i),
                                    $"Team {i + 1} ({part.Players.Count})"
                                );
                            }
                            else if (_tournament.TourneyType == TourneyType.Faction)
                            {
                                if (_tournament.ParticipantsPerMatch == 4)
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
                                        ref builder,
                                        25,
                                        113 + i * 18,
                                        ToButtonID(6, i),
                                        $"{name} ({part.Players.Count})"
                                    );
                                }
                                else if (_tournament.ParticipantsPerMatch == 2)
                                {
                                    AddRightArrow(
                                        ref builder,
                                        25,
                                        113 + i * 18,
                                        ToButtonID(6, i),
                                        $"{(i == 0 ? "Evil" : "Hero")} Team ({part.Players.Count})"
                                    );
                                }
                                else
                                {
                                    AddRightArrow(
                                        ref builder,
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
                                    ref builder,
                                    25,
                                    113 + i * 18,
                                    ToButtonID(6, i),
                                    $"Team {(i == 0 ? "Red" : "Blue")} ({part.Players.Count})"
                                );
                            }
                        }
                    }
                    else if (_tournament.TourneyType == TourneyType.FreeForAll)
                    {
                        builder.AddHtml(25, 113, 250, 20, "Free For All");
                    }

                    break;
                }
        }
    }

    private static void AddRightArrow(ref DynamicGumpBuilder builder, int x, int y, int bid, string text)
    {
        builder.AddButton(x, y, 0x15E1, 0x15E5, bid);

        if (text != null)
        {
            builder.AddHtml(x + 20, y - 1, 230, 20, text);
        }
    }

    private static void AddRightArrow(ref DynamicGumpBuilder builder, int x, int y, int bid)
    {
        AddRightArrow(ref builder, x, y, bid, null);
    }

    private static void AddLeftArrow(ref DynamicGumpBuilder builder, int x, int y, int bid, string text)
    {
        builder.AddButton(x, y, 0x15E3, 0x15E7, bid);

        if (text != null)
        {
            builder.AddHtml(x + 20, y - 1, 230, 20, text);
        }
    }

    private static void AddLeftArrow(ref DynamicGumpBuilder builder, int x, int y, int bid)
    {
        AddLeftArrow(ref builder, x, y, bid, null);
    }

    private static int ToButtonID(int type, int index) => 1 + index * 7 + type;

    private static bool FromButtonID(int bid, out int type, out int index)
    {
        type = (bid - 1) % 7;
        index = (bid - 1) / 7;
        return bid >= 1;
    }

    private void StartPage(ref DynamicGumpBuilder builder, out int index, out int count, out int y, int perPage)
    {
        _perPage = perPage;

        index = Math.Max(_page * perPage, 0);
        count = Math.Clamp(_list.Count - index, 0, perPage);

        y = 53 + (12 - perPage) * 18;

        if (_page > 0)
        {
            AddLeftArrow(ref builder, 242, 35, ToButtonID(1, 0));
        }

        if ((_page + 1) * perPage < _list.Count)
        {
            AddRightArrow(ref builder, 260, 35, ToButtonID(1, 1));
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
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
                            {
                                _type = TourneyBracketGumpType.Index;
                                _list = null;
                                _page = 0;
                                _object = null;
                                _from.SendGump(this); // refresh-via-this
                                break;
                            }
                        case 1:
                            {
                                _type = TourneyBracketGumpType.Participant_List;
                                _list = null;
                                _page = 0;
                                _object = null;
                                _from.SendGump(this); // refresh-via-this
                                break;
                            }
                        case 2:
                            {
                                _type = TourneyBracketGumpType.Round_List;
                                _list = null;
                                _page = 0;
                                _object = null;
                                _from.SendGump(this); // refresh-via-this
                                break;
                            }
                        case 4:
                            {
                                _type = TourneyBracketGumpType.Rules_Info;
                                _list = null;
                                _page = 0;
                                _object = null;
                                _from.SendGump(this); // refresh-via-this
                                break;
                            }
                        case 3:
                            {
                                var mob = _object as Mobile;

                                for (var i = 0; i < _tournament.Participants.Count; ++i)
                                {
                                    var part = _tournament.Participants[i];

                                    if (part.Players.Contains(mob))
                                    {
                                        _type = TourneyBracketGumpType.Participant_Info;
                                        _list = null;
                                        _page = 0;
                                        _object = part;
                                        _from.SendGump(this); // refresh-via-this
                                        break;
                                    }
                                }

                                break;
                            }
                        case 5:
                            {
                                if (_object is not TourneyMatch match)
                                {
                                    break;
                                }

                                for (var i = 0; i < _tournament.Pyramid.Levels.Count; ++i)
                                {
                                    var level = _tournament.Pyramid.Levels[i];

                                    if (level.Matches.Contains(match))
                                    {
                                        _type = TourneyBracketGumpType.Round_Info;
                                        _list = null;
                                        _page = 0;
                                        _object = level;
                                        _from.SendGump(this); // refresh-via-this
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
                                if (_list != null && _page > 0)
                                {
                                    _page--;
                                    _from.SendGump(this); // refresh-via-this
                                }

                                break;
                            }
                        case 1:
                            {
                                if (_list != null && (_page + 1) * _perPage < _list.Count)
                                {
                                    _page++;
                                    _from.SendGump(this); // refresh-via-this
                                }

                                break;
                            }
                    }

                    break;
                }
            case 2:
                {
                    if (_type != TourneyBracketGumpType.Participant_List)
                    {
                        break;
                    }

                    if (index >= 0 && index < _list.Count)
                    {
                        _type = TourneyBracketGumpType.Participant_Info;
                        _object = _list[index];
                        _list = null;
                        _page = 0;
                        _from.SendGump(this); // refresh-via-this
                    }

                    break;
                }
            case 3:
                {
                    if (_type != TourneyBracketGumpType.Round_List)
                    {
                        break;
                    }

                    if (index >= 0 && index < _list.Count)
                    {
                        _type = TourneyBracketGumpType.Round_Info;
                        _object = _list[index];
                        _list = null;
                        _page = 0;
                        _from.SendGump(this); // refresh-via-this
                    }

                    break;
                }
            case 4:
                {
                    if (_type != TourneyBracketGumpType.Participant_Info)
                    {
                        break;
                    }

                    if (_object is TourneyParticipant part && index >= 0 && index < part.Players.Count)
                    {
                        _type = TourneyBracketGumpType.Player_Info;
                        _object = part.Players[index];
                        _list = null;
                        _page = 0;
                        _from.SendGump(this); // refresh-via-this
                    }

                    break;
                }
            case 5:
                {
                    if (_type != TourneyBracketGumpType.Round_Info)
                    {
                        break;
                    }

                    if (_object is not PyramidLevel level)
                    {
                        break;
                    }

                    if (index == 0)
                    {
                        if (level.FreeAdvance != null)
                        {
                            _type = TourneyBracketGumpType.Participant_Info;
                            _object = level.FreeAdvance;
                            _list = null;
                            _page = 0;
                            _from.SendGump(this); // refresh-via-this
                        }
                        else
                        {
                            _from.SendGump(this); // refresh-via-this
                        }
                    }
                    else if (index >= 1 && index <= level.Matches.Count)
                    {
                        _type = TourneyBracketGumpType.Match_Info;
                        _object = level.Matches[index - 1];
                        _list = null;
                        _page = 0;
                        _from.SendGump(this); // refresh-via-this
                    }

                    break;
                }
            case 6:
                {
                    if (_type != TourneyBracketGumpType.Match_Info)
                    {
                        break;
                    }

                    if (_object is TourneyMatch match && index >= 0 && index < match.Participants.Count)
                    {
                        _type = TourneyBracketGumpType.Participant_Info;
                        _object = match.Participants[index];
                        _list = null;
                        _page = 0;
                        _from.SendGump(this); // refresh-via-this
                    }

                    break;
                }
        }
    }
}
