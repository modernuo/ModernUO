namespace Server.Network
{
  public sealed class DamagePacketOld : Packet
  {
    public DamagePacketOld(Mobile m, int amount) : base(0xBF)
    {
      EnsureCapacity(11);

      Stream.Write((short)0x22);
      Stream.Write((byte)1);
      Stream.Write(m.Serial);

      if (amount > 255)
        amount = 255;
      else if (amount < 0)
        amount = 0;

      Stream.Write((byte)amount);
    }
  }

  public sealed class DamagePacket : Packet
  {
    public DamagePacket(Mobile m, int amount) : base(0x0B, 7)
    {
      Stream.Write(m.Serial);

      if (amount > 0xFFFF)
        amount = 0xFFFF;
      else if (amount < 0)
        amount = 0;

      Stream.Write((ushort)amount);
    }
  }
}
