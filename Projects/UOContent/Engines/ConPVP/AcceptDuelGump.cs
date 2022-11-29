using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.ConPVP
{
    public class AcceptDuelGump : Gump
    {
        private const int LabelColor32 = 0xFFFFFF;
        private const int BlackColor32 = 0x000008;

        private static readonly Dictionary<Mobile, List<IgnoreEntry>> m_IgnoreLists =
            new();

        private readonly Mobile m_Challenged;
        private readonly Mobile m_Challenger;
        private readonly DuelContext m_Context;
        private readonly Participant m_Participant;
        private readonly int m_Slot;

        private bool m_Active = true;

        public AcceptDuelGump(Mobile challenger, Mobile challenged, DuelContext context, Participant p, int slot) : base(
            50,
            50
        )
        {
            m_Challenger = challenger;
            m_Challenged = challenged;
            m_Context = context;
            m_Participant = p;
            m_Slot = slot;

            challenged.CloseGump<AcceptDuelGump>();

            Closable = false;

            AddPage(0);

            // AddBackground( 0, 0, 400, 220, 9150 );
            AddBackground(1, 1, 398, 218, 3600);
            // AddBackground( 16, 15, 369, 189, 9100 );

            AddImageTiled(16, 15, 369, 189, 3604);
            AddAlphaRegion(16, 15, 369, 189);

            AddImage(215, -43, 0xEE40);
            // AddImage( 330, 141, 0x8BA );

            AddHtml(22 - 1, 22, 294, 20, Color(Center("Duel Challenge"), BlackColor32));
            AddHtml(22 + 1, 22, 294, 20, Color(Center("Duel Challenge"), BlackColor32));
            AddHtml(22, 22 - 1, 294, 20, Color(Center("Duel Challenge"), BlackColor32));
            AddHtml(22, 22 + 1, 294, 20, Color(Center("Duel Challenge"), BlackColor32));
            AddHtml(22, 22, 294, 20, Color(Center("Duel Challenge"), LabelColor32));

            string fmt;

            if (p.Contains(challenger))
            {
                fmt = "You have been asked to join sides with {0} in a duel. Do you accept?";
            }
            else
            {
                fmt = "You have been challenged to a duel from {0}. Do you accept?";
            }

            AddHtml(22 - 1, 50, 294, 40, Color(string.Format(fmt, challenger.Name), BlackColor32));
            AddHtml(22 + 1, 50, 294, 40, Color(string.Format(fmt, challenger.Name), BlackColor32));
            AddHtml(22, 50 - 1, 294, 40, Color(string.Format(fmt, challenger.Name), BlackColor32));
            AddHtml(22, 50 + 1, 294, 40, Color(string.Format(fmt, challenger.Name), BlackColor32));
            AddHtml(22, 50, 294, 40, Color(string.Format(fmt, challenger.Name), 0xB0C868));

            AddImageTiled(32, 88, 264, 1, 9107);
            AddImageTiled(42, 90, 264, 1, 9157);

            AddRadio(24, 100, 9727, 9730, true, 1);
            AddHtml(60 - 1, 105, 250, 20, Color("Yes, I will fight this duel.", BlackColor32));
            AddHtml(60 + 1, 105, 250, 20, Color("Yes, I will fight this duel.", BlackColor32));
            AddHtml(60, 105 - 1, 250, 20, Color("Yes, I will fight this duel.", BlackColor32));
            AddHtml(60, 105 + 1, 250, 20, Color("Yes, I will fight this duel.", BlackColor32));
            AddHtml(60, 105, 250, 20, Color("Yes, I will fight this duel.", LabelColor32));

            AddRadio(24, 135, 9727, 9730, false, 2);
            AddHtml(60 - 1, 140, 250, 20, Color("No, I do not wish to fight.", BlackColor32));
            AddHtml(60 + 1, 140, 250, 20, Color("No, I do not wish to fight.", BlackColor32));
            AddHtml(60, 140 - 1, 250, 20, Color("No, I do not wish to fight.", BlackColor32));
            AddHtml(60, 140 + 1, 250, 20, Color("No, I do not wish to fight.", BlackColor32));
            AddHtml(60, 140, 250, 20, Color("No, I do not wish to fight.", LabelColor32));

            AddRadio(24, 170, 9727, 9730, false, 3);
            AddHtml(60 - 1, 175, 250, 20, Color("No, knave. Do not ask again.", BlackColor32));
            AddHtml(60 + 1, 175, 250, 20, Color("No, knave. Do not ask again.", BlackColor32));
            AddHtml(60, 175 - 1, 250, 20, Color("No, knave. Do not ask again.", BlackColor32));
            AddHtml(60, 175 + 1, 250, 20, Color("No, knave. Do not ask again.", BlackColor32));
            AddHtml(60, 175, 250, 20, Color("No, knave. Do not ask again.", LabelColor32));

            AddButton(314, 173, 247, 248, 1);

            Timer.StartTimer(TimeSpan.FromSeconds(15.0), AutoReject);
        }

        public string Center(string text) => $"<CENTER>{text}</CENTER>";

        public string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

        public void AutoReject()
        {
            if (!m_Active)
            {
                return;
            }

            m_Active = false;

            m_Challenged.CloseGump<AcceptDuelGump>();

            m_Challenger.SendMessage($"{m_Challenged.Name} seems unresponsive.");
            m_Challenged.SendMessage("You decline the challenge.");
        }

        public static void BeginIgnore(Mobile source, Mobile toIgnore)
        {
            if (!m_IgnoreLists.TryGetValue(source, out var list))
            {
                m_IgnoreLists[source] = list = new List<IgnoreEntry>();
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
            if (!m_IgnoreLists.TryGetValue(source, out var list))
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

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID != 1 || !m_Active || !m_Context.Registered)
            {
                return;
            }

            m_Active = false;

            if (!m_Context.Participants.Contains(m_Participant))
            {
                return;
            }

            if (info.IsSwitched(1))
            {
                if (m_Challenged is not PlayerMobile pm)
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

                    m_Challenger.SendMessage($"{pm.Name} cannot fight because they are already assigned to another duel.");
                }
                else if (DuelContext.CheckCombat(pm))
                {
                    pm.SendMessage(
                        0x22,
                        "You have recently been in combat with another player and must wait before starting a duel."
                    );
                    m_Challenger.SendMessage(
                        $"{pm.Name} cannot fight because they have recently been in combat with another player."
                    );
                }
                else if (TournamentController.IsActive)
                {
                    pm.SendMessage(0x22, "A tournament is currently active and you may not duel.");
                    m_Challenger.SendMessage(0x22, "A tournament is currently active and you may not duel.");
                }
                else
                {
                    var added = false;

                    if (m_Slot >= 0 && m_Slot < m_Participant.Players.Length && m_Participant.Players[m_Slot] == null)
                    {
                        added = true;
                        m_Participant.Players[m_Slot] = new DuelPlayer(m_Challenged, m_Participant);
                    }
                    else
                    {
                        for (var i = 0; i < m_Participant.Players.Length; ++i)
                        {
                            if (m_Participant.Players[i] == null)
                            {
                                added = true;
                                m_Participant.Players[i] = new DuelPlayer(m_Challenged, m_Participant);
                                break;
                            }
                        }
                    }

                    if (added)
                    {
                        m_Challenger.SendMessage($"{m_Challenged.Name} has accepted the request.");
                        m_Challenged.SendMessage($"You have accepted the request from {m_Challenger.Name}.");

                        var ns = m_Challenger.NetState;

                        if (ns != null)
                        {
                            foreach (var g in ns.Gumps)
                            {
                                if (g is ParticipantGump pg && pg.Participant == m_Participant)
                                {
                                    m_Challenger.SendGump(new ParticipantGump(m_Challenger, m_Context, m_Participant));
                                    break;
                                }

                                if (g is DuelContextGump dcg && dcg.Context == m_Context)
                                {
                                    m_Challenger.SendGump(new DuelContextGump(m_Challenger, m_Context));
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        m_Challenger.SendMessage(
                            $"The participant list was full and so {m_Challenged.Name} could not join."
                        );

                        if (m_Participant.Contains(m_Challenger))
                        {
                            m_Challenged.SendMessage($"The participant list was full and so you could not join the fight with {m_Challenger.Name}.");
                        }
                        else
                        {
                            m_Challenged.SendMessage($"The participant list was full and so you could not join the fight against {m_Challenger.Name}.");
                        }
                    }
                }
            }
            else
            {
                if (info.IsSwitched(3))
                {
                    BeginIgnore(m_Challenged, m_Challenger);
                }

                m_Challenger.SendMessage($"{m_Challenged.Name} does not wish to fight.");

                if (m_Participant.Contains(m_Challenger))
                {
                    m_Challenged.SendMessage($"You chose not to fight with {m_Challenger.Name}.");
                }
                else
                {
                    m_Challenged.SendMessage($"You chose not to fight against {m_Challenger.Name}.");
                }
            }
        }

        private class IgnoreEntry
        {
            private static readonly TimeSpan ExpireDelay = TimeSpan.FromMinutes(15.0);
            public readonly Mobile m_Ignored;
            public DateTime m_Expire;

            public IgnoreEntry(Mobile ignored)
            {
                m_Ignored = ignored;
                Refresh();
            }

            public Mobile Ignored => m_Ignored;
            public bool Expired => Core.Now >= m_Expire;

            public void Refresh() => m_Expire = Core.Now + ExpireDelay;
        }
    }
}
