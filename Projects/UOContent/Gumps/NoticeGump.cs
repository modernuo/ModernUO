using System;

namespace Server.Gumps;

public class NoticeGump : WarningGump
{
    public NoticeGump(
        TextDefinition content, int width, int height,
        Action callback = null
    ) : this(
        1060637, // <CENTER>NOTICE</CENTER>
        0x7800,
        content,
        0xFFC000,
        width,
        height,
        callback
    )
    {
    }

    public NoticeGump(
        int header, int headerColor, TextDefinition content, int contentColor, int width, int height,
        Action callback = null
    ) : base(
        header,
        headerColor,
        content,
        contentColor,
        width,
        height,
        callback != null ? _ => callback() : null,
        false
    )
    {
    }
}
