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
    public static void SendChangeUpdateRange(NetState ns, byte range)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(2);
      span[0] = 0xC8; // Packet ID
      span[1] = range;

      _ = ns.Flush(2);
    }

    public static void SendChangeCombatant(NetState ns, Serial combatant)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(5));
      w.Write((byte)0xAA); // Packet ID

      w.Write(combatant);

      _ = ns.Flush(5);
    }

    public static void SendDisplayHuePicker(NetState ns, Serial s, int itemId)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(9));
      w.Write((byte)0x95); // Packet ID

      w.Write(s);
      w.Position += 2; // w.Write((short)0);
      w.Write((short)itemId);

      _ = ns.Flush(9);
    }

    public static void SendUnicodePrompt(NetState ns, Serial player, Serial message)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(21));
      w.Write((byte)0xC2); // Packet ID
      w.Write((short)21); // Length

      w.Write(player);
      w.Write(message);
      // w.Position += 4; w.Write(0);
      // w.Position += 4; w.Write(0);
      // w.Position += 2; w.Write((short)0);

      _ = ns.Flush(21);
    }

    public static void SendDeathStatus_Dead(NetState ns)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(2);
      span[0] = 0x2C; // Packet ID

      _ = ns.Flush(2);
    }

    public static void SendDeathStatus_Alive(NetState ns)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(2);
      span[0] = 0x2C; // Packet ID
      span[1] = 2; // Why not 1?

      _ = ns.Flush(2);
    }

    public static void SendSpeedControlDisabled(NetState ns)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(6);

      span[0] = 0xBF; // Packet ID
      span[2] = 0x06; // Length
      span[4] = 0x26;

      _ = ns.Flush(6);
    }

    public static void SendSpeedControlMount(NetState ns)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(6);

      span[0] = 0xBF; // Packet ID
      span[2] = 0x06; // Length
      span[4] = 0x26;
      span[5] = 1; // Mount

      _ = ns.Flush(6);
    }

    public static void SendSpeedControlWalk(NetState ns)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(6);

      span[0] = 0xBF; // Packet ID
      span[2] = 0x06; // Length
      span[4] = 0x26;
      span[5] = 2; // Walk

      _ = ns.Flush(6);
    }

    public static void SendToggleSpecialAbility(NetState ns, short ability, bool active)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(8));
      w.Write((byte)0xBF); // Packet ID
      w.Write((short)8); // Length

      w.Write((short)0x25); // Command
      w.Write(ability);
      w.Write(active);

      _ = ns.Flush(8);
    }

    public static void SendGlobalLightLevel(NetState ns, sbyte level)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(2);
      span[0] = 0x4F; // Packet ID
      span[1] = (byte)level;

      _ = ns.Flush(2);
    }

    public static void SendLogoutAck(NetState ns)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(2);
      span[0] = 0xD1; // Packet ID
      span[1] = 0x01;

      _ = ns.Flush(2);
    }

    public static void SendWeather(NetState ns, int type, int density, int temperature)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(4);
      span[0] = 0x65; // Packet ID
      span[1] = (byte)type;
      span[2] = (byte)density;
      span[3] = (byte)temperature;

      _ = ns.Flush(4);
    }

    public static void SendPlayerMove(NetState ns, Direction d)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(2);
      span[0] = 0x97; // Packet ID
      span[1] = (byte)d;

      _ = ns.Flush(2);
    }

    public static void SendClientVersionReq(NetState ns)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(3);
      span[0] = 0xBD; // Packet ID
      span[2] = 0x03;

      _ = ns.Flush(3);
    }

    public static void SendDeleteResult(NetState ns, DeleteResultType res)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(2);
      span[0] = 0x85; // Packet ID
      span[1] = (byte)res;

      _ = ns.Flush(2);
    }

    public static void SendInWarMode(NetState ns)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(5);
      span[0] = 0x72; // Packet ID
      span[1] = 0x01; // War mode
      span[3] = 0x32; // ?

      _ = ns.Flush(5);
    }

    public static void SendInPeaceMode(NetState ns)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(5);
      span[0] = 0x72; // Packet ID
      span[3] = 0x32; // ?

      _ = ns.Flush(5);
    }

    public static void SendRemoveEntity(NetState ns, Serial entity)
    {
      Span<byte> span = ns.SendPipe.Writer.GetSpan(5);
      span[0] = 0x1D; // Packet ID

      span[1] = (byte)(entity >> 24);
      span[2] = (byte)(entity >> 16);
      span[3] = (byte)(entity >> 8);
      span[4] = (byte)entity;

      _ = ns.Flush(5);
    }

    public static void SendServerChange(NetState ns, Mobile m, Map map)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(16));
      w.Write((byte)0x76); // Packet ID

      w.Write((short)m.X);
      w.Write((short)m.Y);
      w.Write((short)m.Z);
      w.Position += 5;
      w.Write((short)map.Width);
      w.Write((short)map.Height);

      _ = ns.Flush(16);
    }

    public static void SendSkillsUpdate(NetState ns, Skills skills)
    {
      int length = 6 + skills.Length * 9;

      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(length));
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

        _ = ns.Flush(length);
    }

    public static void SendSkillChange(NetState ns, Skill skill)
    {
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(13));
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

      _ = ns.Flush(13);
    }

    public static void SendLaunchBrowser(NetState ns, string url)
    {
      int length = 4 + url?.Length ?? 0;
      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(length));
      w.Write((byte)0xA5); // Packet ID
      w.Write((short)length); // Length

      w.WriteAsciiNull(url ?? "");

      _ = ns.Flush(length);
    }
  }
}
