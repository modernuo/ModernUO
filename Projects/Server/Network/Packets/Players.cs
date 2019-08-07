using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public static WriteFixedPacketMethod<byte> ChangeUpdateRange(out int length)
    {
      length = 2;
      static void write(Memory<byte> mem, byte range)
      {
        mem.Span[0] = 0xC8; // Packet ID
        mem.Span[1] = range;
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial> ChangeCombatant(out int length)
    {
      length = 5;
      static void write(Memory<byte> mem, Serial combatant)
      {
        SpanWriter w = new SpanWriter(mem.Span, 5);
        w.Write((byte)0xAA); // Packet ID

        w.Write(combatant);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial, int> DisplayHuePicker(out int length)
    {
      length = 9;
      static void write(Memory<byte> mem, Serial s, int itemId)
      {
        SpanWriter w = new SpanWriter(mem.Span, 9);
        w.Write((byte)0x95); // Packet ID

        w.Write(s);
        w.Position += 2; // w.Write((short)0);
        w.Write((short)itemId);
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial, Serial> UnicodePrompt(out int length)
    {
      length = 21;
      static void write(Memory<byte> mem, Serial player, Serial message)
      {
        SpanWriter w = new SpanWriter(mem.Span, 21);
        w.Write((byte)0xC2); // Packet ID
        w.Write((short)21); // Length

        w.Write(player);
        w.Write(message);
        // w.Position += 4; w.Write(0);
        // w.Position += 4; w.Write(0);
        // w.Position += 2; w.Write((short)0);
      }

      return write;
    }

    public static WriteFixedPacketMethod DeathStatus_Dead(out int length)
    {
      length = 2;
      static void write(Memory<byte> mem)
      {
        mem.Span[0] = 0x2C; // Packet ID
      }

      return write;
    }

    public static WriteFixedPacketMethod DeathStatus_Alive(out int length)
    {
      length = 2;
      static void write(Memory<byte> mem)
      {
        mem.Span[0] = 0x2C; // Packet ID
        mem.Span[1] = 2;
      }

      return write;
    }

    public static WriteFixedPacketMethod SpeedControl_Disabled(out int length)
    {
      length = 6;
      static void write(Memory<byte> mem)
      {
        mem.Span[0] = 0xBF; // Packet ID
        mem.Span[2] = 0x06; // Length
        mem.Span[4] = 0x26;
        mem.Span[5] = 0; // Disabled
      }

      return write;
    }

    public static WriteFixedPacketMethod SpeedControl_Walk(out int length)
    {
      length = 6;
      static void write(Memory<byte> mem)
      {
        mem.Span[0] = 0xBF; // Packet ID
        mem.Span[2] = 0x06; // Length
        mem.Span[4] = 0x26;
        mem.Span[5] = 1; // Mount
      }

      return write;
    }

    public static WriteFixedPacketMethod SpeedControl_Mount(out int length)
    {
      length = 6;
      static void write(Memory<byte> mem)
      {
        mem.Span[0] = 0xBF; // Packet ID
        mem.Span[2] = 0x06; // Length
        mem.Span[4] = 0x26;
        mem.Span[5] = 2; // Walk
      }

      return write;
    }

    public static WriteFixedPacketMethod<short, bool> ToggleSpecialAbility(out int length)
    {
      length = 8;
      static void write(Memory<byte> mem, short ability, bool active)
      {
        SpanWriter w = new SpanWriter(mem.Span, 8);
        w.Write((byte)0xBF); // Packet ID
        w.Write((short)8); // Length

        w.Write((short)0x25); // Command
        w.Write(ability);
        w.WriteIfTrue(active);
      }

      return write;
    }

    public static WriteFixedPacketMethod<sbyte> GlobalLightLevel(out int length)
    {
      length = 2;
      static void write(Memory<byte> mem, sbyte level)
      {
        mem.Span[0] = 0x4F; // Packet ID
        mem.Span[1] = (byte)level;
      }

      return write;
    }

    public static WriteFixedPacketMethod LogoutAck(out int length)
    {
      length = 2;
      static void write(Memory<byte> mem)
      {
        mem.Span[0] = 0xD1; // Packet ID
        mem.Span[1] = 0x01;
      }

      return write;
    }

    public static WriteFixedPacketMethod<int, int, int> Weather(out int length)
    {
      length = 4;
      static void write(Memory<byte> mem, int type, int density, int temperature)
      {
        mem.Span[0] = 0x65; // Packet ID
        mem.Span[1] = (byte)type;
        mem.Span[2] = (byte)density;
        mem.Span[3] = (byte)temperature;
      }

      return write;
    }

    public static WriteFixedPacketMethod<Direction> PlayerMove(out int length)
    {
      length = 2;
      static void write(Memory<byte> mem, Direction d)
      {
        mem.Span[0] = 0x97; // Packet ID
        mem.Span[1] = (byte)d;
      }

      return write;
    }

    public static WriteFixedPacketMethod ClientVersionReq(out int length)
    {
      length = 3;
      static void write(Memory<byte> mem)
      {
        mem.Span[0] = 0xBD; // Packet ID
        mem.Span[1] = 0x03; // Length
      }

      return write;
    }
  }
}
