using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.ConPVP
{
    public class AcceptTeamGump : Gump
    {
        private const int BlackColor32 = 0x000008;
        private const int LabelColor32 = 0xFFFFFF;

        private readonly Mobile m_From;
        private readonly List<Mobile> m_Players;
        private readonly Mobile m_Registrar;
        private readonly Mobile m_Requested;
        private readonly Tournament m_Tournament;
        private bool m_Active;

        public AcceptTeamGump(
            Mobile from, Mobile requested, Tournament tourney, Mobile registrar, List<Mobile> players
        ) : base(50, 50)
        {
            m_From = from;
            m_Requested = requested;
            m_Tournament = tourney;
            m_Registrar = registrar;
            m_Players = players;

            m_Active = true;

            var ruleset = tourney.Ruleset;
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

            Closable = false;

            AddPage(0);

            AddBackground(1, 1, 398, height, 3600);

            AddImageTiled(16, 15, 369, height - 29, 3604);
            AddAlphaRegion(16, 15, 369, height - 29);

            AddImage(215, -43, 0xEE40);

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

            sb.Append(" Tournament Invitation");

            AddBorderedText(22, 22, 294, 20, Center(sb.ToString()), LabelColor32, BlackColor32);

            AddBorderedText(
                22,
                50,
                294,
                40,
                $"You have been asked to partner with {from.Name} in a tournament. Do you accept?",
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

            y += 8;
            AddImageTiled(32, y - 1, 264, 1, 9107);
            AddImageTiled(42, y + 1, 264, 1, 9157);
            y += 8;

            AddRadio(24, y, 9727, 9730, true, 1);
            AddBorderedText(60, y + 5, 250, 20, "Yes, I will join them.", LabelColor32, BlackColor32);
            y += 35;

            AddRadio(24, y, 9727, 9730, false, 2);
            AddBorderedText(60, y + 5, 250, 20, "No, I do not wish to fight.", LabelColor32, BlackColor32);
            y += 35;

            AddRadio(24, y, 9727, 9730, false, 3);
            AddBorderedText(60, y + 5, 270, 20, "No, most certainly not. Do not ask again.", LabelColor32, BlackColor32);
            y += 35;

            y -= 3;
            AddButton(314, y, 247, 248, 1);

            Timer.StartTimer(TimeSpan.FromSeconds(15.0), AutoReject);
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

        public void AutoReject()
        {
            if (!m_Active)
            {
                return;
            }

            m_Active = false;

            m_Requested.CloseGump<AcceptTeamGump>();
            m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));

            if (m_Registrar != null)
            {
                m_Registrar.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    $"{m_Requested.Name} seems unresponsive.",
                    m_From.NetState
                );

                m_Registrar.PrivateOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    false,
                    $"You have declined the partnership with {m_From.Name}.",
                    m_Requested.NetState
                );
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = m_From;
            var mob = m_Requested;

            if (info.ButtonID != 1 || !m_Active)
            {
                return;
            }

            m_Active = false;

            if (info.IsSwitched(1))
            {
                if (mob is not PlayerMobile pm)
                {
                    return;
                }

                if (AcceptDuelGump.IsIgnored(mob, from) || mob.Blessed)
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
                else if (pm.DuelContext != null)
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
                    m_Players.Add(mob);

                    m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));

                    if (m_Registrar != null)
                    {
                        m_Registrar.PrivateOverheadMessage(
                            MessageType.Regular,
                            0x59,
                            false,
                            $"{mob.Name} has accepted your offer of partnership.",
                            from.NetState
                        );

                        m_Registrar.PrivateOverheadMessage(
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
                    AcceptDuelGump.BeginIgnore(m_Requested, m_From);
                }

                m_From.SendGump(new ConfirmSignupGump(m_From, m_Registrar, m_Tournament, m_Players));

                if (m_Registrar != null)
                {
                    m_Registrar.PrivateOverheadMessage(
                        MessageType.Regular,
                        0x22,
                        false,
                        $"{mob.Name} has declined your offer of partnership.",
                        from.NetState
                    );

                    m_Registrar.PrivateOverheadMessage(
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
}
