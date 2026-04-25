using Server.Items;
using Server.Network;

namespace Server.Gumps;

public class DawnsMusicBoxGump : DynamicGump
{
    private readonly DawnsMusicBox _box;

    public override bool Singleton => true;

    private DawnsMusicBoxGump(DawnsMusicBox box) : base(60, 36) => _box = box;

    public static void DisplayTo(Mobile from, DawnsMusicBox box)
    {
        if (from?.NetState == null || box?.Deleted != false)
        {
            return;
        }

        from.SendGump(new DawnsMusicBoxGump(box));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 273, 324, 0x13BE);
        builder.AddImageTiled(10, 10, 253, 20, 0xA40);
        builder.AddImageTiled(10, 40, 253, 244, 0xA40);
        builder.AddImageTiled(10, 294, 253, 20, 0xA40);
        builder.AddAlphaRegion(10, 10, 253, 304);
        builder.AddButton(10, 294, 0xFB1, 0xFB2, 0);
        builder.AddHtmlLocalized(45, 296, 450, 20, 1060051, 0x7FFF); // CANCEL
        builder.AddHtmlLocalized(14, 12, 273, 20, 1075130, 0x7FFF);  // Choose a track to play

        var page = 1;
        int i, y = 49;

        builder.AddPage(page);

        for (i = 0; i < _box.Tracks.Count; i++, y += 24)
        {
            var info = DawnsMusicBox.GetInfo(_box.Tracks[i]);

            if (i > 0 && i % 10 == 0)
            {
                builder.AddButton(228, 294, 0xFA5, 0xFA6, 0, GumpButtonType.Page, page + 1);

                builder.AddPage(page + 1);
                y = 49;

                builder.AddButton(193, 294, 0xFAE, 0xFAF, 0, GumpButtonType.Page, page);

                page++;
            }

            if (info == null)
            {
                continue;
            }

            builder.AddButton(19, y, 0x845, 0x846, 100 + i);
            builder.AddHtmlLocalized(44, y - 2, 213, 20, info.Name, 0x7FFF);
        }

        if (i % 10 == 0)
        {
            builder.AddButton(228, 294, 0xFA5, 0xFA6, 0, GumpButtonType.Page, page + 1);

            builder.AddPage(page + 1);
            y = 49;

            builder.AddButton(193, 294, 0xFAE, 0xFAF, 0, GumpButtonType.Page, page);
        }

        builder.AddButton(19, y, 0x845, 0x846, 1);
        builder.AddHtmlLocalized(44, y - 2, 213, 20, 1075207, 0x7FFF); // Stop Song
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_box?.Deleted != false)
        {
            return;
        }

        var m = sender.Mobile;

        if (!_box.IsChildOf(m.Backpack) && !_box.IsLockedDown)
        {
            // You must have the item in your backpack or locked down in order to use it.
            m.SendLocalizedMessage(1061856);
        }
        else if (_box.IsLockedDown && !_box.HasAccess(m))
        {
            m.SendLocalizedMessage(502691); // You must be the owner to use this.
        }
        else if (info.ButtonID == 1)
        {
            _box.EndMusic(m);
        }
        else if (info.ButtonID >= 100 && info.ButtonID - 100 < _box.Tracks.Count)
        {
            _box.PlayMusic(m, _box.Tracks[info.ButtonID - 100]);
        }
    }
}
