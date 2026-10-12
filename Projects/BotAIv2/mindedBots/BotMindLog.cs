using System;
using System.IO;
using Server.Logging;

namespace Server.BotAI.Mind;

/// <summary>
/// A log of its own, beside the shard's, holding what a mind decided and why.
///
/// <para>
/// <b>Separate because it is a different kind of reading.</b> The session log is a stream of events at
/// several a second; a mind produces one decision every few seconds and one reckoning after it, and both
/// are paragraphs. Interleaved, each ruins the other: the thinking is lost in the traffic and the traffic
/// is broken up by walls of text. Here they are in order, in one place, and the shard's own log gets one
/// line per decision.
/// </para>
///
/// <para>
/// <b>Stamped with <see cref="DateTime.Now"/>, and that is a correction rather than a preference.</b>
/// <c>Core.Now</c> is UTC while Serilog stamps local time, so a file written with the former cannot be laid
/// beside a session log at all without arithmetic — and the whole reason to keep this file is to read the
/// two together.
/// </para>
/// </summary>
public static class BotMindLog
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMindLog));

    private static string _path;

    private static bool _broken;

    /// <summary>Lines written this session.</summary>
    public static long Lines { get; private set; }

    /// <summary>Where the file is, once it is known.</summary>
    public static string Path => _path;

    public static void Open()
    {
        _broken = false;
        Lines = 0;

        try
        {
            // Beside the session logs rather than inside the distribution: that is where the shard's own
            // logs are, and a file nobody finds is a file nobody reads.
            var folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Core.BaseDirectory, "..", "logs"));

            Directory.CreateDirectory(folder);

            _path = System.IO.Path.Combine(folder, "bot-minds.log");

            Write($"==== minds awake, {BotOllama.Model} at {BotOllama.Endpoint} ====");
        }
        catch (Exception e)
        {
            _broken = true;

            logger.Warning("The minds' own log could not be opened, so thinking will only appear here: {Message}", e.Message);
        }
    }

    /// <summary>One line, stamped. Failure switches the file off rather than complaining every few seconds.</summary>
    public static void Write(string line)
    {
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

            logger.Warning("The minds' own log stopped taking lines and is switched off: {Message}", e.Message);
        }
    }

    /// <summary>A paragraph under a heading, for the things that are paragraphs.</summary>
    public static void Write(string who, string heading, string body)
    {
        Write($"{who} — {heading}");

        if (!string.IsNullOrWhiteSpace(body))
        {
            Write($"    {body.Replace("\n", " ").Replace("\r", " ").Trim()}");
        }
    }
}
