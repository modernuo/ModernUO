using System;
using Server.Network;

namespace Server.Gumps;

public class WarningGump : DynamicGump
{
    private readonly int _header;
    private readonly int _headerColor;
    private readonly int _contentColor;
    private readonly int _width;
    private readonly int _height;
    private readonly bool _cancelButton;
    private readonly TextDefinition _content;
    private readonly Action<bool> _callback;

    public WarningGump(
        TextDefinition content, int width, int height,
        Action<bool> callback = null, bool cancelButton = true
    ) : this(
        1060635, // <CENTER>WARNING</CENTER>
        0x7800,
        content,
        0xFFC000,
        width,
        height,
        callback,
        cancelButton
    )
    {
    }

    public WarningGump(
        int header, int headerColor, TextDefinition content, int contentColor, int width, int height,
        Action<bool> callback = null, bool cancelButton = true
    ) : base((640 - width) / 2, (480 - height) / 2)
    {
        _header = header;
        _headerColor = headerColor;
        _content = content;
        _contentColor = contentColor;
        _width = width;
        _height = height;
        _cancelButton = cancelButton;
        _callback = callback;
    }

    protected sealed override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.SetNoClose();

        builder.AddPage();

        builder.AddBackground(0, 0, _width, _height, 5054);

        builder.AddImageTiled(10, 10, _width - 20, 20, 2624);
        builder.AddAlphaRegion(10, 10, _width - 20, 20);
        builder.AddHtmlLocalized(10, 10, _width - 20, 20, _header, _headerColor);

        builder.AddImageTiled(10, 40, _width - 20, _height - 80, 2624);
        builder.AddAlphaRegion(10, 40, _width - 20, _height - 80);

        _content.AddHtmlText(
            ref builder,
            10,
            40,
            _width - 20,
            _height - 80,
            scroll: true,
            numberColor: _contentColor,
            stringColor: _contentColor
        );

        builder.AddImageTiled(10, _height - 30, _width - 20, 20, 2624);
        builder.AddAlphaRegion(10, _height - 30, _width - 20, 20);

        builder.AddButton(10, _height - 30, 4005, 4007, 1);
        builder.AddHtmlLocalized(40, _height - 30, 170, 20, 1011036, 0x7FFF); // OKAY

        if (_cancelButton)
        {
            builder.AddButton(10 + (_width - 20) / 2, _height - 30, 4005, 4007, 0);
            builder.AddHtmlLocalized(40 + (_width - 20) / 2, _height - 30, 170, 20, 1011012, 32767); // CANCEL
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info) => _callback?.Invoke(info.ButtonID == 1);
}
