using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP;

public class ReadyGump : DynamicGump
{
    private readonly DuelContext _context;
    private readonly int _count;
    private readonly Mobile _from;

    public override bool Singleton => true;

    private ReadyGump(Mobile from, DuelContext context, int count) : base(50, 50)
    {
        _from = from;
        _context = context;
        _count = count;
    }

    public static void DisplayTo(Mobile from, DuelContext context, int count)
    {
        if (from?.NetState == null || context == null)
        {
            return;
        }

        from.SendGump(new ReadyGump(from, context, count), true);
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.SetNoClose();
        builder.SetNoMove();

        builder.AddPage();

        var parts = _context.Participants;

        var height = 25 + 20;

        for (var i = 0; i < parts.Count; ++i)
        {
            var p = parts[i];

            height += 4;

            if (p.Players.Length > 1)
            {
                height += 22;
            }

            height += p.Players.Length * 22;
        }

        height += 25;

        builder.AddBackground(0, 0, 260, height, 9250);
        builder.AddBackground(10, 10, 240, height - 20, 0xDAC);

        if (_count == -1)
        {
            builder.AddHtml(35, 25, 190, 20, Center("Ready"));
        }
        else
        {
            builder.AddHtml(35, 25, 190, 20, Center("Starting"));
            builder.AddHtml(35, 25, 190, 20, $"<DIV ALIGN=RIGHT>{_count}");
        }

        var y = 25 + 20;

        for (var i = 0; i < parts.Count; ++i)
        {
            var p = parts[i];

            y += 4;

            var isAllReady = true;
            var yStore = y;
            var offset = 0;

            if (p.Players.Length > 1)
            {
                builder.AddHtml(35 + 14, y, 176, 20, $"Participant #{i + 1}");
                y += 22;
                offset = 10;
            }

            for (var j = 0; j < p.Players.Length; ++j)
            {
                var pl = p.Players[j];

                if (pl?.Ready == true)
                {
                    builder.AddImage(35 + offset, y + 4, 0x939);
                }
                else
                {
                    builder.AddImage(35 + offset, y + 4, 0x938);
                    isAllReady = false;
                }

                var name = pl == null ? "(Empty)" : pl.Mobile.Name;

                builder.AddHtml(35 + offset + 14, y, 166, 20, name);

                y += 22;
            }

            if (p.Players.Length > 1)
            {
                builder.AddImage(35, yStore + 4, isAllReady ? 0x939 : 0x938);
            }
        }
    }

    private static string Center(string text) => $"<CENTER>{text}</CENTER>";

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
    }
}
