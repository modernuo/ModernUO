using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.ConPVP;

public class AcceptDuelGump : DynamicGump
{
    private const int LabelColor32 = 0xFFFFFF;
    private const int BlackColor32 = 0x000008;

    private static readonly Dictionary<Mobile, List<IgnoreEntry>> _ignoreLists = new();

    private readonly Mobile _challenged;
    private readonly Mobile _challenger;
    private readonly DuelContext _context;
    private readonly Participant _participant;
    private readonly int _slot;

    private bool _active = true;

    public override bool Singleton => true;

    private AcceptDuelGump(Mobile challenger, Mobile challenged, DuelContext context, Participant p, int slot)
        : base(50, 50)
    {
        _challenger = challenger;
        _challenged = challenged;
        _context = context;
        _participant = p;
        _slot = slot;

        Timer.StartTimer(TimeSpan.FromSeconds(15.0), AutoReject);
    }

    public static void DisplayTo(Mobile challenger, Mobile challenged, DuelContext context, Participant p, int slot)
    {
        if (challenged?.NetState == null || challenger == null || context == null || p == null)
        {
            return;
        }

        challenged.SendGump(new AcceptDuelGump(challenger, challenged, context, p, slot));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.SetNoClose();

        builder.AddPage();

        builder.AddBackground(1, 1, 398, 218, 3600);

        builder.AddImageTiled(16, 15, 369, 189, 3604);
        builder.AddAlphaRegion(16, 15, 369, 189);

        builder.AddImage(215, -43, 0xEE40);

        var duelChallengeBlack = "Duel Challenge".Center(BlackColor32);
        builder.AddHtml(22 - 1, 22, 294, 20, duelChallengeBlack);
        builder.AddHtml(22 + 1, 22, 294, 20, duelChallengeBlack);
        builder.AddHtml(22, 22 - 1, 294, 20, duelChallengeBlack);
        builder.AddHtml(22, 22 + 1, 294, 20, duelChallengeBlack);
        builder.AddHtml(22, 22, 294, 20, "Duel Challenge".Center(LabelColor32));

        var accept = _participant.Contains(_challenger)
            ? $"You have been asked to join sides with {_challenger.Name} in a duel. Do you accept?"
            : $"You have been challenged to a duel from {_challenger.Name}. Do you accept?";

        var acceptBlack = accept.Color(BlackColor32);

        builder.AddHtml(22 - 1, 50, 294, 40, acceptBlack);
        builder.AddHtml(22 + 1, 50, 294, 40, acceptBlack);
        builder.AddHtml(22, 50 - 1, 294, 40, acceptBlack);
        builder.AddHtml(22, 50 + 1, 294, 40, acceptBlack);
        builder.AddHtml(22, 50, 294, 40, accept.Color(0xB0C868));

        builder.AddImageTiled(32, 88, 264, 1, 9107);
        builder.AddImageTiled(42, 90, 264, 1, 9157);

        var yesBlack = "Yes, I will fight this duel.".Color(BlackColor32);

        builder.AddRadio(24, 100, 9727, 9730, true, 1);
        builder.AddHtml(60 - 1, 105, 250, 20, yesBlack);
        builder.AddHtml(60 + 1, 105, 250, 20, yesBlack);
        builder.AddHtml(60, 105 - 1, 250, 20, yesBlack);
        builder.AddHtml(60, 105 + 1, 250, 20, yesBlack);
        builder.AddHtml(60, 105, 250, 20, "Yes, I will fight this duel.".Color(LabelColor32));

        var noBlack = "No, I do not wish to fight.".Color(BlackColor32);

        builder.AddRadio(24, 135, 9727, 9730, false, 2);
        builder.AddHtml(60 - 1, 140, 250, 20, noBlack);
        builder.AddHtml(60 + 1, 140, 250, 20, noBlack);
        builder.AddHtml(60, 140 - 1, 250, 20, noBlack);
        builder.AddHtml(60, 140 + 1, 250, 20, noBlack);
        builder.AddHtml(60, 140, 250, 20, "No, I do not wish to fight.".Color(LabelColor32));

        var noAskBlack = "No, knave. Do not ask again.".Color(BlackColor32);

        builder.AddRadio(24, 170, 9727, 9730, false, 3);
        builder.AddHtml(60 - 1, 175, 250, 20, noAskBlack);
        builder.AddHtml(60 + 1, 175, 250, 20, noAskBlack);
        builder.AddHtml(60, 175 - 1, 250, 20, noAskBlack);
        builder.AddHtml(60, 175 + 1, 250, 20, noAskBlack);
        builder.AddHtml(60, 175, 250, 20, "No, knave. Do not ask again.".Color(LabelColor32));

        builder.AddButton(314, 173, 247, 248, 1);
    }

    public void AutoReject()
    {
        if (!_active)
        {
            return;
        }

        _active = false;

        _challenged.CloseGump<AcceptDuelGump>();

        _challenger.SendMessage($"{_challenged.Name} seems unresponsive.");
        _challenged.SendMessage("You decline the challenge.");
    }

    public static void BeginIgnore(Mobile source, Mobile toIgnore)
    {
        if (!_ignoreLists.TryGetValue(source, out var list))
        {
            _ignoreLists[source] = list = new List<IgnoreEntry>();
        }

        for (var i = 0; i < list.Count; ++i)
        {
            var ie = list[i];

            if (ie.Ignored == toIgnore)
            {
                ie.Refresh();
                return;
            }

            if (ie.Expired)
            {
                list.RemoveAt(i--);
            }
        }

        list.Add(new IgnoreEntry(toIgnore));
    }

    public static bool IsIgnored(Mobile source, Mobile check)
    {
        if (!_ignoreLists.TryGetValue(source, out var list))
        {
            return false;
        }

        for (var i = 0; i < list.Count; ++i)
        {
            var ie = list[i];

            if (ie.Expired)
            {
                list.RemoveAt(i--);
            }
            else if (ie.Ignored == check)
            {
                return true;
            }
        }

        return false;
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID != 1 || !_active || !_context.Registered)
        {
            return;
        }

        _active = false;

        if (!_context.Participants.Contains(_participant))
        {
            return;
        }

        if (info.IsSwitched(1))
        {
            if (_challenged is not PlayerMobile pm)
            {
                return;
            }

            if (pm.DuelContext != null)
            {
                if (pm.DuelContext.Initiator == pm)
                {
                    pm.SendMessage(0x22, "You have already started a duel.");
                }
                else
                {
                    pm.SendMessage(0x22, "You have already been challenged in a duel.");
                }

                _challenger.SendMessage($"{pm.Name} cannot fight because they are already assigned to another duel.");
            }
            else if (DuelContext.CheckCombat(pm))
            {
                pm.SendMessage(
                    0x22,
                    "You have recently been in combat with another player and must wait before starting a duel."
                );
                _challenger.SendMessage(
                    $"{pm.Name} cannot fight because they have recently been in combat with another player."
                );
            }
            else if (TournamentController.IsActive)
            {
                pm.SendMessage(0x22, "A tournament is currently active and you may not duel.");
                _challenger.SendMessage(0x22, "A tournament is currently active and you may not duel.");
            }
            else
            {
                var added = false;

                if (_slot >= 0 && _slot < _participant.Players.Length && _participant.Players[_slot] == null)
                {
                    added = true;
                    _participant.Players[_slot] = new DuelPlayer(_challenged, _participant);
                }
                else
                {
                    for (var i = 0; i < _participant.Players.Length; ++i)
                    {
                        if (_participant.Players[i] == null)
                        {
                            added = true;
                            _participant.Players[i] = new DuelPlayer(_challenged, _participant);
                            break;
                        }
                    }
                }

                if (added)
                {
                    _challenger.SendMessage($"{_challenged.Name} has accepted the request.");
                    _challenged.SendMessage($"You have accepted the request from {_challenger.Name}.");

                    foreach (var g in _challenger.GetGumps())
                    {
                        if (g is ParticipantGump pg && pg.Participant == _participant)
                        {
                            ParticipantGump.DisplayTo(_challenger, _context, _participant);
                            break;
                        }

                        if (g is DuelContextGump dcg && dcg.Context == _context)
                        {
                            DuelContextGump.DisplayTo(_challenger, _context);
                            break;
                        }
                    }
                }
                else
                {
                    _challenger.SendMessage(
                        $"The participant list was full and so {_challenged.Name} could not join."
                    );

                    if (_participant.Contains(_challenger))
                    {
                        _challenged.SendMessage($"The participant list was full and so you could not join the fight with {_challenger.Name}.");
                    }
                    else
                    {
                        _challenged.SendMessage($"The participant list was full and so you could not join the fight against {_challenger.Name}.");
                    }
                }
            }
        }
        else
        {
            if (info.IsSwitched(3))
            {
                BeginIgnore(_challenged, _challenger);
            }

            _challenger.SendMessage($"{_challenged.Name} does not wish to fight.");

            if (_participant.Contains(_challenger))
            {
                _challenged.SendMessage($"You chose not to fight with {_challenger.Name}.");
            }
            else
            {
                _challenged.SendMessage($"You chose not to fight against {_challenger.Name}.");
            }
        }
    }

    private class IgnoreEntry
    {
        private static readonly TimeSpan ExpireDelay = TimeSpan.FromMinutes(15.0);
        private DateTime _expire;

        public IgnoreEntry(Mobile ignored)
        {
            Ignored = ignored;
            Refresh();
        }

        public Mobile Ignored { get; }
        public bool Expired => Core.Now >= _expire;

        public void Refresh() => _expire = Core.Now + ExpireDelay;
    }
}
