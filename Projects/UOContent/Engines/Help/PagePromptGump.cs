using Server.Gumps;
using Server.Network;

namespace Server.Engines.Help;

public sealed class PagePromptGump : StaticGump<PagePromptGump>
{
    private readonly Mobile _from;
    private readonly PageType _type;

    public override bool Singleton => true;

    public PagePromptGump(Mobile from, PageType type) : base(0, 0)
    {
        _from = from;
        _type = type;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddBackground(50, 50, 540, 350, 2600);

        builder.AddPage();

        builder.AddHtmlLocalized(264, 80, 200, 24, 1062524); // Enter Description
        // Please enter a brief description (up to 200 characters) of your problem:
        builder.AddHtmlLocalized(120, 108, 420, 48, 1062638);

        builder.AddBackground(100, 148, 440, 200, 3500);
        builder.AddTextEntry(120, 168, 400, 200, 1153, 0, "");

        builder.AddButton(175, 355, 2074, 2075, 1);  // Okay
        builder.AddButton(405, 355, 2073, 2072, 0); // Cancel
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0)
        {
            _from.SendLocalizedMessage(501235, "", 0x35); // Help request aborted.
            return;
        }

        var text = info.GetTextEntry(0)?.Trim() ?? "";

        if (text.Length == 0)
        {
            _from.SendMessage(0x35, "You must enter a description.");
            _from.SendGump(new PagePromptGump(_from, _type));
        }
        else
        {
            /* The next available Counselor/Game Master will respond as soon as possible.
             * Please check your Journal for messages every few minutes.
             */
            _from.SendLocalizedMessage(501234, "", 0x35);

            PageQueue.Enqueue(new PageEntry(_from, text, _type));
        }
    }
}
