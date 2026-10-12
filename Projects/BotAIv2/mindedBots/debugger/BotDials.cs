using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Server.Text;
using Server.BotAI.V2;
using Server.Logging;

namespace Server.BotAI.Mind;

/// <summary>
/// Every tunable number in the bot assemblies, readable and settable while the shard is running.
///
/// <para>
/// <b>It exists because the cost of a change was never the change.</b> A dial lives as a
/// <c>public static</c> property, a configuration file moves it once at boot, and until now the only way to
/// try a different value was to stop the shard, edit JSON, rebuild and start again. That throws away the ore
/// map, the hearth list and every long window of measurement the debugger had accumulated — two or three
/// hours before any question about exhaustion can be asked again. So the numbers that most wanted trying
/// were the ones tried least, and an evening could afford about four experiments where it wanted forty.
/// </para>
///
/// <para>
/// <b>Nothing here is discovered from a list.</b> The dials are found by reflection over the assemblies
/// themselves, exactly as <see cref="BotDebugSight"/> collects its <c>Describe()</c> lines and for the same
/// reason: a hand-written list is correct on the day it is written and silently short by one subsystem
/// afterwards. What the code has, this has.
/// </para>
///
/// <para>
/// <b>A change lasts until the shard stops, and the answer to every set says so.</b> This writes to the
/// living object, never to <c>Configuration/bot-*.json</c> — a tool that edited the files would be a tool
/// able to make a shard that boots differently from the one anybody tested, and a key spelled in the wrong
/// case, ignored in silence, is already the most expensive trap in this configuration system. Keeping a
/// value means writing it into the file by hand, and <c>dials changed</c> prints the list to copy from.
/// </para>
///
/// <para>
/// <b>The model cannot reach this.</b> <see cref="BotHand"/> is the bounded set of verbs the debugger's own
/// mind may ask for, and no dial verb is in it. An observer that can move the thresholds it is judging
/// against does not measure the shard, it negotiates with it — and its log would no longer explain what it
/// saw. Dials move for a person at the keyboard, through the door, and for nobody else.
/// </para>
///
/// <para>
/// <b>Values are read lazily, and a dial's earlier value is kept the first time it is written.</b> Reading
/// every property at boot would run the static constructor of every type in both assemblies for no reason
/// beyond filling a table nobody has asked for. So <c>was</c> means "what it held before this door touched
/// it", which is the only prior value <c>reset</c> can honestly restore.
/// </para>
/// </summary>
public static class BotDials
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDials));

    /// <summary>Name to property, built once by reflection. Key is <c>class.dial</c>, lower-cased.</summary>
    private static Dictionary<string, PropertyInfo> _dials;

    /// <summary>What a dial held before this session's first change of it, and who moved it when.</summary>
    private static readonly Dictionary<string, (string Was, string Now, DateTime When, string Who)> _moved = new();

    private static string _journal;

    private static bool _broken;

    /// <summary>Changes made through the door this session.</summary>
    public static long Changes { get; private set; }

    /// <summary>How many dials the assemblies turned out to have.</summary>
    public static int Count => Map().Count;

    /// <summary>Dials standing at something other than what they booted with.</summary>
    public static int Moved => _moved.Count;

    /// <summary>
    /// Opens the journal. Ordered as the hand's is: a change is refused outright if it cannot be written
    /// down first, because a lever whose record can fail quietly is a lever whose effects cannot be read
    /// back out of the log afterwards.
    /// </summary>
    public static void Open()
    {
        _broken = false;
        _moved.Clear();
        Changes = 0;

        try
        {
            var folder = Path.GetFullPath(Path.Combine(Core.BaseDirectory, "..", "logs"));

            Directory.CreateDirectory(folder);

            _journal = Path.Combine(folder, "bot-dials.log");

            File.AppendAllText(
                _journal,
                $"{Environment.NewLine}== {DateTime.Now:yyyy-MM-dd HH:mm:ss} == shard up, {Count} dials in reach =={Environment.NewLine}"
            );

            logger.Information(
                "{Count} dials can be moved while the shard runs: ask the debugger's door for \"dials <word>\", set with \"dial <Class.Name> <value>\". Every change goes to {Journal} and lasts until the shard stops",
                Count,
                _journal
            );
        }
        catch (Exception e)
        {
            _broken = true;

            logger.Warning("The dial journal could not be opened, so no dial can be moved: {Message}", e.Message);
        }
    }

    /// <summary>
    /// The dials, found once. Both assemblies: the population's own and the debugger's, because the
    /// observer's sampling rate and its frozen-after thresholds are as worth trying against a live shard as
    /// the smith's number of attempts, and they are the ones a restart makes hardest to compare.
    /// </summary>
    private static Dictionary<string, PropertyInfo> Map()
    {
        if (_dials != null)
        {
            return _dials;
        }

        _dials = new Dictionary<string, PropertyInfo>();

        var assemblies = new[] { typeof(BotBeat).Assembly, typeof(BotDials).Assembly }.Distinct();

        foreach (var assembly in assemblies)
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }

            foreach (var type in types)
            {
                if (type.Namespace?.StartsWith("Server.BotAI", StringComparison.Ordinal) != true)
                {
                    continue;
                }

                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    if (!property.CanRead || !property.CanWrite || !Settable(property.PropertyType))
                    {
                        continue;
                    }

                    if (property.GetSetMethod() == null)
                    {
                        continue;
                    }

                    _dials[$"{type.Name}.{property.Name}".ToLowerInvariant()] = property;
                }
            }
        }

        return _dials;
    }

    /// <summary>
    /// The types a line of text can be turned into without guessing. Everything else a dial might be — a
    /// point, an access level, a list — is left alone rather than half-supported: a door that accepts
    /// <c>1592,1680,10</c> for one dial and refuses it for the next is a door that has to be remembered.
    /// </summary>
    private static bool Settable(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(float)
        || type == typeof(bool) || type == typeof(string);

    /// <summary>Full name as it is printed.</summary>
    private static string Pretty(PropertyInfo property) => $"{property.DeclaringType?.Name}.{property.Name}";

    /// <summary>
    /// Finds one dial by name. Exact <c>Class.Name</c> first, then a bare dial name if exactly one class has
    /// it — <c>Tries</c> is unambiguous, <c>Reach</c> is held by several subsystems and must be said in full.
    /// </summary>
    private static PropertyInfo Find(string name, out string trouble)
    {
        trouble = null;

        var map = Map();
        var key = name.Trim().ToLowerInvariant();

        if (map.TryGetValue(key, out var exact))
        {
            return exact;
        }

        var tail = "." + key;
        var matches = map.Where(pair => pair.Key.EndsWith(tail, StringComparison.Ordinal)).ToList();

        if (matches.Count == 1)
        {
            return matches[0].Value;
        }

        trouble = matches.Count == 0
            ? $"no dial is called \"{name}\". Ask for \"dials {name}\" to see what is near it."
            : $"\"{name}\" is held by {matches.Count} classes: {string.Join(", ", matches.Select(m => Pretty(m.Value)))}. Say it in full.";

        return null;
    }

    /// <summary>Reads one, printed the way it is written in source.</summary>
    private static string Read(PropertyInfo property)
    {
        try
        {
            var value = property.GetValue(null);

            return value switch
            {
                null     => "null",
                double d => d.ToString("0.####", CultureInfo.InvariantCulture),
                float f  => f.ToString("0.####", CultureInfo.InvariantCulture),
                bool b   => b ? "true" : "false",
                _        => value.ToString()
            };
        }
        catch (Exception e)
        {
            return $"unreadable ({e.GetBaseException().Message})";
        }
    }

    /// <summary>
    /// One dial, or a table of every dial whose name contains the word. The word is the whole of the search:
    /// <c>dials ms</c> is every clock in the shard side by side, which is the comparison that says whether a
    /// guard is faster than the work it is judging — the shape of defect this shard produces most often.
    /// </summary>
    public static string Show(string filter)
    {
        var map = Map();

        if (string.IsNullOrWhiteSpace(filter))
        {
            return $"{map.Count} dials, {_moved.Count} moved this session. Ask for \"dials <word>\" — the word is"
                + " matched anywhere in Class.Name, so \"ms\" gives every clock and \"anvil\" gives the smith."
                + " \"dials changed\" lists what this session has moved and where to write it down.";
        }

        if (filter.Trim().ToLowerInvariant() is "changed" or "moved")
        {
            return Changed();
        }

        var word = filter.Trim().ToLowerInvariant();
        var hits = map
            .Where(pair => pair.Key.Contains(word, StringComparison.Ordinal))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToList();

        if (hits.Count == 0)
        {
            return $"no dial has \"{filter}\" in its name.";
        }

        using var text = ValueStringBuilder.Create(hits.Count * 48 + 96);

        text.Append($"{hits.Count} {(hits.Count == 1 ? "dial" : "dials")} matching \"{filter}\":");

        foreach (var (key, property) in hits.Take(60))
        {
            text.Append($"{Environment.NewLine}  {Pretty(property)} = {Read(property)}");

            if (_moved.TryGetValue(key, out var move))
            {
                text.Append($"   (was {move.Was}, moved {move.When:HH:mm:ss})");
            }
        }

        if (hits.Count > 60)
        {
            text.Append($"{Environment.NewLine}  and {hits.Count - 60} more; narrow the word.");
        }

        return text.ToString();
    }

    /// <summary>
    /// What has been moved, with the words to write into the configuration file. Printed on demand and also
    /// carried into the debugger's own state line, because a shard running with hand-moved thresholds and no
    /// visible sign of it is a shard whose next measurement is quietly about a different world.
    /// </summary>
    public static string Changed()
    {
        if (_moved.Count == 0)
        {
            return "no dial has been moved this session; every number is as the shard booted with it.";
        }

        var map = Map();

        using var text = ValueStringBuilder.Create(_moved.Count * 72 + 224);

        text.Append(
            $"{_moved.Count} {(_moved.Count == 1 ? "dial has" : "dials have")} moved this session. None of it"
            + " survives a restart — to keep one, write it into its Configuration/bot-*.json by hand"
            + " (PascalCase, and DIALS.md says whether a key reaches it at all):"
        );

        foreach (var (key, move) in _moved.OrderBy(pair => pair.Value.When))
        {
            var name = map.TryGetValue(key, out var found) ? Pretty(found) : key;
            var now = found == null ? move.Now : Read(found);

            text.Append($"{Environment.NewLine}  {name}: {move.Was} -> {now}   (moved at {move.When:HH:mm:ss} by {move.Who})");
        }

        return text.ToString();
    }

    /// <summary>One line for a summary elsewhere, or null when nothing has been touched.</summary>
    public static string Note()
    {
        if (_moved.Count == 0)
        {
            return null;
        }

        var map = Map();

        return $"{_moved.Count} {(_moved.Count == 1 ? "dial" : "dials")} moved by hand this session: "
            + string.Join(
                ", ",
                _moved.Select(pair => $"{(map.TryGetValue(pair.Key, out var p) ? Pretty(p) : pair.Key)} {pair.Value.Was}->{pair.Value.Now}")
            );
    }

    /// <summary>
    /// Moves one dial, or puts it back with <c>reset</c>. Written down before it happens, and refused
    /// outright if the writing fails.
    /// </summary>
    public static string Set(string name, string value, string who)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "which dial? Say \"dial <Class.Name> <value>\", or \"dials <word>\" to look one up.";
        }

        var property = Find(name, out var trouble);

        if (property == null)
        {
            return trouble;
        }

        var key = $"{property.DeclaringType?.Name}.{property.Name}".ToLowerInvariant();
        var before = Read(property);

        if (string.IsNullOrWhiteSpace(value))
        {
            var moved = _moved.TryGetValue(key, out var earlier)
                ? $" (was {earlier.Was} until {earlier.When:HH:mm:ss})"
                : "";

            return $"{Pretty(property)} = {before}{moved}";
        }

        var wanted = value.Trim();
        var putting = wanted is "reset" or "back";

        if (putting)
        {
            if (!_moved.TryGetValue(key, out var earlier))
            {
                return $"{Pretty(property)} has not been moved this session; there is nothing to put back.";
            }

            wanted = earlier.Was;
        }

        if (!Parse(property.PropertyType, wanted, out var parsed, out var why))
        {
            return why;
        }

        if (_broken)
        {
            return "the dial journal is not being written, so nothing may be moved. A change nobody can read"
                + " back afterwards is worse than no change: every later measurement would be about a shard"
                + " whose thresholds are unknown.";
        }

        try
        {
            File.AppendAllText(
                _journal,
                $"[{DateTime.Now:HH:mm:ss}] {who}: {Pretty(property)} {before} -> {wanted}{Environment.NewLine}"
            );
        }
        catch (Exception e)
        {
            _broken = true;

            return $"the dial journal refused the entry ({e.Message}), so the dial was not moved.";
        }

        try
        {
            property.SetValue(null, parsed);
        }
        catch (Exception e)
        {
            try
            {
                File.AppendAllText(
                    _journal,
                    $"           ...and the shard refused it: {e.GetBaseException().Message}{Environment.NewLine}"
                );
            }
            catch
            {
                // The refusal is already going back through the door; a journal that cannot record it is
                // reported by the next set, which checks the same flag.
                _broken = true;
            }

            return $"{Pretty(property)} refused that: {e.GetBaseException().Message}";
        }

        Changes++;

        var after = Read(property);

        if (putting && _moved.TryGetValue(key, out var was) && after == was.Was)
        {
            _moved.Remove(key);
        }
        else
        {
            _moved[key] = (_moved.TryGetValue(key, out var first) ? first.Was : before, after, DateTime.Now, who);
        }

        BotDebugLog.Rule();
        BotDebugLog.Write($"DIAL moved by {who} - a person changing the shard, not a measurement of it");
        BotDebugLog.Block("  what moved:", $"{Pretty(property)}: {before} -> {after}");
        BotDebugLog.Rule();

        logger.Information(
            "A dial was moved by hand: {Dial} {Before} -> {After} ({Who})",
            Pretty(property),
            before,
            after,
            who
        );

        return $"{Pretty(property)}: {before} -> {after}. It holds until the shard stops; \"dials changed\""
            + " lists everything moved so far.";
    }

    /// <summary>
    /// Text to value. The decimal separator is accepted either way round: this keyboard is a Russian one and
    /// types <c>0,5</c>, while the shard's own numbers are all invariant — a door that answered "not a
    /// number" to that would be answering about the locale rather than about the dial.
    /// </summary>
    private static bool Parse(Type type, string text, out object value, out string why)
    {
        value = null;
        why = null;

        var clean = text.Replace(',', '.').Trim();

        if (type == typeof(int))
        {
            if (int.TryParse(clean, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            {
                value = i;

                return true;
            }

            why = $"\"{text}\" is not a whole number, and that dial is one.";

            return false;
        }

        if (type == typeof(long))
        {
            if (long.TryParse(clean, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
            {
                value = l;

                return true;
            }

            why = $"\"{text}\" is not a whole number, and that dial is one.";

            return false;
        }

        if (type == typeof(double) || type == typeof(float))
        {
            if (double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                value = type == typeof(float) ? (float)d : d;

                return true;
            }

            why = $"\"{text}\" is not a number, and that dial is one.";

            return false;
        }

        if (type == typeof(bool))
        {
            switch (clean.ToLowerInvariant())
            {
                case "true":
                case "yes":
                case "on":
                case "1":
                    {
                        value = true;

                        return true;
                    }
                case "false":
                case "no":
                case "off":
                case "0":
                    {
                        value = false;

                        return true;
                    }
            }

            why = $"\"{text}\" is neither true nor false, and that dial is a switch.";

            return false;
        }

        value = text;

        return true;
    }
}
