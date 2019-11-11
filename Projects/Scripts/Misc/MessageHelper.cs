using Server.Network;

namespace Server
{
  public static class MessageHelper
  {
    public static void SendLocalizedMessageTo(Item from, Mobile to, int number, int hue)
    {
      SendLocalizedMessageTo(from, to, number, "", hue);
    }

    public static void SendLocalizedMessageTo(Item from, Mobile to, int number, string args, int hue)
    {
      Packets.SendMessageLocalized(to.NetState, from.Serial, from.ItemID, MessageType.Regular, hue, 3, number, "", args);
    }

    public static void SendMessageTo(Item from, Mobile to, string text, int hue)
    {
      Packets.SendUnicodeMessage(to.NetState, from.Serial, from.ItemID, MessageType.Regular, hue, 3, "ENU", "", text);
    }
  }
}
