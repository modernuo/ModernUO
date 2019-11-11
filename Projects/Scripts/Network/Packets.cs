using Server.Buffers;

namespace Server.Network
{
  public static class ContentPackets
  {
    public static void SendDisplayHelpTopic(NetState ns, int topicID, bool display)
    {
      if (ns == null)
        return;

      SpanWriter writer = new SpanWriter(stackalloc byte[11]);
      writer.Write((byte)0xBF); // Packet ID
      writer.Write((ushort)11); // Dynamic Length

      writer.Write((short)0x17); // Command
      writer.Write((byte)1);
      writer.Write(topicID);
      writer.Write(display);

      ns.Send(writer.Span);
    }
  }
}
