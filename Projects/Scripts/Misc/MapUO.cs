using System;
using Server.Buffers;
using Server.Engines.PartySystem;
using Server.Guilds;
using Server.Network;

namespace Server.Misc
{
  public static class MapUO
  {
    public static void Initialize()
    {
      if (Settings.PartyTrack)
        ProtocolExtensions.Register(0x00, true, OnPartyTrack);

      if (Settings.GuildTrack)
        ProtocolExtensions.Register(0x01, true, OnGuildTrack);
    }

    private static void OnPartyTrack(NetState state, PacketReader pvSrc)
    {
      MapUOPackets.SendPartyTrack(state, Party.Get(state.Mobile));
    }

    private static void OnGuildTrack(NetState state, PacketReader pvSrc)
    {
      MapUOPackets.SendGuildTrack(state, state.Mobile.Guild as Guild, pvSrc.ReadBoolean());
    }

    private static class Settings
    {
      public const bool PartyTrack = true;
      public const bool GuildTrack = true;
      public const bool GuildHitsPercent = true;
    }

    private static class MapUOPackets
    {
      public static void SendPartyTrack(NetState ns, Party party)
      {
        Mobile from = ns.Mobile;
        int count = party?.Members.Count ?? 0;
        if (count < 2)
          return;

        SpanWriter writer = new SpanWriter(stackalloc byte[Math.Max(count - 1, 0) * 9 + 8]);
        writer.Write((byte)0xF0); // Packet ID
        writer.Position += 2; // Dynamic Length

        writer.Write((byte)0x01); // Command

        count = 0;
        for (int i = 0; i < count; ++i)
        {
          Mobile mob = party.Members[i]?.Mobile; // if count is greater than 0, then party is not null

          if (mob == from || mob?.NetState == null || Utility.InUpdateRange(from, mob) && from.CanSee(mob))
            continue;

          count++;
          writer.Write(mob.Serial);
          writer.Write((short)mob.X);
          writer.Write((short)mob.Y);
          writer.Write((byte)(mob.Map?.MapID ?? 0));
        }

        if (count == 0)
          return;

        writer.Position += 4; // Empty Serial
        writer.Position = 1;
        writer.Write((ushort)writer.WrittenCount);

        ns.Send(writer.Span);
      }

      public static void SendGuildTrack(NetState ns, Guild guild = null, bool locations = false)
      {
        Mobile from = ns.Mobile;

        int count = guild?.Members.Count ?? 0;

        if (count < 2)
          return;

        SpanWriter writer = new SpanWriter(stackalloc byte[Math.Max(count - 1, 0) * (locations ? 10 : 4) + 9]);
        writer.Write((byte)0xF0); // Packet ID
        writer.Position += 2; // Dynamic Length

        writer.Write((byte)0x02); // Command

        writer.Write(locations);

        count = 0;
        for (int i = 0; i < count; ++i)
        {
          Mobile mob = guild.Members[i]; // If guild count is above 0, then guild is not null.

          if (mob == from || mob?.NetState == null || locations && Utility.InUpdateRange(from, mob) && from.CanSee(mob))
            continue;

          count++;
          writer.Write(mob.Serial);

          if (locations)
          {
            writer.Write((short)mob.X);
            writer.Write((short)mob.Y);
            writer.Write((byte)(mob.Map?.MapID ?? 0));

            if (Settings.GuildHitsPercent && mob.Alive)
              writer.Write((byte)(mob.Hits / Math.Max(mob.HitsMax, 1.0) * 100));
            else
              writer.Position++; // writer.Write((byte)0);
          }
        }

        if (count == 0)
          return;

        writer.Position += 4; // Empty Serial

        writer.Position = 1;
        writer.Write((ushort)writer.WrittenCount);

        ns.Send(writer.Span);
      }
    }
  }
}
