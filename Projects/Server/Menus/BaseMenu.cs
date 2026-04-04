using Server.Network;

namespace Server.Menus;

public abstract class BaseMenu : IMenu
{
    private static int _nextSerial;

    public int Serial { get; }

    public abstract int EntryLength { get; }

    public BaseMenu()
    {
        var serial = ++_nextSerial;
        if (serial <= 0)
        {
            serial = 1;
            _nextSerial = 1;
        }

        Serial = serial;
    }

    public abstract void SendTo(NetState state);

    public virtual void OnCancel(NetState state)
    {
    }

    public virtual void OnResponse(NetState state, int index)
    {
    }
}
