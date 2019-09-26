using System;
using Server.Buffers;

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

  public enum PMMessage : byte
  {
    CharNoExist = 1,
    CharExists = 2,
    CharInWorld = 5,
    LoginSyncError = 6,
    IdleWarning = 7
  }

  public enum ALRReason : byte
  {
    Invalid = 0x00,
    InUse = 0x01,
    Blocked = 0x02,
    BadPass = 0x03,
    Idle = 0xFE,
    BadComm = 0xFF
  }

  public static partial class Packets
  {
    public static void SendDeleteResult(NetState ns, DeleteResultType res)
    {
      if (ns == null)
        return;

      Span<byte> span = ns.SendPipe.Writer.GetSpan(2);
      span[0] = 0x85; // Packet ID
      span[1] = (byte)res;

      // Not compressed with Huffman
      _ = ns.Flush(2);
    }

    public static void SendPopupMessage(NetState ns, PMMessage msg)
    {
      ns?.Send(stackalloc byte[]
      {
        0x53, // Packet ID
        (byte)msg
      });
    }

    public static void SendAccountLoginRej(NetState ns, ALRReason reason)
    {
      if (ns == null)
        return;

      Span<byte> span = ns.SendPipe.Writer.GetSpan(2);
      span[0] = 0x82; // Packet ID
      span[1] = (byte)reason;

      // Not compressed with Huffman
      _ = ns.Flush(2);
    }

    public static void SendSupportedFeatures(NetState ns)
    {
      if (ns == null)
        return;

      int length = ns.ExtendedSupportedFeatures ? 7 : 5;

      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0xB9); // Packet ID
      w.Write((short)length); // Length

      FeatureFlags flags = ExpansionInfo.CoreExpansion.SupportedFeatures;

      if (ns.Account?.Limit >= 6)
      {
        flags &= ~FeatureFlags.UOTD;
        flags |= FeatureFlags.LiveAccount |
          (ns.Account.Limit > 6 ? FeatureFlags.SeventhCharacterSlot : FeatureFlags.SixthCharacterSlot);
      }

      if (ns.ExtendedSupportedFeatures)
        w.Write((uint)flags);
      else
        w.Write((ushort)flags);

      ns.Send(w.RawSpan);
    }

    public static void SendCharacterList(NetState ns, CityInfo[] info)
    {
      if (ns == null)
        return;

      if (ns.NewCharacterList)
        SendCharacterListNew(ns, ns.Account, info);
      else
        SendCharacterListOld(ns, ns.Account, info);
    }

    public static void SendCharacterListNew(NetState ns, IAccount a, CityInfo[] info)
    {
      if (ns == null)
        return;

      int highSlot = -1;

      for (int i = 0; i < a.Length; ++i)
        if (a[i] != null)
          highSlot = i;

      int count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);
      int length = 11 + count * 60 + info.Length * 89;

      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0xA9); // Packet ID
      w.Write((short)length); // Length

      w.Write((byte)count);

      for (int i = 0; i < count; ++i)
        if (a[i] != null)
        {
          w.WriteAsciiFixed(a[i].Name, 30);
          w.Position += 30;
        }
        else
          w.Position += 60;

      w.Write((byte)info.Length);

      for (int i = 0; i < info.Length; ++i)
      {
        CityInfo ci = info[i];

        w.Write((byte)i);
        w.WriteAsciiFixed(ci.City, 32);
        w.WriteAsciiFixed(ci.Building, 32);
        w.Write(ci.X);
        w.Write(ci.Y);
        w.Write(ci.Z);
        w.Write(ci.Map.MapID);
        w.Write(ci.Description);
        w.Position += 4; // w.Write(0);
      }

      CharacterListFlags flags = ExpansionInfo.CoreExpansion.CharacterListFlags;

      if (count > 6)
        flags |= CharacterListFlags.SeventhCharacterSlot |
                 CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
      else if (count == 6)
        flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
      else if (a.Limit == 1)
        flags |= CharacterListFlags.SlotLimit &
                 CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

      w.Write((int)flags);

      w.Write((short)-1);

      ns.Send(w.RawSpan);

      // TODO: Razor support?
    }

    public static void SendCharacterListOld(NetState ns, IAccount a, CityInfo[] info)
    {
      if (ns == null)
        return;

      int highSlot = -1;

      for (int i = 0; i < a.Length; ++i)
        if (a[i] != null)
          highSlot = i;

      int count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);
      int length = 9 + count * 60 + info.Length * 63;

      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0xA9); // Packet ID
      w.Write((short)length); // Length

      w.Write((byte)count);

      for (int i = 0; i < count; ++i)
        if (a[i] != null)
        {
          w.WriteAsciiFixed(a[i].Name, 30);
          w.Position += 30;
        }
        else
          w.Position += 60;

      w.Write((byte)info.Length);

      for (int i = 0; i < info.Length; ++i)
      {
        CityInfo ci = info[i];

        w.Write((byte)i);
        w.WriteAsciiFixed(ci.City, 31);
        w.WriteAsciiFixed(ci.Building, 31);
      }

      CharacterListFlags flags = ExpansionInfo.CoreExpansion.CharacterListFlags;

      if (count > 6)
        flags |= CharacterListFlags.SeventhCharacterSlot |
                 CharacterListFlags.SixthCharacterSlot; // 7th Character Slot - TODO: Is SixthCharacterSlot Required?
      else if (count == 6)
        flags |= CharacterListFlags.SixthCharacterSlot; // 6th Character Slot
      else if (a.Limit == 1)
        flags |= CharacterListFlags.SlotLimit &
                 CharacterListFlags.OneCharacterSlot; // Limit Characters & One Character

      w.Write((int)flags);

      ns.Send(w.RawSpan);

      // TODO: Razor support?
    }

    public static void SendCharacterListUpdate(NetState ns, IAccount a)
    {
      if (ns == null)
        return;

      int highSlot = -1;

      for (int i = 0; i < a.Length; ++i)
        if (a[i] != null)
          highSlot = i;

      int count = Math.Max(Math.Max(highSlot + 1, a.Limit), 5);

      int length = 4 + count * 60;

      SpanWriter w = new SpanWriter(stackalloc byte[length]);
      w.Write((byte)0x86); // Packet ID
      w.Write((short)length); // Length

      w.Write((byte)count);

      for (int i = 0; i < count; ++i)
      {
        Mobile m = a[i];

        if (m != null)
        {
          w.WriteAsciiFixed(m.Name, 30);
          w.Position += 30;
        }
        else
          w.Position += 60;
      }

      ns.Send(w.RawSpan);
    }

    public static void SendPlayServerAck(NetState ns, ServerInfo si)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(11));
      w.Write((byte)0x8C); // Packet ID

      int addr = Utility.GetAddressValue(si.Address.Address);

      w.Write((byte)addr);
      w.Write((byte)(addr >> 8));
      w.Write((byte)(addr >> 16));
      w.Write((byte)(addr >> 24));

      w.Write((short)si.Address.Port);
      w.Write(ns.m_AuthID);

      // Not compressed with Huffman
      _ = ns.Flush(11);
    }

    public static void SendAccountLoginAck(NetState ns, ServerInfo[] info)
    {
      if (ns == null)
        return;

      int length = 6 + info.Length * 40;

      SpanWriter w = new SpanWriter(ns.SendPipe.Writer.GetSpan(length));
      w.Write((byte)0xA8); // Packet ID
      w.Write((short)length);

      w.Write((byte)0x5D); // Unknown

      w.Write((ushort)info.Length);

      for (int i = 0; i < info.Length; ++i)
      {
        ServerInfo si = info[i];

        w.Write((ushort)i);
        w.WriteAsciiFixed(si.Name, 32);
        w.Write((byte)si.FullPercent);
        w.Write((sbyte)si.TimeZone);
        w.Write(Utility.GetAddressValue(si.Address.Address));
      }

      // Not compressed with Huffman
      _ = ns.Flush(length);
    }
  }
}
