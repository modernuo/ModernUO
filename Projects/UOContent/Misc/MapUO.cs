using System;
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
      Mobile from = state.Mobile;
      Party party = Party.Get(from);

      if (party != null)
      {
        Packets.PartyTrack packet = new Packets.PartyTrack(from, party);

        if (packet.Stream.Length > 8)
          state.Send(packet);
      }
    }

    private static void OnGuildTrack(NetState state, PacketReader pvSrc)
    {
      Mobile from = state.Mobile;

      if (from.Guild is Guild guild)
      {
        bool locations = pvSrc.ReadByte() != 0;

        Packets.GuildTrack packet = new Packets.GuildTrack(from, guild, locations);

        if (packet.Stream.Length > (locations ? 9 : 5))
          state.Send(packet);
      }
      else
      {
        state.Send(new Packets.GuildTrack());
      }
    }

    private static class Settings
    {
      public const bool PartyTrack = true;
      public const bool GuildTrack = true;
      public const bool GuildHitsPercent = true;
    }

    private static class Packets
    {
      public sealed class PartyTrack : ProtocolExtension
      {
        public PartyTrack(Mobile from, Party party) : base(0x01, (party.Members.Count - 1) * 9 + 4)
        {
          for (int i = 0; i < party.Members.Count; ++i)
          {
            PartyMemberInfo pmi = party.Members[i];

            if (pmi == null || pmi.Mobile == from)
              continue;

            Mobile mob = pmi.Mobile;

            if (Utility.InUpdateRange(from, mob) && from.CanSee(mob))
              continue;

            Stream.Write(mob.Serial);
            Stream.Write((short)mob.X);
            Stream.Write((short)mob.Y);
            Stream.Write((byte)(mob.Map?.MapID ?? 0));
          }

          Stream.Write(0);
        }
      }

      public sealed class GuildTrack : ProtocolExtension
      {
        public GuildTrack() : base(0x02, 5)
        {
          Stream.Write((byte)0);
          Stream.Write(0);
        }

        public GuildTrack(Mobile from, Guild guild, bool locations) : base(0x02,
          (guild.Members.Count - 1) * (locations ? 10 : 4) + 5)
        {
          Stream.Write((byte)(locations ? 1 : 0));

          for (int i = 0; i < guild.Members.Count; ++i)
          {
            Mobile mob = guild.Members[i];

            if (mob == null || mob == from || mob.NetState == null)
              continue;

            if (locations && Utility.InUpdateRange(from, mob) && from.CanSee(mob))
              continue;

            Stream.Write(mob.Serial);

            if (locations)
            {
              Stream.Write((short)mob.X);
              Stream.Write((short)mob.Y);
              Stream.Write((byte)(mob.Map?.MapID ?? 0));

              if (Settings.GuildHitsPercent && mob.Alive)
                Stream.Write((byte)(mob.Hits / Math.Max(mob.HitsMax, 1.0) * 100));
              else
                Stream.Write((byte)0);
            }
          }

          Stream.Write(0);
        }
      }
    }
  }
}