using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>What the thinking layer says about itself, for the page. Filled in by the minds assembly, which this one may not reference.</summary>
public sealed record BotMindsReport(
    string Model,
    string Endpoint,
    int Awake,
    bool Reachable,
    long Asked,
    long Answered,
    long Refused,
    long LastMs
);

/// <summary>One line a debugger wrote into its own log, as it wrote it, for the live window's feed.</summary>
public sealed record BotDebuggerLine(DateTime At, string Text);

/// <summary>
/// One watcher of the debugger squad as the live window shows it (02.10.2026): where it stands and what it is doing there,
/// what it last thought and what the model it asked answered, what it is waiting on, and the last lines it wrote. Every string
/// is one the debugger's own log already prints, built by the minds assembly on the game loop.
/// </summary>
public sealed record BotDebuggerReport(
    string Name,
    int Hue,
    string Charge,
    bool Organiser,
    string Map,
    int X,
    int Y,
    bool Shown,
    string Doing,
    string DoingWhat,
    string Subject,
    DateTime? ThoughtAt,
    string ThoughtKind,
    string Question,
    string Thought,
    string Answer,
    string About,
    double Sure,
    string Asking,
    DateTime? AskingSince,
    long Asked,
    long Findings,
    long Quiet,
    long Reflections,
    long Labels,
    long Echoes,
    long Hops,
    IReadOnlyList<BotDebuggerLine> Feed
);

/// <summary>
/// Where the assemblies that depend on this one hang what the dashboard shows about them.
///
/// The same shape as <see cref="BotCrier"/>: a function that is null until somebody sets it, so an absent
/// thinking layer reads as absent rather than as a row of zeros.
/// </summary>
public static class BotWebHooks
{
    public static Func<BotMindsReport> Minds { get; set; }

    /// <summary>The debugger squad, one report a watcher, asked on the game loop once a second while the live window is open.</summary>
    public static Func<IReadOnlyList<BotDebuggerReport>> Debuggers { get; set; }
}
