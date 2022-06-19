using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Server.Factions;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.ConPVP
{
    public class ConfirmSignupGump : Gump
    {
        private const int BlackColor32 = 0x000008;
        private const int LabelColor32 = 0xFFFFFF;
        private readonly Mobile m_From;
        private readonly List<Mobile> m_Players;
        private readonly Mobile m_Registrar;
        private readonly Tournament m_Tournament;

        public ConfirmSignupGump(Mobile from, Mobile registrar, Tournament tourney, List<Mobile> players) : base(50, 50)
        {
            m_From = from;
            m_Registrar = registrar;
            m_Tournament = tourney;
            m_Players = players;

            m_From.CloseGump<AcceptTeamGump>();
            m_From.CloseGump<AcceptDuelGump>();
            m_From.CloseGump<DuelContextGump>();
            m_From.CloseGump<ConfirmSignupGump>();

            var ruleset = tourney.Ruleset;
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

            if (tourney.PlayersPerParticipant > 1)
            {
                height += 36 + tourney.PlayersPerParticipant * 20;
            }

            Closable = false;

            AddPage(0);

            // AddBackground( 0, 0, 400, 220, 9150 );
            AddBackground(1, 1, 398, height, 3600);
            // AddBackground( 16, 15, 369, 189, 9100 );

            AddImageTiled(16, 15, 369, height - 29, 3604);
            AddAlphaRegion(16, 15, 369, height - 29);

            AddImage(215, -43, 0xEE40);
            // AddImage( 330, 141, 0x8BA );

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
            else if (tourney.TourneyType == TourneyType.Faction)
            {
                sb.Append(tourney.ParticipantsPerMatch);
                sb.Append("-Team Faction");
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

            if (tourney.EventController != null)
            {
                sb.Append(' ').Append(tourney.EventController.Title);
            }

            sb.Append(" Tournament Signup");

            AddBorderedText(22, 22, 294, 20, Center(sb.ToString()), LabelColor32, BlackColor32);
            AddBorderedText(
                22,
                50,
                294,
                40,
                "You have requested to join the tournament. Do you accept the rules?",
                0xB0C868,
                BlackColor32
            );

            AddImageTiled(32, 88, 264, 1, 9107);
            AddImageTiled(42, 90, 264, 1, 9157);

            var y = 100;

            var groupText = tourney.GroupType switch
            {
                GroupingType.HighVsLow => "High vs Low",
                GroupingType.Nearest   => "Closest opponent",
                GroupingType.Random    => "Random",
                _                      => null
            };

            AddBorderedText(35, y, 190, 20, $"Grouping: {groupText}", LabelColor32, BlackColor32);
            y += 20;

            var tieText = tourney.TieType switch
            {
                TieType.Random          => "Random",
                TieType.Highest         => "Highest advances",
                TieType.Lowest          => "Lowest advances",
                TieType.FullAdvancement => tourney.ParticipantsPerMatch == 2 ? "Both advance" : "Everyone advances",
                TieType.FullElimination => tourney.ParticipantsPerMatch == 2 ? "Both eliminated" : "Everyone eliminated",
                _                       => null
            };

            AddBorderedText(35, y, 190, 20, $"Tiebreaker: {tieText}", LabelColor32, BlackColor32);
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

            AddBorderedText(35, y, 240, 20, sdText, LabelColor32, BlackColor32);
            y += 20;

            y += 6;
            AddImageTiled(32, y - 1, 264, 1, 9107);
            AddImageTiled(42, y + 1, 264, 1, 9157);
            y += 6;

            AddBorderedText(35, y, 190, 20, $"Ruleset: {basedef.Title}", LabelColor32, BlackColor32);
            y += 20;

            for (var i = 0; i < ruleset.Flavors.Count; ++i, y += 18)
            {
                AddBorderedText(35, y, 190, 20, $" + {ruleset.Flavors[i].Title}", LabelColor32, BlackColor32);
            }

            y += 4;

            if (changes > 0)
            {
                AddBorderedText(35, y, 190, 20, "Modifications:", LabelColor32, BlackColor32);
                y += 20;

                for (var i = 0; i < opts.Length; ++i)
                {
                    if (defs[i] != opts[i])
                    {
                        var name = ruleset.Layout.FindByIndex(i);

                        if (name != null) // sanity
                        {
                            AddImage(35, y, opts[i] ? 0xD3 : 0xD2);
                            AddBorderedText(60, y, 165, 22, name, LabelColor32, BlackColor32);
                        }

                        y += 22;
                    }
                }
            }
            else
            {
                AddBorderedText(35, y, 190, 20, "Modifications: None", LabelColor32, BlackColor32);
                y += 20;
            }

            if (tourney.PlayersPerParticipant > 1)
            {
                y += 8;
                AddImageTiled(32, y - 1, 264, 1, 9107);
                AddImageTiled(42, y + 1, 264, 1, 9157);
                y += 8;

                AddBorderedText(35, y, 190, 20, "Your Team", LabelColor32, BlackColor32);
                y += 20;

                for (var i = 0; i < players.Count; ++i, y += 20)
                {
                    if (i == 0)
                    {
                        AddImage(35, y, 0xD2);
                    }
                    else
                    {
                        AddGoldenButton(35, y, 1 + i);
                    }

                    AddBorderedText(60, y, 200, 20, players[i].Name, LabelColor32, BlackColor32);
                }

                for (var i = players.Count; i < tourney.PlayersPerParticipant; ++i, y += 20)
                {
                    if (i == 0)
                    {
                        AddImage(35, y, 0xD2);
                    }
                    else
                    {
                        AddGoldenButton(35, y, 1 + i);
                    }

                    AddBorderedText(60, y, 200, 20, "(Empty)", LabelColor32, BlackColor32);
                }
            }

            y += 8;
            AddImageTiled(32, y - 1, 264, 1, 9107);
            AddImageTiled(42, y + 1, 264, 1, 9157);
            y += 8;

            AddRadio(24, y, 9727, 9730, true, 1);
            AddBorderedText(60, y + 5, 250, 20, "Yes, I wish to join the tournament.", LabelColor32, BlackColor32);
            y += 35;

            AddRadio(24, y, 9727, 9730, false, 2);
            AddBorderedText(60, y + 5, 250, 20, "No, I do not wish to join.", LabelColor32, BlackColor32);
            y += 35;

            y -= 3;
            AddButton(314, y, 247, 248, 1);
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

        public void AddGoldenButton(int x, int y, int bid)
        {
            AddButton(x, y, 0xD2, 0xD2, bid);
            AddButton(x + 3, y + 3, 0xD8, 0xD8, bid);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1 && info.IsSwitched(1))
            {
                var tourney = m_Tournament;
                var from = m_From;

                switch (tourney.Stage)
                {
                    case TournamentStage.Fighting:
                        {
                            if (m_Registrar != null)
                            {
                                if (m_Tournament.HasParticipant(from))
                                {
                                    m_Registrar.PrivateOverheadMessage(
                                        MessageType.Regular,
                                        0x35,
                                        false,
                                        "Excuse me? You are already signed up.",
                                        from.NetState
                                    );
                                }
                                else
                                {
                                    m_Registrar.PrivateOverheadMessage(
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
                            m_Registrar?.PrivateOverheadMessage(
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
                            if (m_Players.Count != tourney.PlayersPerParticipant)
                            {
                                m_Registrar?.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x35,
                                    false,
                                    "You have not yet chosen your team.",
                                    from.NetState
                                );

                                m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));
                                break;
                            }

                            var ladder = Ladder.Instance;

                            for (var i = 0; i < m_Players.Count; ++i)
                            {
                                var mob = m_Players[i];

                                var entry = ladder?.Find(mob);

                                if (entry != null && Ladder.GetLevel(entry.Experience) < tourney.LevelRequirement)
                                {
                                    if (m_Registrar != null)
                                    {
                                        if (mob == from)
                                        {
                                            m_Registrar.PrivateOverheadMessage(
                                                MessageType.Regular,
                                                0x35,
                                                false,
                                                "You have not yet proven yourself a worthy dueler.",
                                                from.NetState
                                            );
                                        }
                                        else
                                        {
                                            m_Registrar.PrivateOverheadMessage(
                                                MessageType.Regular,
                                                0x35,
                                                false,
                                                $"{mob.Name} has not yet proven themselves a worthy dueler.",
                                                from.NetState
                                            );
                                        }
                                    }

                                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));
                                    return;
                                }

                                if (tourney.IsFactionRestricted && Faction.Find(mob) == null)
                                {
                                    m_Registrar?.PrivateOverheadMessage(
                                        MessageType.Regular,
                                        0x35,
                                        false,
                                        "Only those who have declared their faction allegiance may participate.",
                                        from.NetState
                                    );

                                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));
                                    return;
                                }

                                if (tourney.HasParticipant(mob))
                                {
                                    if (m_Registrar != null)
                                    {
                                        if (mob == from)
                                        {
                                            m_Registrar.PrivateOverheadMessage(
                                                MessageType.Regular,
                                                0x35,
                                                false,
                                                "You have already entered this tournament.",
                                                from.NetState
                                            );
                                        }
                                        else
                                        {
                                            m_Registrar.PrivateOverheadMessage(
                                                MessageType.Regular,
                                                0x35,
                                                false,
                                                $"{mob.Name} has already entered this tournament.",
                                                from.NetState
                                            );
                                        }
                                    }

                                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));
                                    return;
                                }

                                if (mob is PlayerMobile mobile && mobile.DuelContext != null)
                                {
                                    if (mob == from)
                                    {
                                        m_Registrar?.PrivateOverheadMessage(
                                            MessageType.Regular,
                                            0x35,
                                            false,
                                            "You are already assigned to a duel. You must yield it before joining this tournament.",
                                            from.NetState
                                        );
                                    }
                                    else
                                    {
                                        m_Registrar?.PrivateOverheadMessage(
                                            MessageType.Regular,
                                            0x35,
                                            false,
                                            $"{mobile.Name} is already assigned to a duel. They must yield it before joining this tournament.",
                                            from.NetState
                                        );
                                    }

                                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));
                                    return;
                                }
                            }

                            if (m_Registrar != null)
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

                                m_Registrar.PrivateOverheadMessage(
                                    MessageType.Regular,
                                    0x35,
                                    false,
                                    string.Format(fmt, from.Female ? "Lady" : "Lord", timeUntil),
                                    from.NetState
                                );
                            }

                            var part = new TourneyParticipant(from);
                            part.Players.Clear();
                            part.Players.AddRange(m_Players);

                            tourney.Participants.Add(part);

                            break;
                        }
                }
            }
            else if (info.ButtonID > 1)
            {
                var index = info.ButtonID - 1;

                if (index > 0 && index < m_Players.Count)
                {
                    m_Players.RemoveAt(index);
                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));
                }
                else if (m_Players.Count < m_Tournament.PlayersPerParticipant)
                {
                    m_From.BeginTarget(12, false, TargetFlags.None, AddPlayer_OnTarget);
                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));
                }
            }
        }

        private void AddPlayer_OnTarget(Mobile from, object obj)
        {
            if (obj is not Mobile mob || mob == from)
            {
                m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));

                m_Registrar?.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    "Excuse me?",
                    from.NetState
                );
            }
            else if (!mob.Player)
            {
                m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));

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
                m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));

                m_Registrar?.PrivateOverheadMessage(
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
                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));

                    m_Registrar?.PrivateOverheadMessage(
                        MessageType.Regular,
                        0x22,
                        false,
                        "They are already assigned to another duel.",
                        from.NetState
                    );
                }
                else if (mob.HasGump<AcceptTeamGump>())
                {
                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));

                    m_Registrar?.PrivateOverheadMessage(
                        MessageType.Regular,
                        0x22,
                        false,
                        "They have already been offered a partnership.",
                        from.NetState
                    );
                }
                else if (mob.HasGump<ConfirmSignupGump>())
                {
                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));

                    m_Registrar?.PrivateOverheadMessage(
                        MessageType.Regular,
                        0x22,
                        false,
                        "They are already trying to join this tournament.",
                        from.NetState
                    );
                }
                else if (m_Players.Contains(mob))
                {
                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));

                    m_Registrar?.PrivateOverheadMessage(
                        MessageType.Regular,
                        0x22,
                        false,
                        "You have already named them as a team member.",
                        from.NetState
                    );
                }
                else if (m_Tournament.HasParticipant(mob))
                {
                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));

                    m_Registrar?.PrivateOverheadMessage(
                        MessageType.Regular,
                        0x22,
                        false,
                        "They have already entered this tournament.",
                        from.NetState
                    );
                }
                else if (m_Players.Count >= m_Tournament.PlayersPerParticipant)
                {
                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));

                    m_Registrar?.PrivateOverheadMessage(
                        MessageType.Regular,
                        0x22,
                        false,
                        "Your team is full.",
                        from.NetState
                    );
                }
                else
                {
                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));
                    mob.SendGump(new AcceptTeamGump(from, mob, m_Tournament, m_Registrar, m_Players));

                    m_Registrar?.PrivateOverheadMessage(
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
}
