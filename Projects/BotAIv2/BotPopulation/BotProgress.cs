using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// What a bot has become, kept across restarts: its skills, its fame, its karma and its savings.
///
/// <para>
/// <b>The population itself is deliberately not saved, and this is not a reversal of that.</b> Bots are
/// Mobiles, so the world save contains them, and <see cref="BotPopulation.PurgeSaved"/> throws them away on
/// every load for a good reason written out there: half of what a bot needs lives in objects that would have
/// to be rebuilt anyway, and a kit handed out twice is a bot with two of everything. That argument is about
/// <em>things</em>. It says nothing about what the bot learned, and what the bot learned was going in the
/// bin with the rest every single morning — a population that hunted, mined and sewed for a whole day woke
/// up as sixteen novices, which makes the one number this project measures work by permanently worthless.
/// </para>
///
/// <para>
/// So the belongings are still rebuilt from nothing and only the learning is carried over. Nothing here
/// holds a reference to an item, a mobile or a serial: it is names and numbers, which is exactly why it can
/// outlive a world the rest of the bot cannot.
/// </para>
///
/// <para>
/// <b>Anything that stops matching is thrown away rather than patched.</b> Patrick's rule, and the right one
/// for a store this cheap to rebuild: if the format changes the whole file is dropped, and if a name now
/// belongs to a different class than it did the record for that name is dropped. A bot losing a day of skill
/// costs a day; a bot restored into the wrong body costs an evening of wondering why the smith cannot smith.
/// </para>
/// </summary>
public sealed class BotProgress : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotProgress));

    /// <summary>
    /// The shape of what is written below.
    ///
    /// <para>
    /// Bump this whenever the record changes. A shape this build does not know how to read is dropped whole
    /// rather than guessed at — reading an unknown shape into a new one is how a save file quietly poisons a
    /// population, and there is nothing here worth that risk.
    /// </para>
    ///
    /// <para>
    /// <b>A shape it does know is read and carried forward, and that is a change of rule made on evidence.</b>
    /// The rule here was to drop the file on every bump, and it cost more than it saved twice over. Once in
    /// what it threw away: adding one field to this record is a routine thing to want, and paying for it with
    /// a day of every bot's learning makes the record unextendable in practice. And once in how it failed —
    /// see <see cref="Deserialize"/>, where returning early left the engine's own completeness check staring
    /// at half a file and the shard stopped dead on a console prompt no headless start can answer.
    /// </para>
    /// </summary>
    private const int Shape = 2;

    /// <summary>The oldest shape this build can still read. Below it the file is dropped.</summary>
    private const int Oldest = 1;

    /// <summary>
    /// What each bot had learned, by name and class. Names are dealt out in order, so they are stable.
    ///
    /// <para>
    /// <b>By class as well since 15.09.2026, because a name alone let a class-mix edit wipe a fighter.</b> A mix that
    /// changes deals the same names out to different trades, and the record was keyed by name with the class as a check: a
    /// warrior dealt out as a mage had its record dropped whole, and when the mix went back the warrior's levelling was gone.
    /// Each name and class now keeps its own record, and a bot is handed the one for the class it is. The file's shape is
    /// unchanged — every record already carried its class. Fable's option, taken on Patrick's word (DECISIONS §6).
    /// </para>
    /// </summary>
    private static readonly Dictionary<(string Name, string Class), Learned> _saved = new(NameAndClass.Instance);

    /// <summary>A name and a class compared without regard to case, as the name alone was.</summary>
    private sealed class NameAndClass : IEqualityComparer<(string Name, string Class)>
    {
        public static readonly NameAndClass Instance = new();

        public bool Equals((string Name, string Class) a, (string Name, string Class) b) =>
            string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Class, b.Class, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Name, string Class) key) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(key.Name ?? ""),
                StringComparer.OrdinalIgnoreCase.GetHashCode(key.Class ?? "")
            );
    }

    private static BotProgress _store;

    /// <summary>Registered from <c>BotCore.Configure</c>: a persistence must exist before the world loads.</summary>
    public static void Configure() => _store ??= new BotProgress();

    /// <summary>The priority is only an ordering among save files; nothing else depends on it.</summary>
    public BotProgress() : base("BotProgress", 12)
    {
    }

    /// <summary>How many bots were read back from the last save, for the start-up line to report.</summary>
    public static int Remembered => _saved.Count;

    /// <summary>Whether some bot's learning is kept under this name, of whatever class. See BotPopulation.RaiseNewcomer.</summary>
    public static bool Remembers(string name)
    {
        foreach (var (key, _) in _saved)
        {
            if (key.Name.InsensitiveEquals(name))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>How many of them have actually been handed back to a living bot this session.</summary>
    public static int Restored { get; private set; }

    /// <summary>Coin handed back to bots that had earned it in an earlier session, for the start-up line.</summary>
    public static long Returned { get; private set; }

    /// <summary>
    /// Whether a bot's savings survive a restart along with its skills.
    ///
    /// False by Patrick's order of 08.09.2026: a purse carried across many restarts stops being a fact about
    /// the economy and becomes a fact about how often the shard has been started. Skills still carry over —
    /// what a bot has learned is its own, what it has banked is the session's.
    /// </summary>
    public static bool Savings { get; set; }

    /// <summary>
    /// Whether the record has been wiped by hand this session. The next save then writes an empty store instead of
    /// gathering the living bots' skills back into it — which it would otherwise do, because the store is built from
    /// the bots at every save — and the next boot raises the population as novices. Cleared by a load.
    /// </summary>
    private static bool _wiped;

    /// <summary>Everybody's learning forgotten, to take effect at the next boot. Patrick's reset of 16.09.2026.</summary>
    public static int Wipe()
    {
        var gone = _saved.Count;

        _saved.Clear();
        _wiped = true;

        return gone;
    }

    /// <summary>
    /// Gives a freshly raised bot whatever the bot of that name had learned, or leaves it a novice.
    ///
    /// <para>
    /// Called after <c>Become</c>, so it writes over the starting skills the class deals out rather than
    /// being written over by them. Only upward: a saved skill below the class's own starting value is
    /// ignored, because that would be a restore that makes a bot worse than a new one.
    /// </para>
    /// </summary>
    public static bool Restore(BotMobile bot)
    {
        var name = bot?.Name;
        var calling = bot?.Class?.Name;

        // By name and class. A mix that changed overnight deals the same names out to different trades, and a miner restored
        // into a mage is worse than a novice mage — so the mage starts as a novice, and the miner's record waits for the day
        // the mix deals that name a pick again. See _saved.
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(calling) || !_saved.TryGetValue((name, calling), out var learned))
        {
            return false;
        }

        for (var i = 0; i < learned.Skills.Count; i++)
        {
            var (which, value) = learned.Skills[i];

            if (which < 0 || which >= bot.Skills.Length)
            {
                continue;
            }

            var skill = bot.Skills[(SkillName)which];

            if (skill != null && value > skill.Base)
            {
                skill.Base = value;
            }
        }

        bot.Fame = Math.Max(bot.Fame, learned.Fame);
        bot.Karma = learned.Karma;

        // <b>What it earned, for the same reason as what it learned — and this half was missing.</b> A bot's
        // skills outlived a restart and its money did not, so every session began with the whole population
        // holding its starting float. Twenty restarts on 27.08.2026 and the counters read exactly as a broken
        // economy would: 849 of 849 riders could not afford a horse, 1436 of 2558 could not afford a lesson,
        // 337 could not afford a piece of armour. Not one of those was a price set too high. It was a
        // population that had never been allowed to save up, and the reason it could not was here.
        //
        // Upward only, like the skills above, and into the account rather than the pack: this is savings, not
        // pocket money, every seller on this shard is paid by deposit, and a thousand coins in a backpack is
        // twenty stones of carrying weight that would drop into the first corpse.
        // <b>Off by Patrick's order of 08.09.2026, and the reason it was ever on is directly above.</b> He
        // watched the Architect sit on twelve thousand gold that no single session had earned and asked for
        // the account to be cleared with everything else a restart clears. That is a legitimate call — a
        // fortune carried across twenty restarts is not a measurement of this shard, it is a measurement of
        // how many times it has been started — but it is the exact condition the note above was written to
        // cure, so it is a switch and not a deletion.
        //
        // The numbers to watch after turning it off are the ones that told the story last time: riders who
        // cannot afford a horse, pupils who cannot afford a lesson, bots who cannot afford a piece of
        // armour. If those come back, this is why.
        var has = BotYield.Wealth(bot);

        if (Savings && learned.Purse > has)
        {
            var owed = learned.Purse - has;

            if (Banker.Deposit(bot, owed))
            {
                Returned += owed;
            }
        }

        Restored++;

        return true;
    }

    /// <summary>
    /// Everything the living population knows, taken at the moment of saving.
    ///
    /// <para>
    /// Harvested here rather than kept up to date as skills rise, because a skill rises on the engine's own
    /// check several times a minute per bot and a store that listened for that would be doing bookkeeping all
    /// day to answer a question asked once an hour.
    /// </para>
    /// </summary>
    private static void Gather()
    {
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false } || bot.Class == null || string.IsNullOrEmpty(bot.Name))
            {
                continue;
            }

            var learned = new Learned
            {
                Class = bot.Class.Name,
                Fame = bot.Fame,
                Karma = bot.Karma,
                Purse = BotYield.Wealth(bot)
            };

            for (var s = 0; s < bot.Skills.Length; s++)
            {
                var skill = bot.Skills[s];

                if (skill is { Base: > 0.0 })
                {
                    learned.Skills.Add(((int)skill.SkillName, skill.Base));
                }
            }

            _saved[(bot.Name, bot.Class.Name)] = learned;
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        if (_wiped)
        {
            writer.WriteEncodedInt(Shape);
            writer.WriteEncodedInt(0);

            return;
        }

        Gather();

        writer.WriteEncodedInt(Shape);
        writer.WriteEncodedInt(_saved.Count);

        foreach (var (key, learned) in _saved)
        {
            writer.Write(key.Name);
            writer.Write(learned.Class);
            writer.WriteEncodedInt(learned.Fame);
            writer.WriteEncodedInt(learned.Karma);
            writer.WriteEncodedInt(learned.Purse);
            writer.WriteEncodedInt(learned.Skills.Count);

            for (var i = 0; i < learned.Skills.Count; i++)
            {
                var (which, value) = learned.Skills[i];

                writer.WriteEncodedInt(which);
                writer.Write(value);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        _saved.Clear();
        _wiped = false;

        var shape = reader.ReadEncodedInt();

        if (shape < Oldest || shape > Shape)
        {
            // <b>Nothing is read and nothing is guessed at — and this path stops the shard.</b> The engine
            // checks that a persistence consumed every byte of its own file and asks the console what to do
            // when it did not, which a shard started without a console cannot answer: it simply stands at
            // "Loading world" for ever. That is the right trade for a file this build genuinely cannot read
            // — better a stopped shard than a poisoned population — but it is not something to walk into by
            // accident, which is why the shapes above are read rather than dropped. Delete
            // Saves/BotProgress/BotProgress.bin to get past it.
            logger.Warning(
                "The saved progress is shape {Found} and this build reads {Oldest} to {Wanted}; it cannot be read, and the shard will stop on the engine's own prompt until Saves/BotProgress/BotProgress.bin is deleted",
                shape,
                Oldest,
                Shape
            );

            return;
        }

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var name = reader.ReadString();

            var learned = new Learned
            {
                Class = reader.ReadString(),
                Fame = reader.ReadEncodedInt(),
                Karma = reader.ReadEncodedInt(),

                // Shape 1 knew nothing about money. Those bots come back as they always did — with their
                // learning and an empty account — which is exactly what they had before this field existed.
                Purse = shape >= 2 ? reader.ReadEncodedInt() : 0
            };

            var skills = reader.ReadEncodedInt();

            for (var s = 0; s < skills; s++)
            {
                learned.Skills.Add((reader.ReadEncodedInt(), reader.ReadDouble()));
            }

            if (!string.IsNullOrEmpty(name))
            {
                _saved[(name, learned.Class)] = learned;
            }
        }
    }

    /// <summary>One bot's learning: no items, no serials, nothing that can dangle.</summary>
    private sealed class Learned
    {
        public string Class;

        public int Fame;

        public int Karma;

        /// <summary>Pocket and account together, as <c>BotYield.Wealth</c> reckons them.</summary>
        public int Purse;

        public List<(int Which, double Base)> Skills { get; } = [];
    }
}
