using Server.Buffers;
using Server.Network;

namespace Server.Engines.PartySystem
{
  public static class PartyPackets
  {
    public static void SendPartyEmptyList(NetState ns, Mobile m)
    {
      if (ns == null)
        return;

      SpanWriter writer = new SpanWriter(stackalloc byte[11]);
      writer.Write((byte)0xBF); // Packet ID
      writer.Write((short)0x7); // Dynamic Length

      writer.Write((short)0x06);
      writer.Write((byte)0x02);
      writer.Position++; // Writer(0); // Number of members
      writer.Write(m.Serial);

      ns.Send(writer.Span);
    }

    public static void SendPartyMemberList(NetState ns, Party p)
    {
      if (ns == null)
        return;

      ushort length = (ushort)(7 + p.Count * 4);

      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0xBF); // Packet ID
      writer.Write((ushort)length); // Dynamic Length

      writer.Write((short)0x06);
      writer.Write((byte)0x01);
      writer.Write((byte)p.Count);

      for (int i = 0; i < p.Count; ++i)
        writer.Write(p[i].Mobile.Serial);

      ns.Send(writer.Span);
    }

    public static void SendPartyRemoveMember(NetState ns, Mobile removed, Party p)
    {
      if (ns == null)
        return;

      ushort length = (ushort)(11 + p.Count * 4);

      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0xBF); // Packet ID
      writer.Write((short)length); // Dynamic Length

      writer.Write((short)0x06);
      writer.Write((byte)0x02);
      writer.Write((byte)p.Count);
      writer.Write(removed.Serial);

      for (int i = 0; i < p.Count; ++i)
        writer.Write(p[i].Mobile.Serial);

      ns.Send(writer.Span);
    }

    public static void SendPartyMessage(NetState ns, bool toAll, Mobile from, string text)
    {
      if (ns == null)
        return;

      ushort length = (ushort)(12 + text.Length * 2);

      SpanWriter writer = new SpanWriter(stackalloc byte[length]);
      writer.Write((byte)0xBF); // Packet ID
      writer.Write((short)length); // Dynamic Length

      writer.Write((short)0x06);
      writer.Write((byte)(toAll ? 0x04 : 0x03));
      writer.Write(from.Serial);

      writer.WriteBigUniNull(text);

      ns.Send(writer.Span);
    }

    public static void SendPartyInvitation(NetState ns, Mobile leader)
    {
      if (ns == null)
        return;

      SpanWriter writer = new SpanWriter(stackalloc byte[10]);
      writer.Write((byte)0xBF); // Packet ID
      writer.Write((short)0x0A); // Dynamic Length

      writer.Write((short)0x06);
      writer.Write((byte)0x07);
      writer.Write(leader.Serial);

      ns.Send(writer.Span);
    }

    public static void SendPartyMessageLocalizedToAll(Party p, Serial serial, int graphic, MessageType type, int hue, int font, int number, string name = "",
      string args = "")
    {
      for (int i = 0; i < p.Members.Count; ++i)
        Packets.SendMessageLocalized(p.Members[i].Mobile.NetState, serial, graphic, type, hue, font, number, name, args);

      for (int i = 0; i < p.Listeners.Count; ++i)
      {
        Mobile mob = p.Listeners[i];

        if (mob.Party != p)
          Packets.SendMessageLocalized(mob.NetState, serial, graphic, type, hue, font, number, name, args);
      }
    }

    public static void SendPartyMessageLocalizedAffixToAll(Party p, Serial serial, int graphic, MessageType type, int hue, int font, int number,
      string name = "", AffixType affixType = AffixType.System, string affix = "", string args = "")
    {
      for (int i = 0; i < p.Members.Count; ++i)
        Packets.SendMessageLocalizedAffix(p.Members[i].Mobile.NetState, serial, graphic, type, hue, font, number, name, affixType, affix, args);

      for (int i = 0; i < p.Listeners.Count; ++i)
      {
        Mobile mob = p.Listeners[i];

        if (mob.Party != p)
          Packets.SendMessageLocalizedAffix(mob.NetState, serial, graphic, type, hue, font, number, name, affixType, affix, args);
      }
    }

    public static void SendPartyUnicodeMessageToAll(Party p, Serial serial, int graphic, MessageType type, int hue, int font, string lang, string name,
      string text)
    {
      for (int i = 0; i < p.Members.Count; ++i)
        Packets.SendUnicodeMessage(p.Members[i].Mobile.NetState, serial, graphic, type, hue, font, lang, name, text);

      for (int i = 0; i < p.Listeners.Count; ++i)
      {
        Mobile mob = p.Listeners[i];

        if (mob.Party != p)
          Packets.SendUnicodeMessage(p.Members[i].Mobile.NetState, serial, graphic, type, hue, font, lang, name, text);
      }
    }

    public static void SendPartyAsciiMessageToAll(Party p, Serial serial, int graphic, MessageType type, int hue, int font, string name, string text)
    {
      for (int i = 0; i < p.Members.Count; ++i)
        Packets.SendAsciiMessage(p.Members[i].Mobile.NetState, serial, graphic, type, hue, font, name, text);

      for (int i = 0; i < p.Listeners.Count; ++i)
      {
        Mobile mob = p.Listeners[i];

        if (mob.Party != p)
          Packets.SendAsciiMessage(p.Members[i].Mobile.NetState, serial, graphic, type, hue, font, name, text);
      }
    }
  }
}
