using System;
using Server.Buffers;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendObjectHelpResponse(NetState ns, Serial e, string text)
    {
      if (ns == null)
        return;

      int length = 9 + text.Length * 2;
      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte) 0xB7); // Extended Packet ID
      w.Write((ushort) length); // Length

      w.Write(e);
      w.WriteBigUniNull(text);

      ns.Send(w.RawSpan);
    }

    public static void SendChangeUpdateRange(NetState ns, byte range = 18)
    {
      ns?.Send(stackalloc byte[]
      {
        0xC8, // Packet ID
        range,
      });
    }

    public static void SendChangeCombatant(NetState ns, Serial combatant)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[5]);
      w.Write((byte)0xAA); // Packet ID

      w.Write(combatant);

      ns.Send(w.RawSpan);
    }

    public static void SendDisplayHuePicker(NetState ns, Serial s, int itemId)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[9]);
      w.Write((byte)0x95); // Packet ID

      w.Write(s);
      w.Position += 2; // w.Write((short)0);
      w.Write((short)itemId);

      ns.Send(w.RawSpan);
    }

    public static void SendUnicodePrompt(NetState ns, Serial message)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[21]);
      w.Write((byte)0xC2); // Packet ID
      w.Write((short)21); // Length

      w.Write(message); // Should be Player serial?
      w.Write(message);
      // w.Position += 4; w.Write(0);
      // w.Position += 4; w.Write(0);
      // w.Position += 2; w.Write((short)0);

      ns.Send(w.RawSpan);
    }

    public static void SendDeathStatus_Dead(NetState ns)
    {
      ns?.Send(stackalloc byte[]
      {
        0x2C, // Packet ID
        0x00
      });
    }

    public static void SendDeathStatus_Alive(NetState ns)
    {
      ns?.Send(stackalloc byte[]
      {
        0x2C, // Packet ID
        0x02, // Why not 1?
      });
    }

    private static readonly byte[][] _speedControlPackets = new byte[3][];

    public static void SendSpeedControlDisabled(NetState ns)
    {
      ns?.Send(_speedControlPackets[0] ??= new byte[]
      {
        0xBF, // Extended Packet ID
        0x00,
        0x06, // Length
        0x26,
        0x00, // Disabled
      });
    }

    public static void SendSpeedControlMount(NetState ns)
    {
      ns?.Send(_speedControlPackets[1] ??= new byte[]
      {
        0xBF, // Extended Packet ID
        0x00,
        0x06, // Length
        0x26,
        0x01, // Mount
      });
    }

    public static void SendSpeedControlWalk(NetState ns)
    {
      ns?.Send(_speedControlPackets[2] ??= new byte[]
      {
        0xBF, // Extended Packet ID
        0x00,
        0x06, // Length
        0x26,
        0x02, // Walk
      });
    }

    public static void SendToggleSpecialAbility(NetState ns, short ability, bool active)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[8]);
      w.Write((byte)0xBF); // Packet ID
      w.Write((short)8); // Length

      w.Write((short)0x25); // Command
      w.Write(ability);
      w.Write(active);

      ns.Send(w.RawSpan);
    }

    public static void SendGlobalLightLevel(NetState ns, sbyte level)
    {
      ns?.Send(stackalloc byte[]
      {
        0x4F, // Packet ID
        (byte)level
      });
    }

    public static void SendLogoutAck(NetState ns)
    {
      ns?.Send(stackalloc byte[]
      {
        0xD1, // Packet ID
        0x01
      });
    }

    public static void SendWeather(NetState ns, byte type, byte density, byte temperature)
    {
      ns?.Send(stackalloc byte[]
      {
        0x65, // Packet ID
        type,
        density,
        temperature,
      });
    }

    public static void SendPlayerMove(NetState ns, Direction d)
    {
      ns?.Send(stackalloc byte[]
      {
        0x97, // Packet ID
        (byte)d
      });
    }

    public static void SendClientVersionReq(NetState ns)
    {
      ns?.Send(stackalloc byte[]
      {
        0xBD, // Packet ID
        0x03
      });
    }

    public static void SendSetWarMode(NetState ns, bool warmode)
    {
      if (warmode)
        SendInWarMode(ns);
      else
        SendInPeaceMode(ns);
    }

    public static void SendInWarMode(NetState ns)
    {
      ns?.Send(stackalloc byte[]
      {
        0x72, // Packet ID
        0x01, // War mode
        0x00,
        0x32 // ?
      });
    }

    public static void SendInPeaceMode(NetState ns)
    {
      ns?.Send(stackalloc byte[]
      {
        0x72, // Packet ID
        0x00, // Peace mode
        0x00,
        0x32 // ?
      });
    }

    public static void SendRemoveEntity(NetState ns, Serial entity)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[5]);
      w.Write((byte)0x1D); // Packet ID

      w.Write(entity);

      ns.Send(w.Span);
    }

    public static void SendServerChange(NetState ns, Mobile m, Map map)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[16]);
      w.Write((byte)0x76); // Packet ID

      w.Write((short)m.X);
      w.Write((short)m.Y);
      w.Write((short)m.Z);
      w.Position += 5;
      w.Write((short)map.Width);
      w.Write((short)map.Height);

      ns.Send(w.RawSpan);
    }

    public static void SendSkillsUpdate(NetState ns, Skills skills)
    {
      if (ns == null)
        return;

      int length = 6 + skills.Length * 9;

      SpanWriter w = new SpanWriter(stackalloc byte[length]);
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

      ns.Send(w.RawSpan);
    }

    public static void SendSkillChange(NetState ns, Skill skill)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[13]);
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

      ns.Send(w.RawSpan);
    }

    public static void SendLaunchBrowser(NetState ns, string url)
    {
      if (ns == null)
        return;

      int length = 4 + url?.Length ?? 0;
      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0xA5); // Packet ID
      w.Write((short)length); // Length

      w.WriteAsciiNull(url ?? "");

      ns.Send(w.RawSpan);
    }

    // TODO: Optimize for IEnumerable<NetState>
    public static void SendPlaySound(NetState ns, int soundId, IPoint3D target)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[12]);
      w.Write((byte)0x54); // Packet ID

      w.Write((byte)1); // flags
      w.Write((short)soundId);
      w.Position++; // volume?
      w.Write((short)target.X);
      w.Write((short)target.Y);
      w.Write((short)target.Z);

      ns.Send(w.RawSpan);
    }

    public static void SendPlayRepeatingSound(NetState ns, int soundId, IPoint3D target)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[12]);
      w.Write((byte)0x54); // Packet ID

      w.Position++; // flags
      w.Write((short)soundId);
      w.Position++; // volume?
      w.Write((short)target.X);
      w.Write((short)target.Y);
      w.Write((short)target.Z);

      ns.Send(w.RawSpan);
    }

    public static void SendPlayMusic(NetState ns, MusicName music)
    {
      ns?.Send(stackalloc byte[]
      {
        0x6D, // Packet ID
        0x00,
        (byte)music
      });
    }

    public static void SendScrollMessage(NetState ns, int type, int tip, string text)
    {
      if (ns == null)
        return;

      int length = 10 + text?.Length ?? 0;

      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0xA6); // Packet ID
      w.Write((short)length); // Length

      if (text == null)
        text = "";

      w.Write((byte)type);
      w.Write(tip);
      w.Write((ushort)text.Length);
      w.WriteAsciiFixed(text, text.Length);

      ns.Send(w.RawSpan);
    }

    public static void SendCurrentTime(NetState ns)
    {
      if (ns == null)
        return;

      // TODO: Don't call UtcNow so readily.
      DateTime now = DateTime.UtcNow;

      ns.Send(stackalloc byte[]
      {
        0x5B, // Packet ID
        (byte)now.Hour,
        (byte)now.Minute,
        (byte)now.Second
      });
    }

    public static void SendPathfindMessage(NetState ns, IPoint3D p)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[7]);
      w.Write((byte)0x38); // Packet ID

      w.Write((short)p.X);
      w.Write((short)p.Y);
      w.Write((short)p.Z);

      ns.Send(w.RawSpan);
    }

    public static void SendPingAck(NetState ns, byte ping)
    {
      ns?.Send(stackalloc byte[2]
      {
        0x73, // Packet ID
        ping,
      });
    }

    public static void SendMovementRej(NetState ns, int seq, Mobile m)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[8]);
      w.Write((byte)0x21); // Packet ID

      w.Write((byte)seq);
      w.Write((short)m.X);
      w.Write((short)m.Y);
      w.Write((byte)m.Direction);
      w.Write((sbyte)m.Z);

      ns.Send(w.RawSpan);
    }

    public static void SendMovementAck(NetState ns, Mobile m)
    {
      if (ns == null)
        return;

      SendMovementAck(ns, ns.Sequence, Notoriety.Compute(m, m));
    }

    public static void SendMovementAck(NetState ns, byte seq, byte noto)
    {
      ns?.Send(stackalloc byte[3]
      {
        0x22,
        seq,
        noto,
      });
    }

    public static void SendLoginConfirm(NetState ns, Mobile m)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[8]);
      w.Write((byte)0x1B); // Packet ID

      w.Write(m.Serial);
      w.Position++;
      w.Write((short)m.Body);
      w.Write((short)m.X);
      w.Write((short)m.Y);
      w.Write((short)m.Z);
      w.Write((byte)m.Direction);
      w.Position++;
      w.Write(-1);

      Map map = m.Map;

      if (map == null || map == Map.Internal)
        map = m.LogoutMap;

      w.Position += 4;
      w.Write((short)(map?.Width ?? 6144));
      w.Write((short)(map?.Height ?? 4096));

      ns.Send(w.RawSpan);
    }

    public static void SendLoginComplete(NetState ns)
    {
      ns?.Send(stackalloc byte[] { 0x55 });
    }

    public static void SendClearWeaponAbility(NetState ns)
    {
      ns?.Send(stackalloc byte[]
      {
        0xBF, // Extended Packet ID
        0x00,
        0x05, // Length
        0x21, // Command
        0x00
      });
    }
  }
}
