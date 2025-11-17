using Server.Gumps;
using Server.Network;

namespace Server.Engines.Help;

public sealed class PageResponseGump : StaticGump<PageResponseGump>
{
    private readonly Mobile _from;
    private readonly string _name;
    private readonly string _text;

    public PageResponseGump(Mobile from, string name, string text) : base(0, 0)
    {
        _from = from;
        _name = name;
        _text = text;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddBackground(50, 25, 540, 430, 2600);

        builder.AddPage();

        // <CENTER><U>Ultima Online Help Response</U></CENTER>
        builder.AddHtmlLocalized(150, 40, 360, 40, 1062610);

        builder.AddHtml(80, 90, 480, 290, $"{_name} tells {_from.Name}: {_text}", background: true, scrollbar: true);

        // Clicking the OKAY button will remove the response you have received.
        builder.AddHtmlLocalized(80, 390, 480, 40, 1062611);
        builder.AddButton(400, 417, 2074, 2075, 1); // OKAY

        builder.AddButton(475, 417, 2073, 2072, 0); // CANCEL
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID != 1)
        {
            _from.SendGump(new MessageSentGump(_from, _name, _text));
        }
    }
}
