using Server.Network;

namespace Server;

public static class MessageHelper
{
    public static void SendLocalizedMessageTo(this Item from, Mobile to, int number, int hue)
    {
        SendLocalizedMessageTo(from, to, number, "", hue);
    }

    public static void SendLocalizedMessageTo(this Item from, Mobile to, int number, string args, int hue)
    {
        to?.NetState.SendMessageLocalized(from.Serial, from.ItemID, MessageType.Regular, hue, 3, number, "", args);
    }

    public static void SendMessageTo(this Item from, Mobile to, string text, int hue)
    {
        to?.NetState.SendMessage(from.Serial, from.ItemID, MessageType.Regular, hue, 3, false, "ENU", "", text);
    }
}
