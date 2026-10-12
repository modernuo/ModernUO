using System;
using System.IO;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The one question asked once, at the boot: did the world come back?
///
/// <para>
/// <b>Written after five hours on an empty island.</b> On 16.09.2026 at 00:20 a deploy stopped the shard inside the
/// engine's own autosave, the snapshot on disk was left truncated, and the next boot read "Loading world done (1586
/// items, 83 mobiles)" against 26,342 the boot before — the bots and the watchers, and nothing else: no vendors, no
/// guards, no creatures, no player. Every alarm this folder had was quiet, because on an empty island nobody dies and
/// every walk finishes; the shard ran that way until a person noticed at 05:24. The counts were in the log all along,
/// and nothing read them.
/// </para>
///
/// <para>
/// The best boot on record is kept in <c>logs/bot-boot.txt</c>, outside <c>Saves</c>, so a restore of the saves does
/// not restore the yardstick with them. A boot with fewer than <see cref="Least"/> of that many mobiles raises the
/// alarm <c>world</c>, which stands until a boot comes back whole.
/// </para>
/// </summary>
public static class BotBoot
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBoot));

    /// <summary>The share of the best boot's mobiles below which the world is read as truncated. A half.</summary>
    public static double Least { get; set; } = 0.5;

    /// <summary>Mobiles this boot loaded.</summary>
    public static int Mobiles { get; private set; }

    /// <summary>Items this boot loaded.</summary>
    public static int Items { get; private set; }

    /// <summary>The most mobiles any boot on record loaded.</summary>
    public static int Best { get; private set; }

    /// <summary>Whether this boot's world is read as truncated.</summary>
    public static bool Truncated { get; private set; }

    private static string File => Path.GetFullPath(Path.Combine(Core.BaseDirectory, "..", "logs", "bot-boot.txt"));

    /// <summary>Reads the counts, compares them with the record, raises or clears the alarm, and writes the record.</summary>
    public static void Check()
    {
        Mobiles = World.Mobiles.Count;
        Items = World.Items.Count;
        Best = 0;
        Truncated = false;

        try
        {
            if (System.IO.File.Exists(File))
            {
                foreach (var line in System.IO.File.ReadAllLines(File))
                {
                    if (line.StartsWith("best=", StringComparison.Ordinal) && int.TryParse(line[5..].Trim(), out var best))
                    {
                        Best = best;
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.Warning("The boot record could not be read: {Message}", e.Message);
        }

        if (Best > 0 && Mobiles < Best * Least)
        {
            Truncated = true;

            var say = $"the world loaded with {Mobiles} mobiles and {Items} items against {Best} mobiles at the best boot on record: "
                      + "the save is probably truncated — stop the shard and restore Saves from Backups/Automatic or Archives/Hourly before anything else";

            logger.Error("{Say}", say);
            BotAlarm.Raise("world", say, Mobiles, Best, "boot");
        }
        else
        {
            var say = $"the world loaded with {Mobiles} mobiles and {Items} items ({Best} mobiles at the best boot on record)";

            logger.Information("{Say}", say);

            if (BotAlarm.Up("world"))
            {
                BotAlarm.Clear("world", say, Mobiles, Best, "boot");
            }
            else
            {
                BotAlarm.Note("world", say, Mobiles, Best, "boot");
            }
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(File)!);
            System.IO.File.WriteAllText(File, $"best={Math.Max(Best, Mobiles)}{Environment.NewLine}last={Mobiles}{Environment.NewLine}items={Items}{Environment.NewLine}at={DateTime.Now:O}{Environment.NewLine}");
        }
        catch (Exception e)
        {
            logger.Warning("The boot record could not be written: {Message}", e.Message);
        }
    }

    public static string Describe() =>
        Best == 0 && Mobiles == 0
            ? "the boot has not been checked"
            : $"the boot loaded {Mobiles} mobiles and {Items} items against {Best} at the best boot on record{(Truncated ? " — READ AS TRUNCATED" : "")}";
}
