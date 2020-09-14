using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP
{
    public class DuelContextGump : Gump
    {
        public DuelContextGump(Mobile from, DuelContext context) : base(50, 50)
        {
            From = from;
            Context = context;

            from.CloseGump<RulesetGump>();
            from.CloseGump<DuelContextGump>();
            from.CloseGump<ParticipantGump>();

            var count = context.Participants.Count;

            if (count < 3)
            {
                count = 3;
            }

            var height = 35 + 10 + 22 + 30 + 22 + 22 + 2 + count * 22 + 2 + 30;

            AddPage(0);

            AddBackground(0, 0, 300, height, 9250);
            AddBackground(10, 10, 280, height - 20, 0xDAC);

            AddHtml(35, 25, 230, 20, Center("Duel Setup"));

            var x = 35;
            var y = 47;

            AddGoldenButtonLabeled(x, y, 1, "Rules");
            y += 22;
            AddGoldenButtonLabeled(x, y, 2, "Start");
            y += 22;
            AddGoldenButtonLabeled(x, y, 3, "Add Participant");
            y += 30;

            AddHtml(35, y, 230, 20, Center("Participants"));
            y += 22;

            for (var i = 0; i < context.Participants.Count; ++i)
            {
                var p = context.Participants[i];

                AddGoldenButtonLabeled(
                    x,
                    y,
                    4 + i,
                    string.Format(
                        p.Count == 1 ? "Player {0}: {3}" : "Team {0}: {1}/{2}: {3}",
                        1 + i,
                        p.FilledSlots,
                        p.Count,
                        p.NameList
                    )
                );
                y += 22;
            }
        }

        public Mobile From { get; }

        public DuelContext Context { get; }

        public string Center(string text) => $"<CENTER>{text}</CENTER>";

        public void AddGoldenButton(int x, int y, int bid)
        {
            AddButton(x, y, 0xD2, 0xD2, bid);
            AddButton(x + 3, y + 3, 0xD8, 0xD8, bid);
        }

        public void AddGoldenButtonLabeled(int x, int y, int bid, string text)
        {
            AddGoldenButton(x, y, bid);
            AddHtml(x + 25, y, 200, 20, text);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!Context.Registered)
            {
                return;
            }

            var index = info.ButtonID;

            switch (index)
            {
                case -1: // CloseGump
                    {
                        break;
                    }
                case 0: // closed
                    {
                        Context.Unregister();
                        break;
                    }
                case 1: // Rules
                    {
                        // m_From.SendGump( new RulesetGump( m_From, m_Context.Ruleset, m_Context.Ruleset.Layout, m_Context ) );
                        From.SendGump(new PickRulesetGump(From, Context, Context.Ruleset));
                        break;
                    }
                case 2: // Start
                    {
                        if (Context.CheckFull())
                        {
                            Context.CloseAllGumps();
                            Context.SendReadyUpGump();
                            // m_Context.SendReadyGump();
                        }
                        else
                        {
                            From.SendMessage(
                                "You cannot start the duel before all participating players have been assigned."
                            );
                            From.SendGump(new DuelContextGump(From, Context));
                        }

                        break;
                    }
                case 3: // New Participant
                    {
                        if (Context.Participants.Count < 10)
                        {
                            Context.Participants.Add(new Participant(Context, 1));
                        }
                        else
                        {
                            From.SendMessage("The number of participating parties may not be increased further.");
                        }

                        From.SendGump(new DuelContextGump(From, Context));

                        break;
                    }
                default: // Participant
                    {
                        index -= 4;

                        if (index >= 0 && index < Context.Participants.Count)
                        {
                            From.SendGump(new ParticipantGump(From, Context, Context.Participants[index]));
                        }

                        break;
                    }
            }
        }
    }
}
