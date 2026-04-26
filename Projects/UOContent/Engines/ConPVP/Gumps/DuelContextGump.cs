using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP;

public class DuelContextGump : DynamicGump
{
    public override bool Singleton => true;

    private DuelContextGump(Mobile from, DuelContext context) : base(50, 50)
    {
        From = from;
        Context = context;
    }

    public static void DisplayTo(Mobile from, DuelContext context)
    {
        if (from?.NetState == null || context == null)
        {
            return;
        }

        var gumps = from.GetGumps();
        gumps.Close<RulesetGump>();
        gumps.Close<ParticipantGump>();

        from.SendGump(new DuelContextGump(from, context));
    }

    public Mobile From { get; }

    public DuelContext Context { get; }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var count = Context.Participants.Count;

        if (count < 3)
        {
            count = 3;
        }

        var height = 35 + 10 + 22 + 30 + 22 + 22 + 2 + count * 22 + 2 + 30;

        builder.AddPage();

        builder.AddBackground(0, 0, 300, height, 9250);
        builder.AddBackground(10, 10, 280, height - 20, 0xDAC);

        builder.AddHtml(35, 25, 230, 20, Center("Duel Setup"));

        var x = 35;
        var y = 47;

        AddGoldenButtonLabeled(ref builder, x, y, 1, "Rules");
        y += 22;
        AddGoldenButtonLabeled(ref builder, x, y, 2, "Start");
        y += 22;
        AddGoldenButtonLabeled(ref builder, x, y, 3, "Add Participant");
        y += 30;

        builder.AddHtml(35, y, 230, 20, Center("Participants"));
        y += 22;

        for (var i = 0; i < Context.Participants.Count; ++i)
        {
            var p = Context.Participants[i];

            AddGoldenButtonLabeled(
                ref builder,
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

    private static string Center(string text) => $"<CENTER>{text}</CENTER>";

    private static void AddGoldenButton(ref DynamicGumpBuilder builder, int x, int y, int bid)
    {
        builder.AddButton(x, y, 0xD2, 0xD2, bid);
        builder.AddButton(x + 3, y + 3, 0xD8, 0xD8, bid);
    }

    private static void AddGoldenButtonLabeled(ref DynamicGumpBuilder builder, int x, int y, int bid, string text)
    {
        AddGoldenButton(ref builder, x, y, bid);
        builder.AddHtml(x + 25, y, 200, 20, text);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
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
                    PickRulesetGump.DisplayTo(From, Context, Context.Ruleset);
                    break;
                }
            case 2: // Start
                {
                    if (Context.CheckFull())
                    {
                        Context.CloseAllGumps();
                        Context.SendReadyUpGump();
                    }
                    else
                    {
                        From.SendMessage(
                            "You cannot start the duel before all participating players have been assigned."
                        );
                        From.SendGump(this); // refresh-via-this
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

                    From.SendGump(this); // refresh-via-this

                    break;
                }
            default: // Participant
                {
                    index -= 4;

                    if (index >= 0 && index < Context.Participants.Count)
                    {
                        ParticipantGump.DisplayTo(From, Context, Context.Participants[index]);
                    }

                    break;
                }
        }
    }
}
