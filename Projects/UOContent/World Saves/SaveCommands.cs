using Server.Saves;

namespace Server.Commands;

public static class SaveCommands
{
    public static void Configure()
    {
        CommandSystem.Register("Save", AccessLevel.Administrator, Save_OnCommand);
        CommandSystem.Register("SetSaves", AccessLevel.Administrator, SetAutoSaves_OnCommand);
        CommandSystem.Register("AutoSave", AccessLevel.Administrator, SetAutoSaves_OnCommand);
        CommandSystem.Register("SaveFrequency", AccessLevel.Administrator, SetSaveFrequency_OnCommand);
        CommandSystem.Register("PruneArchives", AccessLevel.Administrator, PruneArchives_OnCommand);
        CommandSystem.Register("ArchiveStatus", AccessLevel.Administrator, ArchiveStatus_OnCommand);
        CommandSystem.Register("ArchiveNow", AccessLevel.Administrator, ArchiveNow_OnCommand);
    }

    [Usage("PruneArchives"), Description("Prunes archives folder.")]
    private static void PruneArchives_OnCommand(CommandEventArgs e)
    {
        AutoArchive.PruneBackups();
    }

    [Usage("Save"), Description("Saves the world.")]
    private static void Save_OnCommand(CommandEventArgs e)
    {
        AutoSave.Save();
    }

    [Usage("AutoSave <on | off>"), Description("Enables or disables automatic shard saving.")]
    public static void SetAutoSaves_OnCommand(CommandEventArgs e)
    {
        if (e.Length == 1)
        {

            var enabled = AutoSave.SavesEnabled = e.GetBoolean(0);

            if (enabled)
            {
                e.Mobile.SendMessage("Saves have been enabled.");
            }
            else
            {
                e.Mobile.SendMessage("Saves have been disabled.");
            }
        }
        else
        {
            e.Mobile.SendMessage("Format: AutoSave <on | off>");
        }
    }

    [Usage("SaveFrequency <duration> [warning duration]"), Description("Sets the save frequency starting at midnight local to the server.")]
    public static void SetSaveFrequency_OnCommand(CommandEventArgs e)
    {
        if (e.Length < 1)
        {
            e.Mobile.SendMessage("Format: SaveFrequency <duration> [warning duration]");
            return;
        }

        var saveDelay = e.GetTimeSpan(0);
        var warningDelay = e.Length >= 2 ? e.GetTimeSpan(1) : AutoSave.Warning;

        AutoSave.ResetAutoSave(saveDelay, warningDelay);

        e.Mobile.SendMessage("Save frequency has been updated.");
    }

    [Usage("ArchiveStatus"), Description("Shows archive system status, journal state, and registered destinations.")]
    private static void ArchiveStatus_OnCommand(CommandEventArgs e)
    {
        var mobile = e.Mobile;

        mobile.SendMessage("--- Archive Status ---");
        mobile.SendMessage($"Next hourly: {AutoArchive.NextHourlyArchive:yyyy-MM-dd HH:mm}");
        mobile.SendMessage($"Next daily: {AutoArchive.NextDailyArchive:yyyy-MM-dd}");
        mobile.SendMessage($"Next monthly: {AutoArchive.NextMonthlyArchive:yyyy-MM}");

        var destinations = ArchiveDestinationRegistry.Destinations;
        mobile.SendMessage($"Registered destinations: {destinations.Count}");
        foreach (var dest in destinations)
        {
            mobile.SendMessage(
                $"  - {dest.Name} (retention: {dest.GetRetentionCount(ArchivePeriod.Hourly)}h/" +
                $"{dest.GetRetentionCount(ArchivePeriod.Daily)}d/" +
                $"{dest.GetRetentionCount(ArchivePeriod.Monthly)}m)"
            );
        }

        var operations = ArchiveJournal.Operations;
        var pending = 0;
        var failed = 0;
        foreach (var op in operations)
        {
            if (op.State is ArchiveOperationState.Started or ArchiveOperationState.Archived or ArchiveOperationState.Distributed)
            {
                pending++;
            }
            else if (op.State == ArchiveOperationState.Failed)
            {
                failed++;
            }
        }

        mobile.SendMessage($"Journal entries: {operations.Count} ({pending} pending, {failed} failed)");
    }

    [Usage("ArchiveNow"), Description("Forces an immediate archive rollup regardless of schedule.")]
    private static void ArchiveNow_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage("Forcing immediate archive rollup...");
        AutoArchive.ForceRollup();
    }
}
