using System;
using System.IO;
using Server.Logging;

namespace Server.BotAI.Mind;

/// <summary>
/// The debugger's own file: <c>logs/bot-debugger.log</c>, and nothing else is written into it.
///
/// <para>
/// <b>Its own file rather than a section of the minds', and the reason is what the two are for.</b>
/// <c>bot-minds.log</c> is a record of decisions being made — one every twenty seconds, three bots deep, and
/// it is read forwards to see whether the choosing is any good. This is a record of the shard being
/// examined: a handful of paragraphs an hour, each one a claim about a defect, and it is read backwards
/// from a symptom. Interleaved, the claims are lost in the choices, and the person who wants to know what
/// the debugger thought at half past two has to read four megabytes of somebody else's shopping.
/// </para>
///
/// <para>
/// <b>Every finding is written with the measurements it was made from, in the same entry.</b> The model's
/// sentence is a conjecture and is labelled as one; underneath it goes the digest the conjecture was made
/// out of, unedited. That is the whole difference between a log that can be checked and a log that has to
/// be believed — and a conjecture nobody can check is worse than no conjecture, because it gets acted on.
/// </para>
///
/// <para>
/// Stamped with <see cref="DateTime.Now"/> so it lies beside the session log without arithmetic:
/// <c>Core.Now</c> is UTC and Serilog stamps local time. Same correction as the minds' log, same reason.
/// </para>
/// </summary>
public static class BotDebugLog
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDebugLog));

    private static string _path;

    private static bool _broken;

    /// <summary>Lines written this session.</summary>
    public static long Lines { get; private set; }

    /// <summary>Where the file is, once it is known.</summary>
    public static string Path => _path;

    public static void Open(string who)
    {
        _broken = false;
        Lines = 0;

        try
        {
            var folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Core.BaseDirectory, "..", "logs"));

            Directory.CreateDirectory(folder);

            _path = System.IO.Path.Combine(folder, "bot-debugger.log");

            Rule();
            // The debugger's own model, not the population's. They have been different since 01.09.2026,
            // and a header naming the wrong one is a fact that goes stale where it is read first.
            Write($"{who} is awake, thinking with {BotVigil.Model} at {BotOllama.Endpoint}");
            Rule();
        }
        catch (Exception e)
        {
            _broken = true;

            logger.Warning("The debugger's own log could not be opened, so it will only speak here: {Message}", e.Message);
        }
    }

    public static void Rule() => Write(new string('=', 96));

    /// <summary>One line, stamped. A failure switches the file off rather than complaining every minute.</summary>
    public static void Write(string line)
    {
        // The live window's feed first, and apart from the file: a log that has stopped taking lines still has a squad thinking.
        try
        {
            BotDebugWeb.Heard(line);
        }
        catch
        {
            // The window answers for itself; the log is never switched off for its sake.
        }

        if (_broken || _path == null || line == null)
        {
            return;
        }

        try
        {
            File.AppendAllText(_path, $"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
            Lines++;
        }
        catch (Exception e)
        {
            _broken = true;

            logger.Warning("The debugger's own log stopped taking lines and is switched off: {Message}", e.Message);
        }
    }

    /// <summary>
    /// A block of several lines under a heading, indented so that a paragraph is obviously one entry.
    /// Used for the measurements a finding was made from, which are the point of keeping this file at all.
    /// </summary>
    public static void Block(string heading, string body)
    {
        Write(heading);

        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        var lines = body.Replace("\r", "").Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(lines[i]))
            {
                Write($"    {lines[i].TrimEnd()}");
            }
        }
    }
}
