using System;

namespace Server.Network
{
  public enum DeleteResultType : byte
  {
    PasswordInvalid,
    CharNotExist,
    CharBeingPlayed,
    CharTooYoung,
    CharQueued,
    BadRequest
  }

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

    public static WriteFixedPacketMethod<DeleteResultType> DeleteResult(out int length)
    {
      length = 2;

      static void write(Memory<byte> mem, DeleteResultType res)
      {
        mem.Span[0] = 0x85; // Packet ID
        mem.Span[1] = (byte)res;
      }

      return write;
    }

    public static WriteFixedPacketMethod InWarMode(out int length)
    {
      length = 5;

      static void write(Memory<byte> mem)
      {
        mem.Span[0] = 0x72; // Packet ID
        mem.Span[1] = 0x01; // War mode
        mem.Span[3] = 0x32; // ?
      }

      return write;
    }

    public static WriteFixedPacketMethod InPeaceMode(out int length)
    {
      length = 5;

      static void write(Memory<byte> mem)
      {
        mem.Span[0] = 0x72; // Packet ID
        mem.Span[3] = 0x32; // ?
      }

      return write;
    }

    public static WriteFixedPacketMethod<Serial> RemoveEntity(out int length)
    {
      length = 5;

      static void write(Memory<byte> mem, Serial entity)
      {
        mem.Span[0] = 0x1D; // Packet ID

        mem.Span[1] = (byte)(entity >> 24);
        mem.Span[2] = (byte)(entity >> 16);
        mem.Span[3] = (byte)(entity >> 8);
        mem.Span[4] = (byte)entity;
      }

      return write;
    }

    public static WriteFixedPacketMethod<Mobile, Map> ServerChange(out int length)
    {
      length = 16;

      static void write(Memory<byte> mem, Mobile m, Map map)
      {
        SpanWriter w = new SpanWriter(mem.Span, 16);
        w.Write((byte)0x76); // Packet ID

        w.Write((short)m.X);
        w.Write((short)m.Y);
        w.Write((short)m.Z);
        w.Position += 5;
        w.Write((short)map.Width);
        w.Write((short)map.Height);
      }

      return write;
    }

    public static WriteDynamicPacketMethod<Skills> SkillsUpdate(out int length, Skills skills)
    {
      length = 6 + skills.Length * 9;

      static int write(Memory<byte> mem, int length, Skills skills)
      {
        SpanWriter w = new SpanWriter(mem.Span, length);
        w.Write((byte)0x3A); // Packet ID
        w.Write((short)length); // Length

        w.Write((byte)0x02); // type: absolute, capped

        for (int i = 0; i < skills.Length; ++i)
        {
          Skill s = skills[i];

          double v = s.NonRacialValue;
          int uv = (int)(v * 10);

          if (uv < 0)
            uv = 0;
          else if (uv >= 0x10000)
            uv = 0xFFFF;

          w.Write((ushort)(s.Info.SkillID + 1));
          w.Write((ushort)uv);
          w.Write((ushort)s.BaseFixedPoint);
          w.Write((byte)s.Lock);
          w.Write((ushort)s.CapFixedPoint);
        }

        return length;
      }

      return write;
    }

    public static WriteFixedPacketMethod<Skill> SkillChange(out int length)
    {
      length = 13;

      static void write(Memory<byte> mem, Skill skill)
      {
        SpanWriter w = new SpanWriter(mem.Span, 13);
        w.Write((byte)0x3A); // Packet ID
        w.Write((short)13); // Length


        double v = skill.NonRacialValue;
        int uv = (int)(v * 10);

        if (uv < 0)
          uv = 0;
        else if (uv >= 0x10000)
          uv = 0xFFFF;

        w.Write((byte)0xDF); // type: delta, capped
        w.Write((ushort)skill.Info.SkillID);
        w.Write((ushort)uv);
        w.Write((ushort)skill.BaseFixedPoint);
        w.Write((byte)skill.Lock);
        w.Write((ushort)skill.CapFixedPoint);
      }

      return write;
    }

    public static WriteDynamicPacketMethod<string> LaunchBrowser(out int length, string url)
    {
      length = 4 + url?.Length ?? 0;

      static int write(Memory<byte> mem, int length, string url)
      {
        SpanWriter w = new SpanWriter(mem.Span, length);
        w.Write((byte)0xA5); // Packet ID
        w.Write((short)length); // Length

        w.WriteAsciiNull(url ?? "");

        return length;
      }

      return write;
    }
  }
}
