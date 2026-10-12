using System;
using System.Collections.Generic;
using System.IO;
using Server.BotAI.V2;
using Server.Logging;
using Server.Text;

namespace Server.BotAI.Mind;

/// <summary>
/// A door into the running shard for whoever is holding the keyboard rather than a character: write a line
/// into <c>argus-in.txt</c> and the answer appears in <c>argus-out.txt</c> within a couple of seconds.
///
/// <para>
/// <b>It exists because this shard is headless and its console cannot be typed into.</b> <c>Core.Headless</c>
/// is true whenever standard input is redirected, which it always is here, and the engine's own console
/// reader throws rather than reads. So the one way to ask a running world a question was to wait for the
/// next report, and the one way to change what was asked was to stop the shard, rebuild and start it again —
/// which throws away every measurement the debugger had accumulated, and the long-window measurements are
/// precisely the ones worth having. Six restarts in one morning cost more evidence than they bought.
/// </para>
///
/// <para>
/// <b>It reads and it thinks, and since 04.09.2026 it can also reach.</b> Most of what is here answers out
/// of measurements already taken, or puts a question to the model with those measurements attached.
/// <c>note</c> files a remark exactly as speaking to the debugger in the world does. <c>do</c> hands one of
/// <see cref="BotHand.Verbs"/> to the same bounded set of commands the model itself may ask for — and every
/// use of it, from here or from the model, is written to <c>logs/bot-debugger-commands.log</c> before it
/// happens, which is the only reason it is safe to have at all.
/// </para>
///
/// <para>
/// <b>Truncated after reading, and that is the whole of the protocol.</b> One writer, one reader, no locks
/// and nothing to get out of step: what is in the file is what has not been answered yet.
/// </para>
/// </summary>
public static class BotConsole
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotConsole));

    /// <summary>How often the door is tried. A file that is almost always absent costs nothing to look for.</summary>
    public static int ListenMs { get; set; } = 2000;

    private static string _in;

    private static string _out;

    private static long _triedTick;

    private static bool _broken;

    /// <summary>Questions taken through the door this session.</summary>
    public static long Asked { get; private set; }

    public static void Open()
    {
        try
        {
            _in = Path.Combine(Core.BaseDirectory, "argus-in.txt");
            _out = Path.Combine(Core.BaseDirectory, "argus-out.txt");

            File.WriteAllText(_out, $"[{DateTime.Now:HH:mm:ss}] {BotVigil.Name} is listening at {_in}{Environment.NewLine}");

            logger.Information(
                "The debugger's door is open: write a line into {In} and the answer appears in {Out}. Words it knows: state, bot <name>, idle, trades, fighting, rollcall, memory, note <text>, think <question>, hands, do <command>, dials <word>, dial <Class.Name> <value>",
                _in,
                _out
            );
        }
        catch (Exception e)
        {
            _broken = true;

            logger.Warning("The debugger's door could not be opened: {Message}", e.Message);
        }
    }

    /// <summary>Called from the vigil's own beat. Cheap when there is nothing there, which is almost always.</summary>
    public static void Listen(long now)
    {
        if (_broken || _in == null || now - _triedTick < ListenMs)
        {
            return;
        }

        _triedTick = now;

        string[] lines;

        try
        {
            if (!File.Exists(_in))
            {
                return;
            }

            var text = File.ReadAllText(_in);

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            // Emptied before anything is answered, so a question that throws is not asked again for ever.
            File.WriteAllText(_in, "");

            lines = text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
        catch (Exception e)
        {
            logger.Warning("The debugger's door stopped taking questions: {Message}", e.Message);

            return;
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.Length == 0)
            {
                continue;
            }

            Asked++;

            try
            {
                Answer(line);
            }
            catch (Exception e)
            {
                // One bad question must not take the door down. Said in the answer file, where whoever asked
                // is looking, rather than only in the session log.
                Say($"that question threw: {e.Message}");
            }
        }
    }

    private static void Answer(string line)
    {
        var (verb, rest) = Split(line);

        switch (verb)
        {
            case "state":
                Say(BotVigil.Digest());

                return;

            case "bot":
                Say(BotVigil.Row(rest));

                return;

            case "trades":
                Say(BotVigil.TradeTable());

                return;

            case "fighting":
                Say(BotVigil.CombatTable());

                return;

            case "idle":
                Say(BotVigil.Loitering());

                return;

            case "rollcall":
                Say(BotAudit.Last);

                return;

            case "memory":
                Say(BotDebugMemory.Recite() ?? "Nothing remembered yet.");

                return;

            case "note":
                BotDebugMemory.Ask("console", rest);
                BotDebugLog.Rule();
                BotDebugLog.Write("NOTE from the console — a person speaking, not a measurement");
                BotDebugLog.Block("  what was said:", rest);
                BotDebugLog.Rule();
                Say($"noted, and it goes in front of me at my next report: {rest}");

                return;

            case "do":
                {
                    var (order, args) = Split(rest);
                    var answer = BotHand.Run("the console", order, args, "asked by hand through the door", true);

                    Say(answer ?? "nothing to do.");
                }

                return;

            case "hands":
                Say(
                    BotHand.Describe() + $". I know: {string.Join(", ", BotHand.Verbs)}. {BotHand.Manual}"
                    + $" By hand only: {BotHand.ByHand}"
                );

                return;

            // Reading and writing are two words rather than one with a flag: "dials" can never change
            // anything, whatever is typed after it, and that is worth being true of a whole word rather
            // than of an argument somebody has to get right at two in the morning.
            case "dials":
                Say(BotDials.Show(rest));

                return;

            case "dial":
                {
                    var (which, wanted) = Split(rest);

                    Say(BotDials.Set(which, wanted, "the console"));
                }

                return;

            case "chart":
                Say(BotHalls.Describe()
                    + (BotHalls.Charting == null ? "" : $"; I am in {BotHalls.Charting} now, at ({BotHalls.Probing.X}, {BotHalls.Probing.Y})"));

                return;

            case "think":
                // Consider now answers true for a question it has merely taken in hand, and says so itself;
                // the line below is only for a question it could not take at all.
                if (BotVigil.Consider(rest, Say))
                {
                    Say($"thinking about: {rest} — the answer will follow here and in the log.");
                }
                else
                {
                    Say("the model is busy or the question was empty; nothing was asked.");
                }

                return;

            default:
                Say(
                    $"I do not know the word \"{verb}\". I know: state, bot <name>, idle, trades, fighting, rollcall,"
                    + " memory, note <text>, think <question>, hands, do <command>, dials <word>, dial <Class.Name> <value>."
                );

                return;
        }
    }

    private static (string Verb, string Tail) Split(string line)
    {
        var space = line.IndexOf(' ');

        return space < 0
            ? (line.ToLowerInvariant(), "")
            : (line[..space].ToLowerInvariant(), line[(space + 1)..].Trim());
    }

    /// <summary>Everything the door says goes to both files: the answer file to be read, the log to be kept.</summary>
    public static void Say(string what)
    {
        if (_broken || _out == null || what == null)
        {
            return;
        }

        try
        {
            File.AppendAllText(_out, $"[{DateTime.Now:HH:mm:ss}] {what}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception e)
        {
            _broken = true;

            logger.Warning("The debugger's answer file stopped taking lines: {Message}", e.Message);
        }
    }
}
