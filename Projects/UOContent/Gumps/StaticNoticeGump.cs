using System;

namespace Server.Gumps;

public abstract class StaticNoticeGump<T> : StaticWarningGump<T> where T : StaticNoticeGump<T>
{
    public override int Header => 1060637; // <CENTER>NOTICE</CENTER>
    public sealed override bool CancelButton => false;

    public StaticNoticeGump(Action callback = null) : base(callback != null ? _ => callback() : null)
    {
    }
}
