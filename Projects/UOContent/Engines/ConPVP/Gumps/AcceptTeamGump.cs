using System;
using System.Collections;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Text;

namespace Server.Engines.ConPVP;

public class AcceptTeamGump : DynamicGump
{
    private const int BlackColor32 = 0x000008;
    private const int LabelColor32 = 0xFFFFFF;

    private readonly Mobile _from;
    private readonly List<Mobile> _players;
    private readonly Mobile _registrar;
    private readonly Mobile _requested;
    private readonly Tournament _tournament;
    private bool _active;

    public override bool Singleton => true;

    private AcceptTeamGump(
        Mobile from, Mobile requested, Tournament tourney, Mobile registrar, List<Mobile> players
    ) : base(50, 50)
    {
        _from = from;
        _requested = requested;
        _tournament = tourney;
        _registrar = registrar;
        _players = players;

        _active = true;

        Timer.StartTimer(TimeSpan.FromSeconds(15.0), AutoReject);
    }

    public static void DisplayTo(
        Mobile from, Mobile requested, Tournament tourney, Mobile registrar, List<Mobile> players
    )
    {
        if (requested?.NetState == null || from == null || tourney == null || players == null)
        {
            return;
        }

        requested.SendGump(new AcceptTeamGump(from, requested, tourney, registrar, players));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.SetNoClose();

        var ruleset = _tournament.Ruleset;
        var basedef = ruleset.Base;

        var height = 185 + 35 + 60 + 12;

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

        sb.Append(" Tournament Invitation");

        AddBorderedText(ref builder, 22, 22, 294, 20, sb.AsSpan().Center(), LabelColor32, BlackColor32);

        AddBorderedText(
            ref builder,
            22,
            50,
            294,
            40,
            $"You have been asked to partner with {_from.Name} in a tournament. Do you accept?",
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

        y += 8;
        builder.AddImageTiled(32, y - 1, 264, 1, 9107);
        builder.AddImageTiled(42, y + 1, 264, 1, 9157);
        y += 8;

        builder.AddRadio(24, y, 9727, 9730, true, 1);
        AddBorderedText(ref builder, 60, y + 5, 250, 20, "Yes, I will join them.", LabelColor32, BlackColor32);
        y += 35;

        builder.AddRadio(24, y, 9727, 9730, false, 2);
        AddBorderedText(ref builder, 60, y + 5, 250, 20, "No, I do not wish to fight.", LabelColor32, BlackColor32);
        y += 35;

        builder.AddRadio(24, y, 9727, 9730, false, 3);
        AddBorderedText(ref builder, 60, y + 5, 270, 20, "No, most certainly not. Do not ask again.", LabelColor32, BlackColor32);
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

    public void AutoReject()
    {
        if (!_active)
        {
            return;
        }

        _active = false;

        _requested.CloseGump<AcceptTeamGump>();
        ConfirmSignupGump.DisplayTo(_from, _registrar, _tournament, _players);

        if (_registrar != null)
        {
            _registrar.PrivateOverheadMessage(
                MessageType.Regular,
                0x22,
                false,
                $"{_requested.Name} seems unresponsive.",
                _from.NetState
            );

            _registrar.PrivateOverheadMessage(
                MessageType.Regular,
                0x22,
                false,
                $"You have declined the partnership with {_from.Name}.",
                _requested.NetState
            );
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = _from;
        var mob = _requested;

        if (info.ButtonID != 1 || !_active)
        {
            return;
        }

        _active = false;

        if (info.IsSwitched(1))
        {
            if (mob is not PlayerMobile pm)
            {
                return;
            }

            if (AcceptDuelGump.IsIgnored(mob, from) || mob.Blessed)
            {
                ConfirmSignupGump.DisplayTo(_from, _registrar, _tournament, _players);

                _registrar?.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    "They ignore your invitation.",
                    from.NetState
                );
            }
            else if (pm.DuelContext != null)
            {
                ConfirmSignupGump.DisplayTo(_from, _registrar, _tournament, _players);

                _registrar?.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    "They are already assigned to another duel.",
                    from.NetState
                );
            }
            else if (_players.Contains(mob))
            {
                ConfirmSignupGump.DisplayTo(_from, _registrar, _tournament, _players);

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
                ConfirmSignupGump.DisplayTo(_from, _registrar, _tournament, _players);

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
                ConfirmSignupGump.DisplayTo(_from, _registrar, _tournament, _players);

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
                _players.Add(mob);

                ConfirmSignupGump.DisplayTo(_from, _registrar, _tournament, _players);

                if (_registrar != null)
                {
                    _registrar.PrivateOverheadMessage(
                        MessageType.Regular,
                        0x59,
                        false,
                        $"{mob.Name} has accepted your offer of partnership.",
                        from.NetState
                    );

                    _registrar.PrivateOverheadMessage(
                        MessageType.Regular,
                        0x59,
                        false,
                        $"You have accepted the partnership with {from.Name}.",
                        mob.NetState
                    );
                }
            }
        }
        else
        {
            if (info.IsSwitched(3))
            {
                AcceptDuelGump.BeginIgnore(_requested, _from);
            }

            ConfirmSignupGump.DisplayTo(_from, _registrar, _tournament, _players);

            if (_registrar != null)
            {
                _registrar.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    $"{mob.Name} has declined your offer of partnership.",
                    from.NetState
                );

                _registrar.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    $"You have declined the partnership with {from.Name}.",
                    mob.NetState
                );
            }
        }
    }
}
