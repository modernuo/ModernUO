using System;
using Server.Saves;

namespace Server.Commands;

public static class DeltaSaveCommands
{
    public static void Configure()
    {
        CommandSystem.Register("DeltaSave", AccessLevel.Administrator, DeltaSave_OnCommand);
    }

    [Usage("DeltaSave [mode <off|verify|on> | full | report | denylist | trust <type> | deny <type> | sample <rate>]")]
    [Description("Shows or changes how world saves skip unchanged entities. With no arguments, shows the status.")]
    private static void DeltaSave_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Length == 0)
        {
            ShowStatus(from);
            return;
        }

        switch (e.GetString(0).ToLowerInvariant())
        {
            case "mode":
                {
                    if (e.Length < 2 || !Enum.TryParse<DeltaSaveMode>(e.GetString(1), true, out var mode))
                    {
                        from.SendMessage("Format: DeltaSave mode <off|verify|on>");
                        return;
                    }

                    DeltaSaves.SetMode(mode);
                    from.SendMessage($"Delta save mode is now {mode}.");
                    return;
                }
            case "full":
                {
                    DeltaSaves.RequestFullSave($"requested by {from.RawName}");
                    from.SendMessage("The next save will serialize every entity.");
                    return;
                }
            case "report":
                {
                    ShowReport(from);
                    return;
                }
            case "denylist":
                {
                    ShowDenylist(from);
                    return;
                }
            case "trust":
                {
                    var type = ResolveType(e.GetString(1));
                    if (type == null)
                    {
                        from.SendMessage("Format: DeltaSave trust <type>");
                        return;
                    }

                    from.SendMessage(
                        DeltaSaves.Trust(type.FullName)
                            ? $"{type.FullName} may be copied again on delta saves."
                            : $"{type.FullName} was not denylisted."
                    );
                    return;
                }
            case "deny":
                {
                    var type = ResolveType(e.GetString(1));
                    if (type == null)
                    {
                        from.SendMessage("Format: DeltaSave deny <type>");
                        return;
                    }

                    from.SendMessage(
                        DeltaSaves.Deny(type)
                            ? $"{type.FullName} will serialize on every save."
                            : $"{type.FullName} was already denylisted."
                    );
                    return;
                }
            case "sample":
                {
                    if (e.Length < 2)
                    {
                        from.SendMessage("Format: DeltaSave sample <rate between 0 and 1>");
                        return;
                    }

                    DeltaSaves.SetSampleRate(e.GetDouble(1));
                    from.SendMessage($"Delta saves now re-serialize {DeltaSaves.SampleRate:P1} of clean entities for verification.");
                    return;
                }
            default:
                {
                    from.SendMessage("Format: DeltaSave [mode <off|verify|on> | full | report | denylist | trust <type> | deny <type> | sample <rate>]");
                    return;
                }
        }
    }

    private static Type ResolveType(string name) =>
        string.IsNullOrWhiteSpace(name) ? null : AssemblyHandler.FindTypeByFullName(name) ?? AssemblyHandler.FindTypeByName(name);

    private static void ShowStatus(Mobile from)
    {
        from.SendMessage("--- Delta Saves ---");
        from.SendMessage($"Mode: {DeltaSaves.Mode}, sample rate {DeltaSaves.SampleRate:P1}, full save every {DeltaSaves.FullSaveEvery} saves");
        from.SendMessage($"Saves since boot: {DeltaSaves.SaveCount}; next save {(DeltaSaves.ForceFullNext ? $"full ({DeltaSaves.ForceFullReason})" : "delta if the mode allows")}");
        from.SendMessage($"Denylisted types: {DeltaSaves.Denylist.Count}");

        foreach (var persistence in Persistence.EntityPersistences)
        {
            from.SendMessage($"  {persistence.Name}: {persistence.EntityCount} entities, {persistence.TrustedTypeCount}/{persistence.TypeCount} types trusted");
        }

        var report = DeltaSaves.LastReport;
        if (report == null)
        {
            from.SendMessage("No save has completed since boot.");
            return;
        }

        from.SendMessage($"Last save: {report.Serialized} serialized ({report.SerializedBytes} bytes), {report.Copied} copied ({report.CopiedBytes} bytes), {report.Verified} verified, {report.Skipped} skipped, freeze {report.FreezeDuration.TotalSeconds:F2}s");

        if (report.HasViolations)
        {
            from.SendMessage($"Last save found {report.ViolationCounts.Count} type(s) with missed dirty tracking; see DeltaSave report.");
        }
    }

    private static void ShowReport(Mobile from)
    {
        var violations = DeltaSaves.ViolationsSinceBoot;

        if (violations.Count == 0)
        {
            from.SendMessage("No verification violations since boot.");
            return;
        }

        from.SendMessage($"--- Missed dirty tracking since boot ({violations.Count} types) ---");

        var last = DeltaSaves.LastReport;
        foreach (var (type, count) in violations)
        {
            var serials = last?.ViolationSerials(type);
            from.SendMessage(
                serials?.Count > 0
                    ? $"{type.FullName}: {count} (last save e.g. {string.Join(", ", serials)})"
                    : $"{type.FullName}: {count}"
            );
        }
    }

    private static void ShowDenylist(Mobile from)
    {
        var denylist = DeltaSaves.Denylist;

        if (denylist.Count == 0)
        {
            from.SendMessage("No types are denylisted.");
            return;
        }

        from.SendMessage($"--- Denylisted types ({denylist.Count}) ---");
        foreach (var name in denylist)
        {
            from.SendMessage(name);
        }
    }
}
