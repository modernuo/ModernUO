using Server.Buffers;
using Server.Network;

namespace Server.Engines.Chat
{
  public static class ChatPackets
  {
    public static void SendChatMessage(NetState ns, Mobile who, int number, string param1, string param2)
    {
      if (ns == null)
        return;

      param1 ??= "";
      param2 ??= "";

      int length = 13 + (param1.Length + param2.Length) * 2;
      //base(0xB2)

      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0xB2); // Packet ID
      writer.Write((ushort)length); // Dynamic Length

      writer.Write((ushort)(number - 20));

      if (who != null)
        writer.WriteAsciiFixed(who.Language, 4);
      else
        writer.Position += 4;

      writer.WriteBigUniNull(param1);
      writer.WriteBigUniNull(param2);

      ns.Send(writer.Span);
    }
  }
}
