using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Logging;
using Server.Targeting;

namespace Server.Mobiles;

/// <summary>
/// Staff diagnostic for AI chase/tracking behavior. `[ChaseDebug` targets a creature to
/// toggle tracking (also flips its overhead DebugSay); `[ChaseDebug clear` untracks all.
/// Tracked creatures emit console log lines from the AI hot spots (action transitions,
/// combatant changes, acquire attempts, movement/pathfinding decisions) so a chase can be
/// reconstructed tick by tick. Zero cost while nothing is tracked.
/// </summary>
public static class ChaseDebug
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ChaseDebug));

    private static readonly HashSet<BaseCreature> _tracked = [];

    public static void Configure()
    {
        CommandSystem.Register("ChaseDebug", AccessLevel.GameMaster, ChaseDebug_OnCommand);
    }

    public static bool Tracks(BaseCreature bc) => _tracked.Count > 0 && bc != null && _tracked.Contains(bc);

    // DIAG: mirrored to a shared folder so client and server traces can be aligned by UTC time.
    private static readonly System.IO.StreamWriter _file = OpenFile();

    private static System.IO.StreamWriter OpenFile()
    {
        try
        {
            const string path = @"C:\Repositories\ModernUO\docs\chase-logs\server-chase.log";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            return new System.IO.StreamWriter(path, false) { AutoFlush = true };
        }
        catch
        {
            return null;
        }
    }

    public static void Log(BaseCreature bc, string message)
    {
        if (bc.Deleted)
        {
            _tracked.Remove(bc);
            return;
        }

        logger.Information("[{Serial}] {Name} @{Location} | {Message}", bc.Serial, bc.RawName, bc.Location, message);
        _file?.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff}\t{Core.TickCount}\t{bc.Serial}\t{bc.Location}\t{message}");
    }

    public static string Describe(Mobile m) =>
        m == null ? "null" : $"{m.RawName} ({m.Serial})";

    /// <summary>Standard target context: distance, LOS, and perception range.</summary>
    public static string TargetInfo(BaseCreature bc, Mobile target) =>
        target == null
            ? "target=null"
            : $"target={Describe(target)} dist={bc.GetDistanceToSqrt(target):F1} los={bc.InLOS(target)} cansee={bc.CanSee(target)}";

    /// <summary>Resolved clocks: seconds per decision vs seconds per step.</summary>
    public static string Clocks(BaseCreature bc) =>
        $"think={bc.CurrentSpeed:F2}s ({bc.ActiveSpeed:F2}/{bc.PassiveSpeed:F2}) " +
        $"move={bc.CurrentMoveSpeed:F2}s ({bc.ActiveMoveSpeed:F2}/{bc.PassiveMoveSpeed:F2})";

    [Usage("ChaseDebug [clear]")]
    [Description("Toggles AI chase tracing on a targeted creature; 'clear' stops all tracing.")]
    private static void ChaseDebug_OnCommand(CommandEventArgs e)
    {
        if (e.GetString(0).InsensitiveEquals("clear"))
        {
            foreach (var bc in _tracked)
            {
                bc.Debug = false;
            }

            _tracked.Clear();
            e.Mobile.SendMessage("Chase tracing stopped for all creatures.");
            return;
        }

        e.Mobile.Target = new ChaseDebugTarget();
        e.Mobile.SendMessage("Target a creature to toggle chase tracing.");
    }

    private static void Toggle(Mobile from, BaseCreature bc)
    {
        if (_tracked.Remove(bc))
        {
            bc.Debug = false;
            from.SendMessage($"No longer tracing {bc.RawName} ({bc.Serial}).");
            Log(bc, "tracing stopped");
            return;
        }

        _tracked.Add(bc);
        bc.Debug = true;
        from.SendMessage($"Now tracing {bc.RawName} ({bc.Serial}) — output in the server console.");
        Log(bc, $"tracing started | ai={bc.AI} action={bc.AIObject?.Action.ToString() ?? "none"} " +
                $"{TargetInfo(bc, bc.Combatant)} perception={bc.RangePerception} fight={bc.RangeFight} {Clocks(bc)}");
    }

    private class ChaseDebugTarget : Target
    {
        public ChaseDebugTarget() : base(-1, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is BaseCreature bc)
            {
                Toggle(from, bc);
            }
            else
            {
                from.SendMessage("That is not a creature.");
            }
        }
    }
}
