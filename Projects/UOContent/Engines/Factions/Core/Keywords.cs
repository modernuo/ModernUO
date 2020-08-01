using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Factions
{
  public static class Keywords
  {
    public static void Initialize()
    {
      EventSink.Speech += EventSink_Speech;
    }

    private static void ShowScore_Sandbox(PlayerState pl)
    {
      pl?.Mobile.PublicOverheadMessage(MessageType.Regular, pl.Mobile.SpeechHue, true,
        pl.KillPoints.ToString("N0")); // NOTE: Added 'N0'
    }

    private static void EventSink_Speech(SpeechEventArgs e)
    {
      Mobile from = e.Mobile;
      int[] keywords = e.Keywords;

      for (int i = 0; i < keywords.Length; ++i)
        switch (keywords[i])
        {
          case 0x00E4: // *i wish to access the city treasury*
            {
              Town town = Town.FromRegion(from.Region);

              if (town?.IsFinance(from) != true || !from.Alive)
                break;

              if (FactionGump.Exists(from))
                from.SendLocalizedMessage(1042160); // You already have a faction menu open.
              else if (town.Owner != null && from is PlayerMobile mobile)
                mobile.SendGump(new FinanceGump(mobile, town.Owner, town));

              break;
            }
          case 0x0ED: // *i am sheriff*
            {
              Town town = Town.FromRegion(from.Region);

              if (town?.IsSheriff(from) != true || !from.Alive)
                break;

              if (FactionGump.Exists(from))
                from.SendLocalizedMessage(1042160); // You already have a faction menu open.
              else if (town.Owner != null)
                from.SendGump(new SheriffGump((PlayerMobile)from, town.Owner, town));

              break;
            }
          case 0x00EF: // *you are fired*
            {
              Town town = Town.FromRegion(from.Region);

              if (town == null)
                break;

              if (town.IsFinance(from) || town.IsSheriff(from))
                town.BeginOrderFiring(from);

              break;
            }
          case 0x00E5: // *i wish to resign as finance minister*
            {
              PlayerState pl = PlayerState.Find(from);

              if (pl?.Finance != null)
              {
                pl.Finance.Finance = null;
                from.SendLocalizedMessage(1005081); // You have been fired as Finance Minister
              }

              break;
            }
          case 0x00EE: // *i wish to resign as sheriff*
            {
              PlayerState pl = PlayerState.Find(from);

              if (pl?.Sheriff != null)
              {
                pl.Sheriff.Sheriff = null;
                from.SendLocalizedMessage(1010270); // You have been fired as Sheriff
              }

              break;
            }
          case 0x00E9: // *what is my faction term status*
            {
              PlayerState pl = PlayerState.Find(from);

              if (pl?.IsLeaving == true)
              {
                if (Faction.CheckLeaveTimer(from))
                  break;

                TimeSpan remaining = pl.Leaving + Faction.LeavePeriod - DateTime.UtcNow;

                if (remaining.TotalDays >= 1)
                  from.SendLocalizedMessage(1042743,
                    remaining.TotalDays
                      .ToString("N0")); // Your term of service will come to an end in ~1_DAYS~ days.
                else if (remaining.TotalHours >= 1)
                  from.SendLocalizedMessage(1042741,
                    remaining.TotalHours
                      .ToString("N0")); // Your term of service will come to an end in ~1_HOURS~ hours.
                else
                  from.SendLocalizedMessage(
                    1042742); // Your term of service will come to an end in less than one hour.
              }
              else if (pl != null)
              {
                from.SendLocalizedMessage(1042233); // You are not in the process of quitting the faction.
              }

              break;
            }
          case 0x00EA: // *message faction*
            {
              Faction faction = Faction.Find(from);

              if (faction?.IsCommander(from) != true)
                break;

              if (from.AccessLevel == AccessLevel.Player && !faction.FactionMessageReady)
                from.SendLocalizedMessage(
                  1010264); // The required time has not yet passed since the last message was sent
              else
                faction.BeginBroadcast(from);

              break;
            }
          case 0x00EC: // *showscore*
            {
              PlayerState pl = PlayerState.Find(from);

              if (pl != null)
                Timer.DelayCall(ShowScore_Sandbox, pl);

              break;
            }
          case 0x0178: // i honor your leadership
            {
              Faction.Find(from)?.BeginHonorLeadership(from);
              break;
            }
        }
    }
  }
}
