using Server.Buffers;

namespace Server.Network
{
  public static class StatuePackets
  {
    public static void SendUpdateStatueAnimation(NetState ns, Serial m, int status, int animation, int frame)
    {
      if (ns == null)
        return;

      SpanWriter writer = new SpanWriter(stackalloc byte[17]);
      writer.Write((byte)0xBF); // Extended Packet ID
      writer.Write((short)17); // Dynamic Length

      writer.Write((short)0x19);
      writer.Write((byte)0x5);
      writer.Write(m);
      writer.Write((short)0xFF);
      writer.Write((byte)status);
      writer.Write((short)animation);
      writer.Write((short)frame);
    }
  }
}
