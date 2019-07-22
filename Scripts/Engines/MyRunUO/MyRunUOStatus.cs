using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Network;

namespace Server.Engines.MyRunUO
{
  public class MyRunUOStatus
  {
    private static DatabaseCommandQueue m_Command;

    public static void Initialize()
    {
      if (Config.Enabled)
      {
        Timer.DelayCall(TimeSpan.FromSeconds(20.0), Config.StatusUpdateInterval, Begin);

        CommandSystem.Register("UpdateWebStatus", AccessLevel.Administrator, UpdateWebStatus_OnCommand);
      }
    }

    [Usage("UpdateWebStatus")]
    [Description("Starts the process of updating the MyRunUO online status database.")]
    public static void UpdateWebStatus_OnCommand(CommandEventArgs e)
    {
      if (m_Command == null || m_Command.HasCompleted)
      {
        Begin();
        e.Mobile.SendMessage("Web status update process has been started.");
      }
      else
      {
        e.Mobile.SendMessage("Web status database is already being updated.");
      }
    }

    public static void Begin()
    {
      if (m_Command?.HasCompleted == false)
        return;

      Console.WriteLine("MyRunUO: Updating status database");

      try
      {
        m_Command = new DatabaseCommandQueue("MyRunUO: Status database updated in {0:F1} seconds",
          "MyRunUO Status Database Thread");

        m_Command.Enqueue("DELETE FROM myrunuo_status");

        List<NetState> online = NetState.Instances;

        for (int i = 0; i < online.Count; ++i)
        {
          NetState ns = online[i];
          Mobile mob = ns.Mobile;

          if (mob != null)
            m_Command.Enqueue(
              $"INSERT INTO myrunuo_status (char_id) VALUES ({mob.Serial.Value.ToString()})");
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("MyRunUO: Error updating status database");
        Console.WriteLine(e);
      }

      m_Command?.Enqueue(null);
    }
  }
}
