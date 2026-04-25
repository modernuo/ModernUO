using System;
using System.Collections;
using System.Collections.Generic;
using Server.Factions;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using Server.Text;

namespace Server.Engines.ConPVP;

public class ConfirmSignupGump : DynamicGump
{
    private const int BlackColor32 = 0x000008;
    private const int LabelColor32 = 0xFFFFFF;
    private readonly Mobile _from;
    private readonly List<Mobile> _players;
    private readonly Mobile _registrar;
    private readonly Tournament _tournament;

    public override bool Singleton => true;

    private ConfirmSignupGump(Mobile from, Mobile registrar, Tournament tourney, List<Mobile> players) : base(50, 50)
    {
        _from = from;
        _registrar = registrar;
        _tournament = tourney;
        _players = players;
    }

    public static void DisplayTo(Mobile from, Mobile registrar, Tournament tourney, List<Mobile> players)
    {
        if (from?.NetState == null || tourney == null || players == null)
        {
            return;
        }

        var gumps = from.GetGumps();
        gumps.Close<AcceptTeamGump>();
        gumps.Close<AcceptDuelGump>();
        gumps.Close<DuelContextGump>();

        from.SendGump(new ConfirmSignupGump(from, registrar, tourney, players));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.SetNoClose();

        var ruleset = _tournament.Ruleset;
        var basedef = ruleset.Base;

        var height = 185 + 60 + 12;

        var changes = 0;

        BitArray defs;

        if (ruleset.Flavors.Count > 0)
        {
            defs = new BitArray(basedef.Options);

            for (var i = 0; i < ruleset.Flavors.Count; ++i)
            {
                defs.Or(ruleset.Flavors[i].Options);
            }

            height += ruleset.Flavors.Count * 18;
        }
        else
        {
            defs = basedef.Options;
        }

        var opts = ruleset.Options;

        for (var i = 0; i < opts.Length; ++i)
        {
            if (defs[i] != opts[i])
            {
                ++changes;
            }
        }

        height += changes * 22;

        height += 10 + 22 + 25 + 25;

        if (_tournament.PlayersPerParticipant > 1)
        {
            height += 36 + _tournament.PlayersPerParticipant * 20;
        }

        builder.AddPage();

        builder.AddBackground(1, 1, 398, height, 3600);

        builder.AddImageTiled(16, 15, 369, height - 29, 3604);
        builder.AddAlphaRegion(16, 15, 369, height - 29);

        builder.AddImage(215, -43, 0xEE40);

        using var sb = new ValueStringBuilder(stackalloc char[64]);

        if (_tournament.TourneyType == TourneyType.FreeForAll)
        {
            sb.Append("FFA");
        }
        else if (_tournament.TourneyType == TourneyType.RandomTeam)
        {
            sb.Append($"{_tournament.ParticipantsPerMatch}-Team");
        }
        else if (_tournament.TourneyType == TourneyType.Faction)
        {
            sb.Append($"{_tournament.ParticipantsPerMatch}-Team Faction");
        }
        else if (_tournament.TourneyType == TourneyType.RedVsBlue)
        {
            sb.Append("Red v Blue");
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

        sb.Append(" Tournament Signup");

        AddBorderedText(ref builder, 22, 22, 294, 20, sb.AsSpan().Center(), LabelColor32, BlackColor32);
        AddBorderedText(
            ref builder,
            22,
            50,
            294,
            40,
            "You have requested to join the tournament. Do you accept the rules?",
            0xB0C868,
            BlackColor32
        );

        builder.AddImageTiled(32, 88, 264, 1, 9107);
        builder.AddImageTiled(42, 90, 264, 1, 9157);

        var y = 100;

        var groupText = _tournament.GroupType switch
        {
            GroupingType.HighVsLow => "High vs Low",
            GroupingType.Nearest   => "Closest opponent",
            GroupingType.Random    => "Random",
            _                      => null
        };

        AddBorderedText(ref builder, 35, y, 190, 20, $"Grouping: {groupText}", LabelColor32, BlackColor32);
        y += 20;

        var tieText = _tournament.TieType switch
        {
            TieType.Random          => "Random",
            TieType.Highest         => "Highest advances",
            TieType.Lowest          => "Lowest advances",
            TieType.FullAdvancement => _tournament.ParticipantsPerMatch == 2 ? "Both advance" : "Everyone advances",
            TieType.FullElimination => _tournament.ParticipantsPerMatch == 2 ? "Both eliminated" : "Everyone eliminated",
            _                       => null
        };

        AddBorderedText(ref builder, 35, y, 190, 20, $"Tiebreaker: {tieText}", LabelColor32, BlackColor32);
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

        AddBorderedText(ref builder, 35, y, 240, 20, sdText, LabelColor32, BlackColor32);
        y += 20;

        y += 6;
        builder.AddImageTiled(32, y - 1, 264, 1, 9107);
        builder.AddImageTiled(42, y + 1, 264, 1, 9157);
        y += 6;

        AddBorderedText(ref builder, 35, y, 190, 20, $"Ruleset: {basedef.Title}", LabelColor32, BlackColor32);
        y += 20;

        for (var i = 0; i < ruleset.Flavors.Count; ++i, y += 18)
        {
            AddBorderedText(ref builder, 35, y, 190, 20, $" + {ruleset.Flavors[i].Title}", LabelColor32, BlackColor32);
        }

        y += 4;

        if (changes > 0)
        {
            AddBorderedText(ref builder, 35, y, 190, 20, "Modifications:", LabelColor32, BlackColor32);
            y += 20;

            for (var i = 0; i < opts.Length; ++i)
            {
                if (defs[i] != opts[i])
                {
                    var name = ruleset.Layout.FindByIndex(i);

                    if (name != null) // sanity
                    {
                        builder.AddImage(35, y, opts[i] ? 0xD3 : 0xD2);
                        AddBorderedText(ref builder, 60, y, 165, 22, name, LabelColor32, BlackColor32);
                    }

                    y += 22;
                }
            }
        }
        else
        {
            AddBorderedText(ref builder, 35, y, 190, 20, "Modifications: None", LabelColor32, BlackColor32);
            y += 20;
        }

        if (_tournament.PlayersPerParticipant > 1)
        {
            y += 8;
            builder.AddImageTiled(32, y - 1, 264, 1, 9107);
            builder.AddImageTiled(42, y + 1, 264, 1, 9157);
            y += 8;

            AddBorderedText(ref builder, 35, y, 190, 20, "Your Team", LabelColor32, BlackColor32);
            y += 20;

            for (var i = 0; i < _players.Count; ++i, y += 20)
            {
                if (i == 0)
                {
                    builder.AddImage(35, y, 0xD2);
                }
                else
                {
                    AddGoldenButton(ref builder, 35, y, 1 + i);
                }

                AddBorderedText(ref builder, 60, y, 200, 20, _players[i].Name, LabelColor32, BlackColor32);
            }

            for (var i = _players.Count; i < _tournament.PlayersPerParticipant; ++i, y += 20)
            {
                if (i == 0)
                {
                    builder.AddImage(35, y, 0xD2);
                }
                else
                {
                    AddGoldenButton(ref builder, 35, y, 1 + i);
                }

                AddBorderedText(ref builder, 60, y, 200, 20, "(Empty)", LabelColor32, BlackColor32);
            }
        }

        y += 8;
        builder.AddImageTiled(32, y - 1, 264, 1, 9107);
        builder.AddImageTiled(42, y + 1, 264, 1, 9157);
        y += 8;

        builder.AddRadio(24, y, 9727, 9730, true, 1);
        AddBorderedText(ref builder, 60, y + 5, 250, 20, "Yes, I wish to join the tournament.", LabelColor32, BlackColor32);
        y += 35;

        builder.AddRadio(24, y, 9727, 9730, false, 2);
        AddBorderedText(ref builder, 60, y + 5, 250, 20, "No, I do not wish to join.", LabelColor32, BlackColor32);
        y += 35;

        y -= 3;
        builder.AddButton(314, y, 247, 248, 1);
    }

    private static void AddBorderedText(ref DynamicGumpBuilder builder, int x, int y, int width, int height, string text, int color, int borderColor)
    {
        AddColoredText(ref builder, x - 1, y - 1, width, height, text, borderColor);
        AddColoredText(ref builder, x - 1, y + 1, width, height, text, borderColor);
        AddColoredText(ref builder, x + 1, y - 1, width, height, text, borderColor);
        AddColoredText(ref builder, x + 1, y + 1, width, height, text, borderColor);
        AddColoredText(ref builder, x, y, width, height, text, color);
    }

    private static void AddColoredText(ref DynamicGumpBuilder builder, int x, int y, int width, int height, string text, int color)
    {
        builder.AddHtml(x, y, width, height, color == 0 ? text : text.Color(color));
    }

    private static void AddGoldenButton(ref DynamicGumpBuilder builder, int x, int y, int bid)
    {
        builder.AddButton(x, y, 0xD2, 0xD2, bid);
        builder.AddButton(x + 3, y + 3, 0xD8, 0xD8, bid);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1 && info.IsSwitched(1))
        {
            var tourney = _tournament;
            var from = _from;

            switch (tourney.Stage)
            {
                case TournamentStage.Fighting:
                    {
                        if (_registrar != null)
                        {
                            if (_tournament.HasParticipant(from))
                            {
                                _registrar.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x35,
                                    false,
                                    "Excuse me? You are already signed up.",
                                    from.NetState
                                );
                            }
                            else
                            {
                                _registrar.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x22,
                                    false,
                                    "The tournament has already begun. You are too late to signup now.",
                                    from.NetState
                                );
                            }
                        }

                        break;
                    }
                case TournamentStage.Inactive:
                    {
                        _registrar?.PrivateOverheadMessage(
                            MessageType.Regular,
                            0x35,
                            false,
                            "The tournament is closed.",
                            from.NetState
                        );

                        break;
                    }
                case TournamentStage.Signup:
                    {
                        if (_players.Count != tourney.PlayersPerParticipant)
                        {
                            _registrar?.PrivateOverheadMessage(
                                MessageType.Regular,
                                0x35,
                                false,
                                "You have not yet chosen your team.",
                                from.NetState
                            );

                            _from.SendGump(this); // refresh-via-this
                            break;
                        }

                        var ladder = Ladder.Instance;

                        for (var i = 0; i < _players.Count; ++i)
                        {
                            var mob = _players[i];

                            var entry = ladder?.Find(mob);

                            if (entry != null && Ladder.GetLevel(entry.Experience) < tourney.LevelRequirement)
                            {
                                if (_registrar != null)
                                {
                                    if (mob == from)
                                    {
                                        _registrar.PrivateOverheadMessage(
                                            MessageType.Regular,
                                            0x35,
                                            false,
                                            "You have not yet proven yourself a worthy dueler.",
                                            from.NetState
                                        );
                                    }
                                    else
                                    {
                                        _registrar.PrivateOverheadMessage(
                                            MessageType.Regular,
                                            0x35,
                                            false,
                                            $"{mob.Name} has not yet proven themselves a worthy dueler.",
                                            from.NetState
                                        );
                                    }
                                }

                                _from.SendGump(this); // refresh-via-this
                                return;
                            }

                            if (tourney.IsFactionRestricted && Faction.Find(mob) == null)
                            {
                                _registrar?.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x35,
                                    false,
                                    "Only those who have declared their faction allegiance may participate.",
                                    from.NetState
                                );

                                _from.SendGump(this); // refresh-via-this
                                return;
                            }

                            if (tourney.HasParticipant(mob))
                            {
                                if (_registrar != null)
                                {
                                    if (mob == from)
                                    {
                                        _registrar.PrivateOverheadMessage(
                                            MessageType.Regular,
                                            0x35,
                                            false,
                                            "You have already entered this tournament.",
                                            from.NetState
                                        );
                                    }
                                    else
                                    {
                                        _registrar.PrivateOverheadMessage(
                                            MessageType.Regular,
                                            0x35,
                                            false,
                                            $"{mob.Name} has already entered this tournament.",
                                            from.NetState
                                        );
                                    }
                                }

                                _from.SendGump(this); // refresh-via-this
                                return;
                            }

                            if (mob is PlayerMobile mobile && mobile.DuelContext != null)
                            {
                                if (mob == from)
                                {
                                    _registrar?.PrivateOverheadMessage(
                                        MessageType.Regular,
                                        0x35,
                                        false,
                                        "You are already assigned to a duel. You must yield it before joining this tournament.",
                                        from.NetState
                                    );
                                }
                                else
                                {
                                    _registrar?.PrivateOverheadMessage(
                                        MessageType.Regular,
                                        0x35,
                                        false,
                                        $"{mobile.Name} is already assigned to a duel. They must yield it before joining this tournament.",
                                        from.NetState
                                    );
                                }

                                _from.SendGump(this); // refresh-via-this
                                return;
                            }
                        }

                        if (_registrar != null)
                        {
                            string fmt;

                            if (tourney.PlayersPerParticipant == 1)
                            {
                                fmt =
                                    "As you say m'{0}. I've written your name to the bracket. The tournament will begin {1}.";
                            }
                            else if (tourney.PlayersPerParticipant == 2)
                            {
                                fmt =
                                    "As you wish m'{0}. The tournament will begin {1}, but first you must name your partner.";
                            }
                            else
                            {
                                fmt =
                                    "As you wish m'{0}. The tournament will begin {1}, but first you must name your team.";
                            }

                            string timeUntil;
                            var minutesUntil = (int)Math.Round(
                                (tourney.SignupStart + tourney.SignupPeriod - Core.Now)
                                .TotalMinutes
                            );

                            if (minutesUntil == 0)
                            {
                                timeUntil = "momentarily";
                            }
                            else
                            {
                                timeUntil = $"in {minutesUntil} minute{(minutesUntil == 1 ? "" : "s")}";
                            }

                            _registrar.PrivateOverheadMessage(
                                MessageType.Regular,
                                0x35,
                                false,
                                string.Format(fmt, from.Female ? "Lady" : "Lord", timeUntil),
                                from.NetState
                            );
                        }

                        var part = new TourneyParticipant(from);
                        part.Players.Clear();
                        part.Players.AddRange(_players);

                        tourney.Participants.Add(part);

                        break;
                    }
            }
        }
        else if (info.ButtonID > 1)
        {
            var index = info.ButtonID - 1;

            if (index > 0 && index < _players.Count)
            {
                _players.RemoveAt(index);
                _from.SendGump(this); // refresh-via-this
            }
            else if (_players.Count < _tournament.PlayersPerParticipant)
            {
                _from.BeginTarget(12, false, TargetFlags.None, AddPlayer_OnTarget);
                _from.SendGump(this); // refresh-via-this
            }
        }
    }

    private void AddPlayer_OnTarget(Mobile from, object obj)
    {
        if (obj is not Mobile mob || mob == from)
        {
            _from.SendGump(this); // refresh-via-this

            _registrar?.PrivateOverheadMessage(
                MessageType.Regular,
                0x22,
                false,
                "Excuse me?",
                from.NetState
            );
        }
        else if (!mob.Player)
        {
            _from.SendGump(this); // refresh-via-this

            if (mob.Body.IsHuman)
            {
                mob.SayTo(from, 1005443); // Nay, I would rather stay here and watch a nail rust.
            }
            else
            {
                mob.SayTo(from, 1005444); // The creature ignores your offer.
            }
        }
        else if (AcceptDuelGump.IsIgnored(mob, from) || mob.Blessed)
        {
            _from.SendGump(this); // refresh-via-this

            _registrar?.PrivateOverheadMessage(
                MessageType.Regular,
                0x22,
                false,
                "They ignore your invitation.",
                from.NetState
            );
        }
        else
        {
            if (mob is not PlayerMobile pm)
            {
                return;
            }

            if (pm.DuelContext != null)
            {
                _from.SendGump(this); // refresh-via-this

                _registrar?.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    "They are already assigned to another duel.",
                    from.NetState
                );
            }
            else if (mob.HasGump<AcceptTeamGump>())
            {
                _from.SendGump(this); // refresh-via-this

                _registrar?.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    "They have already been offered a partnership.",
                    from.NetState
                );
            }
            else if (mob.HasGump<ConfirmSignupGump>())
            {
                _from.SendGump(this); // refresh-via-this

                _registrar?.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    "They are already trying to join this tournament.",
                    from.NetState
                );
            }
            else if (_players.Contains(mob))
            {
                _from.SendGump(this); // refresh-via-this

                _registrar?.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    "You have already named them as a team member.",
                    from.NetState
                );
            }
            else if (_tournament.HasParticipant(mob))
            {
                _from.SendGump(this); // refresh-via-this

                _registrar?.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    "They have already entered this tournament.",
                    from.NetState
                );
            }
            else if (_players.Count >= _tournament.PlayersPerParticipant)
            {
                _from.SendGump(this); // refresh-via-this

                _registrar?.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    "Your team is full.",
                    from.NetState
                );
            }
            else
            {
                _from.SendGump(this); // refresh-via-this
                AcceptTeamGump.DisplayTo(from, mob, _tournament, _registrar, _players);

                _registrar?.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x59,
                    false,
                    $"As you command m'{(from.Female ? "Lady" : "Lord")}. I've given your offer to {mob.Name}.",
                    from.NetState
                );
            }
        }
    }
}
