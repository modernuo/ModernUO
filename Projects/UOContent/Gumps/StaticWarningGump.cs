using System;
using Server.Network;

namespace Server.Gumps;

public abstract class StaticWarningGump<T> : StaticGump<T> where T : StaticWarningGump<T>
{
    public virtual int Header => 1060635; // <CENTER>WARNING</CENTER>
    public virtual int HeaderColor => 0x7800;
    public virtual string ContentColor => "#FFC000";
    public abstract int Width { get; }
    public abstract int Height { get; }
    public virtual bool CancelButton => true;

    // If this is overridden, then the content will be localized and cached.
    public virtual int StaticLocalizedContent => 0;
    public virtual int StaticLocalizedContentColor => 0x7F00;

    public virtual string Content => null;

    private readonly Action<bool> _callback;

    public StaticWarningGump(Action<bool> callback = null) : base(0, 0)
    {
        _callback = callback;
        X = (640 - Width) / 2;
        Y = (480 - Height) / 2;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        var width = Width;
        var height = Height;
        var header = Header;
        var headerColor = HeaderColor;
        var cancelButton = CancelButton;

        builder.SetNoClose();

        builder.AddPage();

        builder.AddBackground(0, 0, width, height, 5054);

        builder.AddImageTiled(10, 10, width - 20, 20, 2624);
        builder.AddAlphaRegion(10, 10, width - 20, 20);
        builder.AddHtmlLocalized(10, 10, width - 20, 20, header, headerColor);

        builder.AddImageTiled(10, 40, width - 20, height - 80, 2624);
        builder.AddAlphaRegion(10, 40, width - 20, height - 80);

        if (StaticLocalizedContent > 0)
        {
            builder.AddHtmlLocalized(
                10,
                40,
                width - 20,
                height - 80,
                StaticLocalizedContent,
                StaticLocalizedContentColor,
                false,
                true
            );
        }
        else
        {
            builder.AddHtmlPlaceholder(
                10,
                40,
                width - 20,
                height - 80,
                "content",
                false,
                true
            );
        }

        builder.AddImageTiled(10, height - 30, width - 20, 20, 2624);
        builder.AddAlphaRegion(10, height - 30, width - 20, 20);

        builder.AddButton(10, height - 30, 4005, 4007, 1);
        builder.AddHtmlLocalized(40, height - 30, 170, 20, 1011036, 32767); // OKAY

        if (cancelButton)
        {
            builder.AddButton(10 + (width - 20) / 2, height - 30, 4005, 4007, 0);
            builder.AddHtmlLocalized(40 + (width - 20) / 2, height - 30, 170, 20, 1011012, 32767); // CANCEL
        }
    }

    protected sealed override void BuildStrings(ref GumpStringsBuilder builder)
    {
        builder.SetHtmlText("content", Content, ContentColor);
    }

    public override void OnResponse(NetState sender, in RelayInfo info) => _callback?.Invoke(info.ButtonID == 1);
}
