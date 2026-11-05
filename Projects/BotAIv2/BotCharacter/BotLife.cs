using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>One remembered event of a bot's life, told in its own words: what happened, who to, and how much it weighed.</summary>
public sealed class BotTale
{
    /// <summary>death, saved, raised, wronged, sale, war, duel, veteran.</summary>
    public string Kind;

    /// <summary>The other one in it: the killer, the rescuer, the buyer. Null for a veteran's title.</summary>
    public string Other;

    /// <summary>The sentence as the bot says it, first person, with the real names and numbers.</summary>
    public string Text;

    /// <summary>How much it matters. The three heaviest are kept; see <see cref="BotBiography.Keep"/>.</summary>
    public double Weight;

    /// <summary>The number in it (gold, hits, kills), for the page and for "is the new one bigger".</summary>
    public int Number;

    public DateTime At;

    /// <summary>When it was last told aloud at a fire or a table; not told yet when default. See <see cref="BotBiography.TaleRestHours"/>.</summary>
    public DateTime ToldAt;
}

/// <summary>
/// A story this bot heard told aloud at a fire or a table (PLAN-life A6): who told it, what kind it is, whom it is about,
/// the words as they were said, where it was heard and how often it has been carried on. Recorded only for a bot actually
/// seated there when the line was said — the shard's honest "heard of X".
/// </summary>
public sealed class BotHeardTale
{
    public string Teller;

    public string Kind;

    /// <summary>The one the story is about, as <see cref="BotTale.Other"/>; may be null.</summary>
    public string About;

    public string Text;

    public string Place;

    public DateTime At;

    /// <summary>Times this copy has been retold onward; a story carried too far is let go.</summary>
    public int Hops;
}

/// <summary>What one bot thinks of one other bot, by name, from −100 to +100. See <see cref="BotTies"/>.</summary>
public sealed class BotTie
{
    public string Name;

    public double Regard;

    /// <summary>Times the other saved, raised or healed this one.</summary>
    public int Saved;

    /// <summary>Times the other robbed or killed this one.</summary>
    public int Wronged;

    /// <summary>Kills made side by side in one company.</summary>
    public int Beside;
}

/// <summary>
/// Everything one bot has become as a person rather than as a worker: its nature, its life's story and the bots it
/// loves and hates. Keyed by name, because a bot is raised afresh with a new serial at every boot and its name is the
/// one thing that carries over (see <c>BotProgress</c>). Persisted by <see cref="BotCharacterStore"/>.
/// </summary>
public sealed class BotLife
{
    public BotLife(string name) => Name = name;

    public string Name { get; }

    public BotTrait Traits;

    /// <summary>Whether <see cref="Traits"/> has been drawn yet; false only between a record read from an older shape and its first look.</summary>
    public bool Drawn;

    /// <summary>Drawn in this run and not yet announced: a newcomer, as against a bot read back from the save. Not saved.</summary>
    public bool Fresh;

    /// <summary>Kills to its credit: one for a creature it felled alone or a bot it killed in war, a share of one for a company's.</summary>
    public double Kills;

    public int Deaths;

    public int SavedTimes;

    public int RaisedTimes;

    public int Robbed;

    public int WarKills;

    public int DuelsWon;

    public int DuelsLost;

    public int BestSale;

    public bool Veteran;

    public DateTime VeteranAt;

    public readonly List<BotTale> Tales = new(3);

    /// <summary>Stories heard told aloud at fires and tables, the heaviest few kept; see <see cref="BotBiography.Heard"/>.</summary>
    public readonly List<BotHeardTale> HeardTales = new(4);

    public readonly List<BotTie> Ties = new(12);

    /// <summary>The tale told last, so the next is a different one. Not saved.</summary>
    public int ToldIndex = -1;

    /// <summary>The lowest regard on any tie, kept so a proposer can skip a bot with no enemy without walking its ties.</summary>
    public double Worst;

    public BotTie Tie(string other)
    {
        for (var i = 0; i < Ties.Count; i++)
        {
            if (string.Equals(Ties[i].Name, other, StringComparison.Ordinal))
            {
                return Ties[i];
            }
        }

        return null;
    }

    public void Rewound()
    {
        var worst = 0.0;

        for (var i = 0; i < Ties.Count; i++)
        {
            if (Ties[i].Regard < worst)
            {
                worst = Ties[i].Regard;
            }
        }

        Worst = worst;
    }
}

/// <summary>
/// The register of <see cref="BotLife"/> records: by name for the store and for other bots' ties, and by serial for the
/// auction, which asks once per offer and must not hash a string to do it.
/// </summary>
public static class BotLives
{
    private static readonly Dictionary<string, BotLife> _byName = new(StringComparer.Ordinal);

    private static readonly Dictionary<Serial, BotLife> _bySerial = [];

    /// <summary>Bodies by name, refreshed by the module's clock, so a tie can find the one it is about without a search.</summary>
    private static readonly Dictionary<string, BotMobile> _bodies = new(StringComparer.Ordinal);

    public static IEnumerable<BotLife> All => _byName.Values;

    public static int Count => _byName.Count;

    /// <summary>Records drawn fresh in this run, as against read back from the save.</summary>
    public static long Drawn { get; private set; }

    /// <summary>The record for this bot, made (and its nature drawn) the first time it is asked for. Null for anything not a bot.</summary>
    public static BotLife Of(Mobile m)
    {
        if (m is not BotMobile bot || string.IsNullOrEmpty(bot.Name))
        {
            return null;
        }

        if (_bySerial.TryGetValue(bot.Serial, out var life) && string.Equals(life.Name, bot.Name, StringComparison.Ordinal))
        {
            return life;
        }

        life = Named(bot.Name);
        _bySerial[bot.Serial] = life;
        _bodies[bot.Name] = bot;

        return life;
    }

    /// <summary>The record for a name, made if there is none.</summary>
    public static BotLife Named(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (!_byName.TryGetValue(name, out var life))
        {
            life = new BotLife(name);
            _byName[name] = life;
        }

        if (!life.Drawn)
        {
            life.Traits = BotTraits.Draw(name);
            life.Drawn = true;
            life.Fresh = true;
            Drawn++;
        }

        return life;
    }

    /// <summary>The record for a name if one exists; never makes one.</summary>
    public static BotLife Find(string name) =>
        name != null && _byName.TryGetValue(name, out var life) ? life : null;

    /// <summary>A record read back from the save, put in as it was.</summary>
    public static void Put(BotLife life)
    {
        if (life?.Name != null)
        {
            _byName[life.Name] = life;
        }
    }

    /// <summary>The living body of a bot by name, in the world now, or null.</summary>
    public static BotMobile Body(string name) =>
        name != null && _bodies.TryGetValue(name, out var bot) && bot is { Deleted: false } ? bot : null;

    /// <summary>Re-reads who is in the world. On the module's clock; the population is two hundred, never a world walk.</summary>
    public static void Refresh()
    {
        _bodies.Clear();

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && !string.IsNullOrEmpty(bot.Name))
            {
                _bodies[bot.Name] = bot;
            }
        }

        // Serials of bots no longer in the world are dropped so the map does not grow with every rest and return.
        if (_bySerial.Count > bots.Count * 2 + 64)
        {
            _bySerial.Clear();
        }
    }

    /// <summary>A new world: everything read again from the save.</summary>
    public static void Forget()
    {
        _byName.Clear();
        _bySerial.Clear();
        _bodies.Clear();
        Drawn = 0;
    }

    /// <summary>
    /// <b>Patrick's reset (07.10.2026, reset26): every life forgotten, so the save writes the characters empty.</b> The reset of
    /// 20:56 that night deleted the bodies and left <c>Saves/BotCharacter</c> whole, and the newcomers born under the old names were
    /// born with the old kills, veteran's titles, tales and ties — the friend of a bot that had never met them, the grudge of a duel
    /// nobody fought — until the folder was moved away by hand. <see cref="_byName"/> is exactly what <see cref="BotCharacterStore"/>
    /// writes. A nature is drawn from the name (<see cref="BotTraits.Draw"/>), so a name born again draws the same nature; that is
    /// the design, not something kept. The grudges standing and the session's counters go with it. Returns the lives forgotten.
    /// </summary>
    public static int Wipe()
    {
        var lives = _byName.Count;

        Forget();
        BotTraits.Forget();
        BotBiography.Forget();
        BotTies.Forget();
        BotGrudges.Forget();

        return lives;
    }
}
