using System;
using System.IO;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Counts the errors the shard is printing, by reading the tail of its own session log.
///
/// <para>
/// <b>Why the shard reads its own log rather than being told.</b> Errors here go through Serilog, configured
/// once in <c>Projects/Logger</c> with a single console sink, and this fork's first rule is that the engine
/// is not modified — so there is no seam to subscribe to. Every error every subsystem raises, and every one
/// the engine itself raises, is nonetheless already written to one file. Reading it is the only way to have
/// them all without touching what is not ours.
/// </para>
///
/// <para>
/// <b>What is taken from it is a number, never a retelling.</b> The channel gets "26 errors in the last
/// minute" plus one specimen line, not a copy of the log — the log is already the place to read errors, and
/// an alarm channel that reprints it would be a second, slower copy of the thing it is supposed to point at.
/// The rule this shard learned the hard way is to listen to the shard's own sentences rather than to a
/// re-derivation of them, and a count of a line the shard wrote is the smallest possible re-derivation.
/// </para>
///
/// <para>
/// <b>The level sits outside the bracket: <c>[19:57:35 ERR]</c>.</b> A pattern of <c>[ERR</c> matches nothing
/// and reports a clean run for ever — which it did, for a whole evening, through twenty-six real errors. The
/// needle here is <c>" ERR]"</c>, with the space, and it is a constant rather than a setting for that reason.
/// </para>
///
/// <para>
/// <b>Two honest limits, both named in what it reports.</b> The file is written through a redirect and
/// arrives in buffers, so a count for "the last minute" can be a minute or so behind the world — fine for
/// noticing, useless for timing. And reading starts at the end of the file: errors already in it when the
/// shard came up belong to a previous life, and counting them at the first tick would open every session
/// with an alarm about the last one.
/// </para>
/// </summary>
public static class BotTail
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTail));

    /// <summary>Most bytes read in one look. A tail this far behind is skipped to the end instead.</summary>
    public static int MostBytes { get; set; } = 2000000;

    private static string _path;

    private static long _at;

    private static bool _broken;

    /// <summary>Errors counted since the shard came up.</summary>
    public static long Errors { get; private set; }

    /// <summary>Times the reader fell so far behind that it skipped to the end.</summary>
    public static long Skips { get; private set; }

    /// <summary>The last error line read, cut short. One specimen, so the count has a face.</summary>
    public static string Worst { get; private set; }

    /// <summary>Which file is being watched, or null when none was found.</summary>
    public static string Path => _path;

    /// <summary>
    /// Finds the newest session log and starts at its end.
    ///
    /// The newest rather than a configured name because the launcher stamps the name with the minute it
    /// started, and a watcher pointed at a fixed name would quietly be watching the previous session for
    /// ever — which is the same fault as a stale counter, in file form.
    /// </summary>
    public static void Open()
    {
        _broken = false;
        _at = 0;
        Errors = 0;
        Skips = 0;
        Worst = null;

        try
        {
            var folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Core.BaseDirectory, "..", "logs"));

            if (!Directory.Exists(folder))
            {
                _broken = true;

                return;
            }

            var newest = default(FileInfo);

            foreach (var name in Directory.GetFiles(folder, "session-*.log"))
            {
                var file = new FileInfo(name);

                if (newest == null || file.LastWriteTimeUtc > newest.LastWriteTimeUtc)
                {
                    newest = file;
                }
            }

            if (newest == null)
            {
                _broken = true;

                logger.Warning("No session log to watch for errors, so the alarm channel will not count them");

                return;
            }

            _path = newest.FullName;
            _at = newest.Length;

            logger.Information("Errors will be counted from {Path}, starting at its end ({At} bytes in)", _path, _at);
        }
        catch (Exception e)
        {
            _broken = true;

            logger.Warning("The error tail could not be opened: {Message}", e.Message);
        }
    }

    /// <summary>
    /// Reads whatever has been appended since the last look and counts the error lines in it.
    /// Returns how many there were.
    /// </summary>
    public static int Since()
    {
        if (_broken || _path == null)
        {
            return 0;
        }

        try
        {
            // Shared read-write: the shard's own output is redirected into this file and the handle is held
            // open by the process that started it. Anything stricter fails on every read.
            using var file = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (file.Length < _at)
            {
                // Shorter than last time: a different file, or one that has been rotated under us.
                _at = 0;
            }

            var behind = file.Length - _at;

            if (behind <= 0)
            {
                return 0;
            }

            if (behind > MostBytes)
            {
                _at = file.Length;
                Skips++;

                return 0;
            }

            file.Seek(_at, SeekOrigin.Begin);

            using var reader = new StreamReader(file);

            var found = 0;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains(" ERR]", StringComparison.Ordinal))
                {
                    found++;
                    Errors++;
                    Worst = line.Length > 220 ? line[..220] : line;
                }
            }

            _at = file.Length;

            return found;
        }
        catch (Exception e)
        {
            logger.Warning("The error tail stopped reading: {Message}", e.Message);

            _broken = true;

            return 0;
        }
    }

    /// <summary>One line for the boot log and the summaries.</summary>
    public static string Describe() =>
        _broken || _path == null
            ? "errors are not being counted; no session log is being read"
            : $"{Errors} errors seen in {System.IO.Path.GetFileName(_path)} since the shard came up, {Skips} times too far behind to read";

    public static void Forget()
    {
        _at = 0;
        Errors = 0;
        Skips = 0;
        Worst = null;
    }
}
