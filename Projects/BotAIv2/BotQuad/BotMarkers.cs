using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Writes what the population knows about the island into the client's own world-map pins.
///
/// <para>
/// <b>The one thing on this shard that a person reads without the server telling them anything.</b> Every
/// other view of this project is a log line or a gump: both require somebody to go and look at the right
/// place at the right moment. A pin sits on the world map inside the client the whole time, so "which parts
/// of the island are dangerous" stops being a question anybody has to ask and becomes something they simply
/// see while doing something else.
/// </para>
///
/// <para>
/// <b>The client reads this file once, when it opens its map.</b> Nothing here pushes anything to a running
/// client: the file is rewritten on a slow clock and the map picks it up next time it is opened. That is a
/// property of ClassicUO and not a choice made here, and it is why the clock is minutes rather than seconds
/// — writing it faster would cost the same and change nothing anybody can see.
/// </para>
///
/// <para>
/// <b>Written from the loop rather than from a thread, and measured rather than assumed.</b> The threading
/// rule in this repository says heavy external I/O belongs off the loop; this is a few tens of kilobytes
/// written once every few minutes, which is not heavy, and a background writer would need its own snapshot
/// of a table the loop is mutating. The cost of each write is logged — see <see cref="Spent"/> — so the
/// assumption is checked by the shard itself rather than by me. If it ever stops being a fraction of a
/// millisecond, that number is the argument for moving it.
/// </para>
/// </summary>
public static class BotMarkers
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMarkers));

    /// <summary>
    /// Where the client keeps its pins, relative to the shard's own working directory.
    ///
    /// <para>
    /// The server runs out of <c>Distribution</c> and the client lives beside the fork, so the default walks
    /// up two and across. It is a setting because that is a fact about one person's disk and not about this
    /// shard: anybody else's layout differs, and a wrong path here should be a line in a config file rather
    /// than a rebuild.
    /// </para>
    /// </summary>
    public static string Path { get; set; } = @"..\..\ClassicUO\Data\Client\userMarkers.usr";

    /// <summary>
    /// How often the pins are rewritten.
    ///
    /// A minute, by order. The client only reads the file when a map is opened, so this is not about how
    /// quickly a change appears on screen — it is about how stale the file is when somebody does open it.
    /// The write is a fraction of a millisecond and is measured, so a minute costs nothing worth counting.
    /// </summary>
    public static int EveryMs { get; set; } = 60000;

    /// <summary>
    /// Most pins written at once.
    ///
    /// <para>
    /// A guard on the client rather than on the server: every pin is drawn and labelled, and a map carrying
    /// several thousand of them is a map nobody can read — which is the state the old file was found in, at
    /// 1383 pins from a version of this project that no longer exists. The worst ground is written first, so
    /// what a cap loses is always the quiet ground nobody was going to act on.
    ///
    /// A thousand, by Patrick's order on 02.09.2026, and the number changed because what fills it did: the
    /// file no longer spends its room on squares with no verdict, so a bigger cap buys more ground worth
    /// looking at rather than more markers reading "0.00".
    /// </para>
    ///
    /// <para>
    /// Ten thousand, by Patrick's order on 03.09.2026, and the same reasoning has carried it there: the
    /// island is being walked far wider than it was — 3265 quadrants stood in that evening against 1475 that
    /// morning — so a cap of a thousand had started to throw away ground that had a verdict on it rather
    /// than only the quiet. The readability argument still holds and is still the only argument here; it is
    /// now the client's own zoom that decides it rather than the file.
    /// </para>
    /// </summary>
    public static int Most { get; set; } = 10000;

    /// <summary>
    /// Whether ground nobody has ever stood in is pinned at all.
    ///
    /// Off. An unvisited square is not a fact about the island, it is the absence of one, and pinning them
    /// would bury the squares that mean something under a grid of everywhere a bot happened to walk past.
    /// </summary>
    public static bool PinUnknown { get; set; }

    private static long _tick;

    private static bool _started;

    private static bool _complained;

    /// <summary>Pins written the last time round, and what the write cost.</summary>
    public static int Written { get; private set; }

    public static long Writes { get; private set; }

    public static double Spent { get; private set; }

    /// <summary>Rewrites the file if it is due. Called from the population's own summary clock.</summary>
    public static void Tick()
    {
        var now = Core.TickCount;

        // <b>The first call writes rather than merely starting the clock.</b> Seeding and returning is the
        // right shape for a counter that measures an interval, and the wrong one here: this is asked from a
        // five-minute summary, so skipping the first call meant the file did not appear until ten minutes
        // into a session — and an empty pin file looks exactly like a broken one to whoever opens the map.
        // The tick is still seeded from a real reading and still compared by subtraction.
        if (!_started)
        {
            _started = true;
            _tick = now;

            Write();

            return;
        }

        if (now - _tick < EveryMs)
        {
            return;
        }

        _tick = now;

        Write();
    }

    /// <summary>Rewrites the file now, whatever the clock says.</summary>
    public static void Write()
    {
        var quads = BotQuad.Worst(0);

        if (quads.Count == 0)
        {
            return;
        }

        var watch = Stopwatch.StartNew();

        // Built whole and written once. A file the client may open at any moment should never be half a file,
        // and this is small enough that there is nothing to gain by streaming it.
        var text = new StringBuilder(quads.Count * 64);
        var written = 0;

        for (var i = 0; i < quads.Count && written < Most; i++)
        {
            var quad = quads[i];

            if (!quad.Trodden && !PinUnknown)
            {
                continue;
            }

            // <b>A square with nothing to say is the absence of a fact, exactly as unstood ground is.</b> The
            // cap is worst-first and hundreds of squares sit in the middle band, so on 02.09.2026 the file
            // held 400 pins of which 3 were dire, 9 were worth going to and 388 were blue markers reading
            // "0.00" — squares stood in, never bled in, never acted on. The map looked frozen because the only
            // twelve pins that could ever change were buried under them, and the twelve are the whole point.
            //
            // <b>Said as a band rather than as equality, which is how the first attempt at this failed an hour
            // later the same evening.</b> A crossing lifts a square by a fraction, so a square walked through
            // twice reads 0.004 and prints as "0.00" while being nothing like nought — the file came back with
            // its 388 blues untouched. What is skipped is the square that is neither feared nor proven quiet
            // and has never had a blow landed in it: no verdict either way, and nothing anybody would act on.
            // <b>Read whole, creatures included, which is the half this test was missing.</b> It looked at
            // the earned record alone, so a square whose record is nought and which has four hostiles
            // standing in it right now — reading minus four tenths, the worst kind of ground there is —
            // scored as "neutral, nothing to say" and was left off the map. That is exactly the square
            // somebody opening the map needs to see.
            var reading = BotQuad.Reading(quad);

            if (!PinUnknown
                && reading > BotQuad.Unsafe
                && reading < BotQuad.Safest
                && quad.Blows == 0
                && quad.Deaths == 0)
            {
                continue;
            }

            var middle = quad.Middle;

            // Nothing in a label may be a comma: the file is comma-separated and the client does not quote.
            // The middle dot and the semicolon are what the old file used, and they read well in game.
            text.Append(middle.X).Append(',')
                .Append(middle.Y).Append(",0,")
                .Append(Label(quad, reading))
                .Append(",,")
                .Append(Colour(quad, reading))
                .Append(",3\n");

            written++;
        }

        try
        {
            var full = System.IO.Path.GetFullPath(Path);
            var folder = System.IO.Path.GetDirectoryName(full);

            if (folder == null || !Directory.Exists(folder))
            {
                Complain("there is no folder at {Where}", full);

                return;
            }

            File.WriteAllText(full, text.ToString());
        }
        catch (Exception e)
        {
            Complain("the file at {Where} could not be written: " + e.Message, Path);

            return;
        }

        watch.Stop();

        Written = written;
        Writes++;
        Spent = watch.Elapsed.TotalMilliseconds;
        _complained = false;

        logger.Information(
            "The world map has {Count} pins on it, written in {Spent:F1}ms",
            written,
            Spent
        );
    }

    /// <summary>
    /// What the pin says, in the fewest words that still decide something.
    ///
    /// The rating first, because it is the one number the rules are written in; then only the counts that
    /// are not nought, so a quiet square is a short label rather than a row of zeroes.
    /// </summary>
    private static string Label(BotQuad.Quad quad, double reading)
    {
        // <b>Two fields and nothing else, by Patrick's order of 11.09.2026: the threat and the guild.</b>
        // The first cut of this kept the old extras behind the head — hostiles, what it asks of whoever goes,
        // the dead, the blows landed — on my own judgement that they were worth the room. They were not what
        // was asked for. A pin is read off a world map at a glance, and everything after the second field is
        // something the reader has to skip past to find the two things that decide anything.
        //
        // Everything dropped here still exists and is still printed: the Quad tab of the dashboard carries
        // the same numbers in a place where there is room to read them.
        var text = new StringBuilder(24);

        text.Append(reading.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));

        var held = BotClaim.Pin(
            quad.Map,
            new Point3D(
                quad.X * BotQuad.Side + BotQuad.Side / 2,
                quad.Y * BotQuad.Side + BotQuad.Side / 2,
                0
            )
        );

        if (held != null)
        {
            text.Append(" - ").Append(held);
        }

        return text.ToString();
    }

    /// <summary>
    /// The pin's colour, which is the whole of what makes the map readable at a glance.
    ///
    /// Four states and no more: go here, ordinary, nothing here, never been. A person looking at this map is
    /// asking one question — where should anybody be sent — and every extra colour makes that question
    /// harder rather than easier.
    /// </summary>
    private static string Colour(BotQuad.Quad quad, double reading)
    {
        if (!quad.Trodden)
        {
            return "purple";
        }

        // <b>The bands, and nothing else.</b> The colours used to be drawn off three thresholds that belong
        // to hunting rather than to safety — too quiet to bother with, worth going to, dire — so the map was
        // answering "where is there a fight" while its labels answered "how safe is this". One question per
        // map: Patrick's table of 03.09.2026 is the question, and these are its five answers.
        if (reading <= BotQuad.Bleakest)
        {
            return "red";
        }

        if (reading <= BotQuad.Unsafe)
        {
            return "yellow";
        }

        if (reading >= BotQuad.Safest)
        {
            return "green";
        }

        return reading >= BotQuad.Positive ? "blue" : "white";
    }

    /// <summary>
    /// Said once until it works again, because a path that is wrong is wrong every two minutes for ever.
    /// </summary>
    private static void Complain(string what, object where)
    {
        if (_complained)
        {
            return;
        }

        _complained = true;

        logger.Warning("The world map pins were not written: " + what, where);
    }

    public static string Describe() =>
        Writes == 0
            ? "no world-map pins have been written yet"
            : $"{Written} pins written {Writes} times, last of them in {Spent:F1}ms, at {Path}";

    public static void Forget()
    {
        Written = 0;
        Writes = 0;
        Spent = 0.0;
        _started = false;
        _complained = false;
    }
}
