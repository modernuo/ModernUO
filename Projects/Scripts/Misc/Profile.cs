using System;
using Server.Accounting;
using Server.Network;

namespace Server.Misc
{
  public static class Profile
  {
    public static void Initialize()
    {
      EventSink.ProfileRequest += EventSink_ProfileRequest;
      EventSink.ChangeProfileRequest += EventSink_ChangeProfileRequest;
    }

    public static void EventSink_ChangeProfileRequest(ChangeProfileRequestEventArgs e)
    {
      Mobile from = e.Beholder;

      if (from.ProfileLocked)
        from.SendMessage("Your profile is locked. You may not change it.");
      else
        from.Profile = e.Text;
    }

    public static void EventSink_ProfileRequest(ProfileRequestEventArgs e)
    {
      Mobile beholder = e.Beholder;
      Mobile beheld = e.Beheld;

      if (!beheld.Player)
        return;

      if (beholder.Map != beheld.Map || !beholder.InRange(beheld, 12) || !beholder.CanSee(beheld))
        return;

      string header = Titles.ComputeTitle(beholder, beheld);

      string footer = "";

      if (beheld.ProfileLocked)
      {
        if (beholder == beheld)
          footer = "Your profile has been locked.";
        else if (beholder.AccessLevel >= AccessLevel.Counselor)
          footer = "This profile has been locked.";
      }

      if (footer.Length == 0 && beholder == beheld)
        footer = GetAccountDuration(beheld);

      string body = beheld.Profile ?? "";

      Packets.SendDisplayProfile(beholder.NetState, beholder != beheld || !beheld.ProfileLocked ? beheld.Serial : Serial.Zero, header, body, footer);
    }

    private static string GetAccountDuration(Mobile m)
    {
      if (!(m.Account is Account a))
        return "";

      TimeSpan ts = DateTime.UtcNow - a.Created;

      if (Format(ts.TotalDays, "This account is {0} day{1} old.", out string v))
        return v;

      if (Format(ts.TotalHours, "This account is {0} hour{1} old.", out v))
        return v;

      if (Format(ts.TotalMinutes, "This account is {0} minute{1} old.", out v))
        return v;

      return Format(ts.TotalSeconds, "This account is {0} second{1} old.", out v) ? v : "";
    }

    public static bool Format(double value, string format, out string op)
    {
      if (value >= 1.0)
      {
        op = string.Format(format, (int)value, (int)value != 1 ? "s" : "");
        return true;
      }

      op = null;
      return false;
    }
  }
}
