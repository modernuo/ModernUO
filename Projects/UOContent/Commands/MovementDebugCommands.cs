using Server.Commands;
using Server.Network;
using Server.Targeting;

namespace Server.Commands;

public static class MovementDebugCommands
{
    public static void Configure()
    {
        CommandSystem.Register("MovementDebug", AccessLevel.GameMaster, MovementDebug_OnCommand);
        CommandSystem.Register("MovementStats", AccessLevel.GameMaster, MovementStats_OnCommand);

        // Subscribe to speed hack detection events and broadcast to online staff
        MovementThrottle.OnSpeedHackDetected += OnSpeedHackDetected;
    }

    private static void OnSpeedHackDetected(
        NetState ns,
        Mobile mobile,
        float rate,
        MovementThrottle.DetectionVerdict verdict,
        string urgency)
    {
        if (mobile == null)
        {
            return;
        }

        var hue = urgency switch
        {
            "HIGH"   => 0x25, // Red
            "MEDIUM" => 0x35, // Orange
            _        => 0x3B  // Yellow
        };

        CommandHandlers.BroadcastMessage(
            AccessLevel.Counselor,
            hue,
            $"[{urgency}] Speed hack: {mobile.RawName} ({ns.Account?.Username}) " +
            $"Rate:{rate:F2} Verdict:{verdict} @ {mobile.Location} {mobile.Map?.Name}"
        );
    }

    [Usage("MovementDebug")]
    [Description("Target a player to toggle verbose movement logging for their connection.")]
    private static void MovementDebug_OnCommand(CommandEventArgs e)
    {
        e.Mobile.BeginTarget(-1, false, TargetFlags.None, MovementDebug_OnTarget);
        e.Mobile.SendMessage("Target a player to toggle movement debug logging.");
    }

    private static void MovementDebug_OnTarget(Mobile from, object obj)
    {
        if (obj is not Mobile target || target.NetState == null)
        {
            from.SendMessage("That is not a valid online player.");
            return;
        }

        var ns = target.NetState;
        ns.MovementLogging = !ns.MovementLogging;

        var state = ns.MovementLogging ? "ENABLED" : "DISABLED";
        from.SendMessage($"Movement debug logging {state} for {target.RawName}.");
    }

    [Usage("MovementStats")]
    [Description("Target a player to see their current movement throttle state.")]
    private static void MovementStats_OnCommand(CommandEventArgs e)
    {
        e.Mobile.BeginTarget(-1, false, TargetFlags.None, MovementStats_OnTarget);
        e.Mobile.SendMessage("Target a player to view their movement stats.");
    }

    private static void MovementStats_OnTarget(Mobile from, object obj)
    {
        if (obj is not Mobile target || target.NetState == null)
        {
            from.SendMessage("That is not a valid online player.");
            return;
        }

        var stats = MovementThrottle.GetMovementStats(target.NetState);

        from.SendMessage($"--- Movement Stats for {target.RawName} ---");
        from.SendMessage($"Rate: {stats.Rate:F3} ({stats.SampleCount} samples) | Verdict: {stats.Verdict} | Confidence: {stats.Confidence:P0}");
        from.SendMessage($"RTT: avg={stats.AverageRtt}ms last={stats.LastRtt}ms var={stats.RttVariance} stable={stats.StableConnection} samples={stats.RttSampleCount}");
        from.SendMessage($"Queue: depth={stats.QueueDepth} | Credit: {stats.MovementCredit}ms");
        from.SendMessage($"Packet rate: {stats.CurrentPacketRate}/s (peak: {stats.PeakPacketRate}/s)");
        from.SendMessage($"Burst: size={stats.BurstSize} gap={stats.PrecedingGap}ms | Sustained: {stats.SustainedSeconds}s");
        from.SendMessage($"Debug logging: {(stats.DebugLogging ? "ON" : "OFF")}");
    }
}
