using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using Server.Guilds;
using Server.Logging;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// What goes into a bot's book, when its story is rebuilt and handed to the page, and which chapter the model retells next.
/// Each bot's life story in chapters, and the island's Chronicle (PLAN-life A3, Patrick 08.10.2026: "the generation of the
/// bots' history"; the week's plan of 12.10.2026, §5 A3).
///
/// <para>
/// <b>Why a book of its own, and not the biography alone.</b> The biography of 07.10.2026 (<see cref="BotBiography"/>) keeps the
/// three heaviest tales a bot has and lets the rest go: on the run of 12–13.10 (boot 23:18, 4½ hours) it wrote 232 tales —
/// 127 sales, 85 rescues, 12 wrongs, 6 first deaths, 2 raisings — and turned 798 away as too light. Three tales of two kinds
/// make at most three chapters and usually one; and joining a guild, an office, a house, a craft's rank, a relic, a title are
/// not tales at all. So the story keeps its own notes, a few dozen a bot (<see cref="MostNotes"/>), from two places: the event
/// stream as each event is posted (<see cref="Heard"/>, the officers', politics', relics', crime's, residence's and the guild
/// halls' own sentences) and a slow walk over the population (<see cref="Tick"/>) that notices what no event says — a guild
/// joined or left by any road, a rank risen, a house bought, the champion's yellow, the veteran's title. The biography's tales
/// are quoted into the chapters as they stand, in the bot's own words.
/// </para>
///
/// <para>
/// <b>Off the loop where it matters, on a slow clock where it does not.</b> The walk looks at <see cref="EveryMs"/>'s share of the
/// population each <see cref="TickMs"/> (three bots in two seconds at 450), notes what changed and builds each visited bot's story
/// by template, which is a few dozen microseconds (measured into <see cref="BuildTicks"/> and printed). The model is asked
/// through a seam the minds assembly fills (<see cref="Ask"/>, as the duke and the steward are): at most
/// <see cref="VoicePerHour"/> times an hour, only when its single slot is free, once per chapter for the facts it was told —
/// the answer is kept against their hash and the same facts are never asked again. Its HTTP call runs off the loop
/// (<c>BotOllama</c>) and the answer comes back through <c>Core.LoopContext.Post</c>; it is checked on the loop
/// (<see cref="BotStoryWriter.Check"/>) and thrown away if it names anybody or any number the facts do not. The dashboard reads
/// the stories through one immutable snapshot swapped by a volatile reference and asks for a story by posting its name to the
/// loop; nothing on the listener's threads touches a book.
/// </para>
/// </summary>
public static class BotStory
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotStory));

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    // ------------------------------------------------------------------ dials

    /// <summary>The stories and the Chronicle at all. Off: nothing is noted, built or asked, and the page says so.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The story's clock. Two seconds: a slice of the walk, the Chronicle's page, the model's turn and the page's snapshot ride on it.</summary>
    public static int TickMs { get; set; } = 2000;

    /// <summary>
    /// Each living bot is looked at once in this long: what changed is noted and its story rebuilt. Five minutes — a story is
    /// read by a person, not by the auction, and three bots a tick at 450 costs the loop well under a millisecond.
    /// </summary>
    public static int EveryMs { get; set; } = 300000;

    /// <summary>The stories handed to the page at most this often; a story asked for on the page is built and handed over at once.</summary>
    public static int PublishEveryMs { get; set; } = 10000;

    /// <summary>
    /// How long a book just opened (and its first look after this), and the walk after a boot, take what they see as where the bot
    /// stands rather than as news.
    /// Two minutes. A bot's rank is worked out on its own clock some seconds after it is reclaimed, and the houses are adopted by
    /// the estate's start: without this, the first deploy would have written "became an Expert Tailor" and "bought a house" for
    /// every bot that had been both for a week, and every restart "left The Anchor" for a guild re-formed a few ticks late.
    /// </summary>
    public static int SettleMs { get; set; } = 120000;

    /// <summary>
    /// Notes kept a bot; past it the lightest goes, never the last of its stage. Forty: a busy bot of 12.10 gathers four or five
    /// a day (a guild, a rank, a home, an office, a relic), so this is a week or two of a full life before anything is let go.
    /// </summary>
    public static int MostNotes { get; set; } = 40;

    /// <summary>Books kept; past it the one longest unseen whose bot is not in the world goes. 450 bots, fifty children, the rested and the dead.</summary>
    public static int MostBooks { get; set; } = 1500;

    /// <summary>The old-age chapter opens after this many days since the bot was born or first found. Fourteen.</summary>
    public static int OldAgeDays { get; set; } = 14;

    /// <summary>Whether the model retells chapters and closed days at all. Off: the templates are the stories.</summary>
    public static bool Voiced { get; set; } = true;

    /// <summary>
    /// The most the model is asked an hour, chapters and days together. Six: the model's one slot is shared with Argus's watchers,
    /// the duke and the stewards, and a retelling costs a few seconds warm and a cold load of half a minute when the watchers'
    /// model held the card. A story asked for on the page jumps the queue but not this bound.
    /// </summary>
    public static int VoicePerHour { get; set; } = 6;

    /// <summary>The model the story asks; empty means the duke's (<c>BotParley.JudgeModel</c>), else the population's own.</summary>
    public static string VoiceModel { get; set; } = "";

    /// <summary>A chapter is offered to the model only with this many facts of things seen to happen: a thin chapter gains nothing by a voice.</summary>
    public static int VoiceLeastFacts { get; set; } = 3;

    /// <summary>The longest retelling kept, in characters, cut at the last whole sentence.</summary>
    public static int VoiceMostChars { get; set; } = 1200;

    /// <summary>The length the model is asked to keep to, in words.</summary>
    public static int VoiceMostWords { get; set; } = 150;

    /// <summary>Names asked for on the page whose chapters go to the model first; the oldest ask goes past it.</summary>
    public static int MostWanted { get; set; } = 20;

    /// <summary>The seam the minds assembly fills (<c>BotStoryVoice</c>): asks, and calls back on the loop with the text or null. False: not asked now.</summary>
    public static Func<BotStoryAsk, Action<string, long>, bool> Ask { get; set; }

    // ------------------------------------------------------------------ counters

    public static long BornNoted { get; private set; }

    public static long Opened { get; private set; }

    public static long FromEvents { get; private set; }

    public static long FromWalk { get; private set; }

    public static long Trimmed { get; private set; }

    public static long Repeats { get; private set; }

    public static long Looks { get; private set; }

    public static long Builds { get; private set; }

    /// <summary>Stopwatch ticks spent building stories by template, and the most one build took: the loop's cost of the stories.</summary>
    public static long BuildTicks { get; private set; }

    public static long BuildMostTicks { get; private set; }

    public static long Published { get; private set; }

    public static long Requests { get; private set; }

    public static long Unknown { get; private set; }

    public static long VoiceAsked { get; private set; }

    public static long VoiceAnswered { get; private set; }

    public static long VoiceNone { get; private set; }

    public static long VoiceKept { get; private set; }

    public static long VoiceBusy { get; private set; }

    public static long VoiceStale { get; private set; }

    public static long VoiceLost { get; private set; }

    public static long VoiceWaitedMs { get; private set; }

    /// <summary>Chapters a reader was shown in the model's words (its prose cached for these very facts), and in the template's though they qualify.</summary>
    public static long CacheHits { get; private set; }

    public static long CacheMisses { get; private set; }

    public static long Faults { get; private set; }

    /// <summary>Crowns of forced tests the walk was told to take as seen and not write (<see cref="TestCrowned"/>).</summary>
    public static long TestCrowns { get; private set; }

    private static readonly Dictionary<string, long> _thrown = new(StringComparer.Ordinal);

    /// <summary>Notes written from events and the walk, by kind (office, guild, opened:shop …). See <see cref="Counted"/>.</summary>
    private static readonly Dictionary<string, long> _byKind = new(StringComparer.Ordinal);

    /// <summary>Events heard on a thread not the loop's and dropped. Should stay 0; the only field touched off the loop, by Interlocked.</summary>
    private static long _offLoop;

    // ------------------------------------------------------------------ state

    private static readonly Dictionary<string, BotStoryBook> _books = new(StringComparer.Ordinal);

    /// <summary>The loop's working copy of each story's JSON; the page reads <see cref="_shelf"/>, never this.</summary>
    private static readonly Dictionary<string, string> _json = new(StringComparer.OrdinalIgnoreCase);

    private static readonly BotStoryChapter[] _chapters = NewChapters();

    private static readonly List<string> _wanted = new(8);

    private static readonly Queue<long> _asks = new(8);

    private static int _cursor;

    private static int _voiceCursor;

    private static bool _asking;

    private static long _askedTick;

    private static long _publishedTick;

    private static bool _publishedOnce;

    private static bool _changed;

    /// <summary>A story asked for on the page came within a second of the last snapshot: the next tick hands it over.</summary>
    private static bool _owed;

    /// <summary>
    /// Set by <see cref="Wipe"/> and cleared by the next <see cref="Started"/>: between <c>do reset</c> and the restart that follows
    /// it nothing is noted or recorded, so the save the reset takes writes the store empty rather than with the minutes in between.
    /// </summary>
    private static bool _sealed;

    /// <summary>Bumped by a reload or a wipe, so an answer asked for in the old world is not written into the new one.</summary>
    private static int _generation;

    private static long _startedTick;

    private static bool _started;

    /// <summary>Within <see cref="SettleMs"/> of the module's start: the walk builds but notes nothing, the books note no event.</summary>
    private static bool Booting(long now) => !_started || now - _startedTick < Math.Max(0, SettleMs);

    /// <summary>
    /// The module started: the boot's grace runs from here, and every bot in the world without a book has one opened now, so that an
    /// event after the grace finds a book whose first sight was taken before it rather than after (a bot found having just joined a
    /// guild would otherwise read "belonged to The Anchor" and "joined The Anchor" side by side). Only the missing are opened: at an
    /// ordinary restart that is nobody; on the first deploy it is the whole population, a few milliseconds once. Game loop only.
    /// </summary>
    public static void Started(long now)
    {
        _startedTick = now;
        _started = true;
        _sealed = false;

        if (!Running)
        {
            return;
        }

        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && !string.IsNullOrEmpty(bot.Name) && !_books.ContainsKey(bot.Name))
            {
                Book(bot.Name, bot);
            }
        }
    }

    /// <summary>What the dashboard reads: every story's JSON and the index, made on the loop and never changed after it is handed over.</summary>
    private sealed class Shelf
    {
        public static readonly Shelf Empty = new(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), "{\"stories\":[]}");

        public Shelf(Dictionary<string, string> stories, string index)
        {
            Stories = stories;
            Index = index;
        }

        public readonly Dictionary<string, string> Stories;

        public readonly string Index;
    }

    private static volatile Shelf _shelf = Shelf.Empty;

    private static BotStoryChapter[] NewChapters()
    {
        var chapters = new BotStoryChapter[BotStages.Count];

        for (var i = 0; i < chapters.Length; i++)
        {
            chapters[i] = new BotStoryChapter();
        }

        return chapters;
    }

    public static int Count => _books.Count;

    public static IReadOnlyDictionary<string, BotStoryBook> Books => _books;

    // ------------------------------------------------------------------ the page's side: any thread

    /// <summary>
    /// A bot's story as the last snapshot holds it, or the index of stories when no name is given; null for a name the snapshot
    /// has no story for. A read is posted to the loop, which builds that story now, counts the read and puts its chapters first for
    /// the model; a page's own refresh (<paramref name="poll"/>) of a story already on the shelf is not a read and posts nothing —
    /// counted as reads, an open Story tab made two a minute of every cache hit and request (a3 review B, 3.5). Any thread: it
    /// reads one volatile reference and posts; it touches no book.
    /// </summary>
    public static string Story(string name, bool poll = false)
    {
        var shelf = _shelf;

        if (string.IsNullOrWhiteSpace(name))
        {
            return shelf.Index;
        }

        name = name.Trim();

        var json = shelf.Stories.TryGetValue(name, out var found) ? found : null;

        if (!poll || json == null)
        {
            Core.LoopContext.Post(() => Wanted(name));
        }

        return json;
    }

    // ------------------------------------------------------------------ what comes in: game loop only

    /// <summary>
    /// Every event posted on the stream (<see cref="BotEvents.Heard"/>): the Chronicle takes what is history, the books what is
    /// a bot's own. Called about ten times a second; a kind of no interest costs a string switch.
    /// </summary>
    public static void Heard(BotEvent e)
    {
        if (!Running || _sealed || e?.Kind == null)
        {
            return;
        }

        // The stream is the loop's (BotEvents.Post says so), and every book and the Chronicle's page are the loop's alone: an event
        // posted from any other thread is dropped and counted here rather than written into them from the wrong side.
        if (Thread.CurrentThread != Core.Thread)
        {
            Interlocked.Increment(ref _offLoop);

            return;
        }

        try
        {
            BotChronicle.Heard(e);
            Noted(e);
        }
        catch (Exception ex)
        {
            Fault(ex, "an event");
        }
    }

    private static void Noted(BotEvent e)
    {
        var bot = e.Bot;
        var text = e.Text;

        // A boot's own settling (residences re-read, offices re-filled) is not a life's news; the Chronicle still takes the day's.
        if (bot == null || string.IsNullOrEmpty(text) || Booting(Core.TickCount))
        {
            return;
        }

        switch (e.Kind)
        {
            case "officers":
                {
                    if (text.StartsWith("joined ", StringComparison.Ordinal) || text.StartsWith("left ", StringComparison.Ordinal))
                    {
                        // The walk would see the same change of guild; the event says why, so it is the one kept, and the walk is
                        // told the guild now so that it does not write the change a second time in its own words.
                        Note(bot, BotStage.Guild, "guild", $"{bot} {text}", 40.0);

                        if (_books.TryGetValue(bot, out var book))
                        {
                            book.SeenGuild = e.Guild;
                        }
                    }
                    else if (text.StartsWith("appointed ", StringComparison.Ordinal))
                    {
                        Note(bot, BotStage.Guild, "office", $"{bot} was {text}", 45.0);
                    }
                    else if (text.StartsWith("won the guilds' bout", StringComparison.Ordinal))
                    {
                        Note(bot, BotStage.Deeds, "bout", $"{bot} {text}", 45.0);
                    }

                    break;
                }
            case "politics":
                {
                    if (text.Contains(" re-elected head of ", StringComparison.Ordinal))
                    {
                        break;
                    }

                    // A line goes into the book of the bot it names, never only of the bot that carried it. The steward's lines are
                    // carried by whoever said them in the town (BotStewardship.Speak: any member of the steward's guild standing there),
                    // and 47 of the 62 such lines in the stream named somebody else ("Dunstan Ashdown" carrying "Malachi of The Hammer is
                    // the steward of Britain now.") — written into the carrier's book, the model was then told to name the carrier as
                    // the steward (a3 review B, 3.1). So the steward is read off the sentence, and a head's election or deposition is
                    // kept only when the sentence names the bot it came with.
                    if (text.Contains(" elected head of ", StringComparison.Ordinal))
                    {
                        if (text.StartsWith($"{bot} elected head of ", StringComparison.Ordinal))
                        {
                            Note(bot, BotStage.Guild, "office", text.Replace(" elected head of ", " was elected head of ", StringComparison.Ordinal), 55.0);
                        }
                    }
                    else if (e.Channel == "steward")
                    {
                        if (StewardNamed(text, e.Guild) is { } steward)
                        {
                            Note(steward, BotStage.Guild, "office", BotChronicle.Steward(text), 50.0);
                        }
                        else if (DeposedSteward(text, e.Guild) is { } deposed)
                        {
                            Note(deposed, BotStage.Guild, "office", text, 50.0);
                        }
                    }
                    else if (text.Contains($" deposed {bot}", StringComparison.Ordinal))
                    {
                        Note(bot, BotStage.Guild, "office", text, 50.0);
                    }

                    break;
                }
            case "relic":
                {
                    Note(bot, BotStage.Deeds, "relic", $"{bot} {text}", 60.0);

                    break;
                }
            case "crime":
                {
                    // 123 such lines on 12.10.2026, a thief's every turn of the wheel: the turning points weigh more than each arrest.
                    if (text.StartsWith("stole ", StringComparison.Ordinal))
                    {
                        Note(bot, BotStage.Deeds, "crime", $"{bot} {text}", 55.0);
                    }
                    else if (text.StartsWith("recruited by ", StringComparison.Ordinal) || text.StartsWith("repentant ", StringComparison.Ordinal)
                             || text.StartsWith("ransomed by ", StringComparison.Ordinal))
                    {
                        Note(bot, BotStage.Deeds, "crime", $"{bot} was {text}", 35.0, own: true);
                    }
                    else if (text.StartsWith("caught by ", StringComparison.Ordinal) || text.StartsWith("posted as wanted ", StringComparison.Ordinal))
                    {
                        Note(bot, BotStage.Deeds, "crime", $"{bot} was {text}", 25.0, own: true);
                    }
                    else if (text.StartsWith("takes the bounty ", StringComparison.Ordinal))
                    {
                        Note(bot, BotStage.Deeds, "bounty", $"{bot} took the bounty {text["takes the bounty ".Length..]}", 30.0);
                    }

                    break;
                }
            case "residence":
                {
                    if (text.StartsWith("lives in ", StringComparison.Ordinal))
                    {
                        Note(bot, BotStage.Home, "home", $"{bot} came to live in {text["lives in ".Length..]}", 30.0);
                    }
                    else if (text.StartsWith("moving from ", StringComparison.Ordinal))
                    {
                        Note(bot, BotStage.Home, "home", $"{bot} moved from {text["moving from ".Length..]}", 30.0, own: true);
                    }

                    break;
                }
            case "house":
                {
                    // The guild's leader carries the event: "The Anchor settles in Britain at 1424,1687 for 5000gp".
                    var settled = text.Replace(" settles in ", " settled in ", StringComparison.Ordinal);

                    // Keyed by the sentence with its coordinates: two settlings in one town a day apart read the same once they
                    // are taken out of the words (a3 review B, 3.2).
                    Note(bot, BotStage.Guild, "hall", $"{settled}, with {bot} at its head", 40.0, key: text);

                    break;
                }
            case "infirmary":
            case "library":
            case "tavern":
            case "ragman":
                {
                    if (text.StartsWith("opened ", StringComparison.Ordinal))
                    {
                        Note(bot, BotStage.Apprenticeship, $"opened:{e.Kind}", $"{bot} {text}", 25.0, true);
                    }

                    break;
                }
            case "market":
                {
                    if (text.StartsWith("opened a shop ", StringComparison.Ordinal))
                    {
                        // "First" only for a bot seen born: for one found living the chronicle has seen its first shop since it
                        // began, not its first (148 bots opened 2022 shops on 12.10.2026; a3 review B, 3.3).
                        var first = _books.TryGetValue(bot, out var opener) && opener.Born ? "first " : "";

                        Note(bot, BotStage.Apprenticeship, "opened:shop", $"{bot} {first}{text}", 20.0, true);
                    }

                    break;
                }
        }
    }

    /// <summary>"Malachi of The Hammer is the steward of Britain now." → "Malachi" (<c>BotStewardship</c>'s chosen line); null for another line.</summary>
    private static string StewardNamed(string text, string guild)
    {
        var at = text.IndexOf(" is the steward of ", StringComparison.Ordinal);

        return at > 0 ? Before(text[..at], guild) : null;
    }

    /// <summary>"Britain deposed its steward Malachi of The Hammer: …" → "Malachi"; null for another line.</summary>
    private static string DeposedSteward(string text, string guild)
    {
        const string deposed = " deposed its steward ";

        var at = text.IndexOf(deposed, StringComparison.Ordinal);

        if (at < 0)
        {
            return null;
        }

        var rest = text[(at + deposed.Length)..];
        var colon = rest.IndexOf(':');

        return Before(colon > 0 ? rest[..colon] : rest, guild);
    }

    /// <summary>"Malachi Ashdown of The Hammer" → "Malachi Ashdown": the name before the guild, by the guild's own name when it is known.</summary>
    private static string Before(string who, string guild)
    {
        var of = guild != null && who.EndsWith($" of {guild}", StringComparison.Ordinal)
            ? who.Length - guild.Length - 4
            : who.LastIndexOf(" of ", StringComparison.Ordinal);

        return of > 0 ? who[..of] : null;
    }

    /// <summary>
    /// A note into a bot's book, by name — also the seam for what is to come (an apprenticeship of A5, a family of A7, a legend
    /// slain of A8). <paramref name="once"/> keeps only the first note of its kind. <paramref name="own"/>: the stream calls a bot
    /// "it" ("on its head", "after its term", "its trade"), and in a sentence about the bot that "its" is the bot's own, so it is
    /// said as his or her; not for sentences where "its" is a guild's ("at its herald's invitation"). <paramref name="key"/>: what tells
    /// this note from an earlier one when the words alone do not (the sentence before its coordinates were taken out). Returns the
    /// book, or null when the name is no bot's or the note was refused as one of a kind already kept; a note left out as said in
    /// the last day still returns the book. Game loop only.
    /// </summary>
    public static BotStoryBook Note(string name, BotStage stage, string kind, string text, double weight, bool once = false, bool own = false, string key = null)
    {
        if (!Running || _sealed || string.IsNullOrEmpty(name) || BotLives.Find(name) == null)
        {
            return null;
        }

        var book = Book(name, BotLives.Body(name));

        if (book == null)
        {
            return null;
        }

        if (once && book.Has(kind))
        {
            Repeats++;

            return null;
        }

        if (own)
        {
            text = text.Replace(" its ", $" {BotStoryWriter.Possessive(book)} ", StringComparison.Ordinal);
        }

        if (Write(book, stage, kind, text, weight, DateTime.UtcNow, key))
        {
            FromEvents++;
            Counted(kind);
        }

        return book;
    }

    /// <summary>
    /// A bot raised (<c>BotPopulation.Raise</c>): when its character was drawn fresh — a newcomer of growth, or everybody on the
    /// first boot after a wipe — its book opens with its birth; otherwise it is found as it stands. Game loop only.
    /// </summary>
    public static void Born(BotMobile bot, bool fresh)
    {
        if (!Running || _sealed || bot?.Name == null)
        {
            return;
        }

        try
        {
            // Found living (a name the characters remember): the walk opens its book once the body is in the world, where it stands.
            if (!fresh)
            {
                return;
            }

            if (_books.Remove(bot.Name))
            {
                _json.Remove(bot.Name);
            }

            var now = DateTime.UtcNow;
            var book = Open(bot, now);

            book.Born = true;

            var town = book.Town != null ? $" in {book.Town}" : "";

            Write(book, BotStage.Youth, "born", $"{bot.Name} was born {BotStoryWriter.A(Kind(book))}{town}", 100.0, now);
            BornNoted++;
        }
        catch (Exception ex)
        {
            Fault(ex, "a birth");
        }
    }

    /// <summary>The book for this name; opened when the bot is in the world, with where it stands now written as the chronicle's first sight of it.</summary>
    private static BotStoryBook Book(string name, BotMobile body)
    {
        if (_books.TryGetValue(name, out var book))
        {
            return book;
        }

        if (body == null || body.Deleted)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        book = Open(body, now);

        var town = book.Town != null ? $" living in {book.Town}" : "";

        Write(book, BotStage.Youth, "seen", $"{book.Name} was {BotStoryWriter.A(Kind(book))}{town}", 10.0, now);

        if (book.SeenGuild != null)
        {
            Write(book, BotStage.Guild, "seen", $"{book.Name} belonged to {book.SeenGuild}", 10.0, now);
        }

        if (Level(book.SeenRank) >= 2)
        {
            Write(book, BotStage.Apprenticeship, "seen", $"{book.Name} was already {BotStoryWriter.A(book.SeenRank)}", 10.0, now);
        }

        if (book.SeenHouse != null)
        {
            Write(book, BotStage.Home, "seen", $"{book.Name} had a house of {BotStoryWriter.Possessive(book)} own {book.SeenHouse}", 10.0, now);
        }

        if (book.SeenChampion)
        {
            Write(book, BotStage.Deeds, "seen", $"{book.Name} was the champion of the island", 10.0, now);
        }

        if (book.SeenVeteran)
        {
            Write(book, BotStage.Deeds, "seen", $"{book.Name} was already called {BotTraits.VeteranTitle}", 10.0, now);
        }

        return book;
    }

    /// <summary>A new book: what the bot is now, and what the walk is to compare against next time.</summary>
    private static BotStoryBook Open(BotMobile body, DateTime now)
    {
        var book = new BotStoryBook(body.Name)
        {
            FirstSeen = now,
            LastSeen = now,
            Female = body.Female,
            Class = body.Class?.Name,
            People = BotPeoples.Of(body.Name)?.Name,
            Town = Town(body),
            SeenGuild = (body.Guild as Guild)?.Name,
            SeenRank = body.BotRank,
            SeenHouse = House(body),
            SeenChampion = body.NameHue == BotMobile.ChampionHue,
            SeenVeteran = BotLives.Find(body.Name) is { Veteran: true }
        };

        book.OpenedTick = Core.TickCount;
        _books[book.Name] = book;
        Opened++;
        _changed = true;

        if (_books.Count > Math.Max(64, MostBooks))
        {
            Shed();
        }

        return book;
    }

    /// <summary>One book too many: the one longest unseen whose bot is not in the world goes.</summary>
    private static void Shed()
    {
        BotStoryBook oldest = null;

        foreach (var (_, book) in _books)
        {
            if (BotLives.Body(book.Name) == null && (oldest == null || book.LastSeen < oldest.LastSeen))
            {
                oldest = book;
            }
        }

        if (oldest != null)
        {
            _books.Remove(oldest.Name);
            _json.Remove(oldest.Name);
        }
    }

    /// <summary>A note written, or left out (false) as the same thing said in the last day; only a written note is counted.</summary>
    private static bool Write(BotStoryBook book, BotStage stage, string kind, string text, double weight, DateTime at, string key = null)
    {
        text = Plain(text);

        if (book.Said(text, key, at))
        {
            Repeats++;

            return false;
        }

        book.Notes.Add(new BotStoryNote { Stage = stage, Kind = kind, Text = text, Key = key, At = at, Weight = weight });
        _changed = true;

        while (book.Notes.Count > Math.Max(BotStages.Count, MostNotes))
        {
            Trim(book);
        }

        return true;
    }

    /// <summary>Notes from events and the walk by kind, for the line: "from events" alone passes on shop openings (a3 review B, §4).</summary>
    private static void Counted(string kind) => _byKind[kind] = _byKind.GetValueOrDefault(kind) + 1;

    /// <summary>The lightest note goes, but never the last of its stage while another stage has two; ties go to the older.</summary>
    private static void Trim(BotStoryBook book)
    {
        Span<int> perStage = stackalloc int[BotStages.Count];

        for (var i = 0; i < book.Notes.Count; i++)
        {
            perStage[(int)book.Notes[i].Stage]++;
        }

        var drop = -1;

        for (var pass = 0; pass < 2 && drop < 0; pass++)
        {
            for (var i = 0; i < book.Notes.Count; i++)
            {
                var note = book.Notes[i];

                if ((pass == 0 && perStage[(int)note.Stage] < 2) || note.Kind == "born")
                {
                    continue;
                }

                if (drop < 0 || note.Weight < book.Notes[drop].Weight)
                {
                    drop = i;
                }
            }
        }

        book.Notes.RemoveAt(drop < 0 ? 0 : drop);
        Trimmed++;
    }

    // ------------------------------------------------------------------ the walk

    /// <summary>The story's clock. Game loop only.</summary>
    public static void Tick(long now)
    {
        if (!Running || _sealed)
        {
            return;
        }

        try
        {
            Walk(now);
            BotChronicle.Tick(now);
            Voice(now);
            Publish(now, false);
        }
        catch (Exception ex)
        {
            Fault(ex, "the clock");
        }
    }

    private static void Walk(long now)
    {
        var bots = BotPopulation.Bots;

        if (bots.Count == 0)
        {
            return;
        }

        var many = (int)Math.Ceiling(bots.Count * (double)Math.Max(250, TickMs) / Math.Max(TickMs, EveryMs));

        for (var n = 0; n < Math.Max(1, many); n++)
        {
            if (_cursor >= bots.Count)
            {
                _cursor = 0;
            }

            var bot = bots[_cursor++];

            if (bot is not { Deleted: false } || string.IsNullOrEmpty(bot.Name))
            {
                continue;
            }

            Look(bot);
        }
    }

    /// <summary>One bot looked at: what changed since the last look is noted, and its story is built.</summary>
    private static void Look(BotMobile bot)
    {
        Looks++;

        var book = Book(bot.Name, bot);

        if (book == null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var life = BotLives.Find(bot.Name);

        book.LastSeen = now;

        // Born at boot, before the residences were read, the birth had no town: the town it was made a resident of is where it was
        // born (BotResidence.Raised runs before the body is put down), so the first look writes it in.
        if (book.Born && book.Town == null && BotResidence.NameOf(bot) is { } town)
        {
            book.Town = town;

            for (var i = 0; i < book.Notes.Count; i++)
            {
                if (book.Notes[i].Kind == "born")
                {
                    book.Notes[i].Text = $"{book.Name} was born {BotStoryWriter.A(Kind(book))} in {town}";
                }
            }
        }

        // Its sex is set by the newcomer's caller after it is raised (BotPopulation.RaiseNewcomer), after the book was opened.
        book.Female = bot.Female;

        var tick = Core.TickCount;

        if (book.OpenedTick is { } opened)
        {
            // A just-opened book takes what it sees as where the bot stands on every look within the settle, and on the first look
            // after it too: books are opened at the module's start, before any bot has had the beat that works out its rank, and
            // the walk comes round once in five minutes — a settle that ended by the clock alone left three bots in five to be
            // looked at first after it, and the rank they had all along would have been written as "became a Journeyman".
            Absorb(book, bot, life, now);

            // A bot born under the chronicle has no standing to take in but its rank: the guild it founds or joins in its first
            // minutes is news. After a wipe that is every guild of the new world, founded within minutes of the boot.
            if (book.Born && !Booting(tick))
            {
                Guilds(book, bot, now);
            }

            if (tick - opened >= Math.Max(0, SettleMs))
            {
                book.OpenedTick = null;
            }
        }
        else if (!Booting(tick))
        {
            Guilds(book, bot, now);
            Rank(book, bot, now);
            Houses(book, bot, now);
            Champion(book, bot, now);

            if (life is { Veteran: true } && !book.SeenVeteran)
            {
                book.SeenVeteran = true;
                Walked(book, BotStage.Deeds, "veteran", $"{book.Name} was called {BotTraits.VeteranTitle} from then on, for {BotTraits.VeteranKills} kills and more", 55.0, life.VeteranAt == default ? now : life.VeteranAt);
            }
        }

        Build(book, life, bot, false);
    }

    /// <summary>
    /// A book just opened: what the walk sees is where the bot stands, not news. For a bot found living it goes into the chronicle's
    /// first sight of it ("seen" notes, replaced as the picture fills in); for a bot born under the chronicle it is only remembered,
    /// its birth being the whole of its story so far — all but its guild, which the walk notes as news (see <see cref="Look"/>).
    /// </summary>
    private static void Absorb(BotStoryBook book, BotMobile bot, BotLife life, DateTime now)
    {
        var guild = (bot.Guild as Guild)?.Name;

        if (!book.Born && !string.Equals(guild, book.SeenGuild, StringComparison.Ordinal))
        {
            book.SeenGuild = guild;
            Seen(book, BotStage.Guild, guild != null ? $"{book.Name} belonged to {guild}" : null, now);
        }

        var rank = bot.BotRank;

        if (!string.IsNullOrEmpty(rank) && Level(rank) > Level(book.SeenRank))
        {
            book.SeenRank = rank;
            Seen(book, BotStage.Apprenticeship, Level(rank) >= 2 ? $"{book.Name} was already {BotStoryWriter.A(rank)}" : null, now);
        }

        var house = House(bot);

        if (!string.Equals(house, book.SeenHouse, StringComparison.Ordinal))
        {
            book.SeenHouse = house;
            Seen(book, BotStage.Home, house != null ? $"{book.Name} had a house of {BotStoryWriter.Possessive(book)} own {house}" : null, now);
        }

        var champion = bot.NameHue == BotMobile.ChampionHue;

        if (champion != book.SeenChampion)
        {
            book.SeenChampion = champion;
            Seen(book, BotStage.Deeds, champion ? $"{book.Name} was the champion of the island" : null, now, "champion");
        }

        if (life is { Veteran: true } && !book.SeenVeteran)
        {
            book.SeenVeteran = true;
            Seen(book, BotStage.Deeds, $"{book.Name} was already called {BotTraits.VeteranTitle}", now, "called");
        }
    }

    /// <summary>
    /// The first-sight note of a stage written, replaced or (null) taken out; nothing for a bot seen born. The deeds stage holds two
    /// such notes (the champion, the veteran), told apart by a word of theirs (<paramref name="about"/>).
    /// </summary>
    private static void Seen(BotStoryBook book, BotStage stage, string text, DateTime now, string about = null)
    {
        if (book.Born)
        {
            return;
        }

        for (var i = 0; i < book.Notes.Count; i++)
        {
            var note = book.Notes[i];

            if (note.Kind != "seen" || note.Stage != stage || (about != null && !note.Text.Contains(about, StringComparison.Ordinal)))
            {
                continue;
            }

            if (text == null)
            {
                book.Notes.RemoveAt(i);
            }
            else
            {
                note.Text = Plain(text);
            }

            _changed = true;

            return;
        }

        if (text != null)
        {
            Write(book, stage, "seen", text, 10.0, now);
        }
    }

    private static void Walked(BotStoryBook book, BotStage stage, string kind, string text, double weight, DateTime at)
    {
        if (Write(book, stage, kind, text, weight, at))
        {
            FromWalk++;
            Counted(kind);
        }
    }

    private static void Guilds(BotStoryBook book, BotMobile bot, DateTime now)
    {
        var guild = (bot.Guild as Guild)?.Name;

        if (string.Equals(guild, book.SeenGuild, StringComparison.Ordinal))
        {
            return;
        }

        var old = book.SeenGuild;
        var tick = Core.TickCount;
        var founded = guild != null && string.Equals(BotChronicle.FounderOf(guild, tick, 3600000), book.Name, StringComparison.Ordinal);

        book.SeenGuild = guild;

        if (old == null)
        {
            Walked(book, BotStage.Guild, founded ? "founded" : "guild", founded ? $"{book.Name} founded {guild}" : $"{book.Name} joined {guild}", founded ? 60.0 : 40.0, now);
        }
        else if (guild == null)
        {
            Walked(book, BotStage.Guild, "guild", BotChronicle.ClosedLately(old, tick, 3600000) ? $"{book.Name}'s guild, {old}, closed" : $"{book.Name} left {old}", 35.0, now);
        }
        else
        {
            Walked(book, BotStage.Guild, founded ? "founded" : "guild", founded ? $"{book.Name} left {old} and founded {guild}" : $"{book.Name} left {old} for {guild}", founded ? 60.0 : 40.0, now);
        }
    }

    /// <summary>A rank written when it rises to a level not reached before, from Apprentice up; the novice's first steps are not a story.</summary>
    private static void Rank(BotStoryBook book, BotMobile bot, DateTime now)
    {
        var rank = bot.BotRank;

        if (string.IsNullOrEmpty(rank) || string.Equals(rank, book.SeenRank, StringComparison.Ordinal))
        {
            return;
        }

        var level = Level(rank);

        if (level <= Level(book.SeenRank))
        {
            return;
        }

        book.SeenRank = rank;

        if (level >= 2)
        {
            Walked(book, BotStage.Apprenticeship, "rank", $"{book.Name} became {BotStoryWriter.A(rank)}", 15.0 + 5.0 * level, now);
        }
    }

    private static readonly string[] Levels =
        ["Neophyte", "Novice", "Apprentice", "Journeyman", "Expert", "Adept", "Master", "Grandmaster", "Elder", "Legendary"];

    /// <summary>The level of a skill title, 0 for Neophyte to 9 for Legendary; −1 for none. The engine's own words (<c>Titles</c>).</summary>
    public static int Level(string rank)
    {
        if (string.IsNullOrEmpty(rank))
        {
            return -1;
        }

        var space = rank.IndexOf(' ');
        var word = space > 0 ? rank[..space] : rank;

        for (var i = Levels.Length - 1; i >= 0; i--)
        {
            if (string.Equals(word, Levels[i], StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        // The samurai's and the ninja's words for the top two.
        return word is "Tatsujin" or "Shinobi" ? 8 : word is "Kengo" or "Ka-ge" ? 9 : -1;
    }

    private static void Houses(BotStoryBook book, BotMobile bot, DateTime now)
    {
        var place = House(bot);

        if (string.Equals(place, book.SeenHouse, StringComparison.Ordinal))
        {
            return;
        }

        var old = book.SeenHouse;

        book.SeenHouse = place;

        if (place == null)
        {
            Walked(book, BotStage.Home, "house", $"{book.Name} no longer had the house {old}", 30.0, now);
        }
        else if (old == null)
        {
            Walked(book, BotStage.Home, "house", $"{book.Name} bought a house of {BotStoryWriter.Possessive(book)} own {place}", 50.0, now);
        }
        else
        {
            Walked(book, BotStage.Home, "house", $"{book.Name} moved into a house {place}", 45.0, now);
        }
    }

    /// <summary>The champion wears the champion's yellow (<c>BotTourney.Crowned</c>, every minute): read here, where the minds assembly cannot be.</summary>
    private static void Champion(BotStoryBook book, BotMobile bot, DateTime now)
    {
        var champion = bot.NameHue == BotMobile.ChampionHue;

        if (champion == book.SeenChampion)
        {
            return;
        }

        book.SeenChampion = champion;

        if (!champion)
        {
            // Lost only what the book says was held: a yellow taken in a forced test was never written (see TestCrowned).
            if (HeldTitle(book))
            {
                Walked(book, BotStage.Deeds, "champion", $"{book.Name} lost the champion's title", 40.0, now);
            }

            return;
        }

        var guild = (bot.Guild as Guild)?.Name;

        Walked(book, BotStage.Deeds, "champion", $"{book.Name} became the champion of the island", 70.0, now);
        BotChronicle.Record("champion", $"{book.Name} the {BotStoryWriter.Words(bot.Class?.Name)}{(guild != null ? $" of {guild}" : "")} became the champion of the island", 75.0);
    }

    /// <summary>Whether the book's last word on the title is that the bot held it (crowned, or found champion), not that it lost it.</summary>
    private static bool HeldTitle(BotStoryBook book)
    {
        for (var i = book.Notes.Count - 1; i >= 0; i--)
        {
            var note = book.Notes[i];

            if (note.Kind == "champion")
            {
                return !note.Text.Contains("lost the champion's title", StringComparison.Ordinal);
            }

            if (note.Baseline && note.Text.Contains("was the champion of the island", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A forced test crowned this bot on a vacant title (<c>BotTourney.Crown</c>): it wears the yellow, but a test is not a
    /// championship, so nothing is written — the book is only told the yellow is seen, so that the walk, which reads the hue, does
    /// not write the crown five minutes later, nor its loss when a real championship takes it (a3 review B, §2). A book not yet
    /// open is not opened for it. Game loop only.
    /// </summary>
    public static void TestCrowned(BotMobile bot)
    {
        if (!Running || _sealed || bot?.Name == null || !_books.TryGetValue(bot.Name, out var book))
        {
            return;
        }

        book.SeenChampion = true;
        TestCrowns++;
    }

    /// <summary>
    /// The championship's own word that a bot was crowned (<c>BotTourney.Crown</c>, in the minds assembly, which this one cannot
    /// see): the Chronicle's entry at the minute it happened and in the tourney's words of how ("took the title from Faron in the
    /// ring"), and the bot's own note. The book is told the yellow is seen, so the walk, which would find it up to
    /// <see cref="EveryMs"/> later, writes nothing more; the walk stays the way in for a crown this call does not report.
    /// A forced test is not a championship and is not reported. Game loop only.
    /// </summary>
    public static void Crowned(BotMobile bot, string how)
    {
        if (!Running || _sealed || bot?.Name == null)
        {
            return;
        }

        try
        {
            var guild = (bot.Guild as Guild)?.Name;
            var said = string.IsNullOrWhiteSpace(how) ? "became" : $"{how.Trim()} and became";

            BotChronicle.Record("champion", $"{bot.Name} the {BotStoryWriter.Words(bot.Class?.Name)}{(guild != null ? $" of {guild}" : "")} {said} the champion of the island", 75.0);

            if (Note(bot.Name, BotStage.Deeds, "champion", $"{bot.Name} {said} the champion of the island", 70.0) is { } book)
            {
                book.SeenChampion = true;
            }
        }
        catch (Exception ex)
        {
            Fault(ex, "a crowning");
        }
    }

    /// <summary>A champion who kept the title in the ring (<c>BotTourney</c>): a line of the Chronicle and a deed of the champion's. Game loop only.</summary>
    public static void Defended(string champion, string challenger, int defences)
    {
        if (!Running || _sealed || champion == null)
        {
            return;
        }

        try
        {
            var against = challenger != null ? $" against {challenger}" : "";

            BotChronicle.Record("champion", $"{champion} defended the title of champion{against}, {defences} {(defences == 1 ? "defence" : "defences")} in this reign", 45.0);
            Note(champion, BotStage.Deeds, "defence", $"{champion} defended the title of champion{against}", 45.0);
        }
        catch (Exception ex)
        {
            Fault(ex, "a title defence");
        }
    }

    private static string House(BotMobile bot) =>
        BotAbode.Of(bot) is { Deleted: false } house ? BotBiography.Place(house.Map, house.Location) : null;

    /// <summary>The bot's town of residence, else the town nearest where it stands — but never for a body not yet put down in the world.</summary>
    private static string Town(BotMobile bot) =>
        BotResidence.NameOf(bot)
        ?? (bot.Map != null && bot.Map != Map.Internal && bot.Location != Point3D.Zero ? BotTowns.Nearest(bot.Location)?.Name : null);

    /// <summary>"dwarf warrior", "human tailor": the people when the names file knows it, and the class as a word.</summary>
    private static string Kind(BotStoryBook book)
    {
        var klass = BotStoryWriter.Words(book.Class);

        return string.IsNullOrEmpty(book.People) ? klass : $"{book.People.ToLowerInvariant()} {klass}";
    }

    // ------------------------------------------------------------------ building

    /// <summary>
    /// A story built by template into the loop's working copy, timed. <paramref name="read"/>: a person asked for it, so the
    /// chapters shown in the model's words or the template's are counted as cache hits and misses.
    /// </summary>
    private static void Build(BotStoryBook book, BotLife life, BotMobile body, bool read)
    {
        var started = Stopwatch.GetTimestamp();
        var now = DateTime.UtcNow;
        var count = BotStoryWriter.Build(book, life, body, now, _chapters);
        var lived = 0;
        var unvoiced = 0;

        for (var s = 0; s < _chapters.Length; s++)
        {
            var chapter = _chapters[s];

            if (!chapter.Present)
            {
                continue;
            }

            if (chapter.Lived)
            {
                lived++;
            }

            if (Voiceable(book, chapter))
            {
                unvoiced++;
            }

            if (read && chapter.Lived && chapter.Facts.Count >= Math.Max(1, VoiceLeastFacts))
            {
                if (chapter.Voiced != null)
                {
                    CacheHits++;
                }
                else
                {
                    CacheMisses++;
                }
            }
        }

        book.Chapters = count;
        book.Lived = lived;
        book.Unvoiced = unvoiced;

        _json[book.Name] = Json(book, life, body, count, lived);
        _changed = true;

        var spent = Stopwatch.GetTimestamp() - started;

        Builds++;
        BuildTicks += spent;

        if (spent > BuildMostTicks)
        {
            BuildMostTicks = spent;
        }
    }

    /// <summary>A chapter the model may retell and has not, for these facts (a refusal is kept too, so the same facts are never asked twice).</summary>
    private static bool Voiceable(BotStoryBook book, BotStoryChapter chapter) =>
        chapter.Present && chapter.Lived && chapter.Facts.Count >= Math.Max(1, VoiceLeastFacts)
        && (book.Voiced[(int)chapter.Stage] is not { } voiced || voiced.Hash != chapter.Hash);

    private static string Json(BotStoryBook book, BotLife life, BotMobile body, int count, int lived) =>
        BotJson.Object(
            w =>
            {
                w.WriteString("bot", book.Name);
                w.WriteString("class", body?.Class?.Name ?? book.Class);
                BotJson.StringOrNull(w, "people", book.People);
                BotJson.StringOrNull(w, "guild", (body?.Guild as Guild)?.Name ?? book.SeenGuild);
                BotJson.StringOrNull(w, "rank", body?.BotRank ?? book.SeenRank);
                w.WriteBoolean("born", book.Born);
                w.WriteString("firstSeen", book.FirstSeen.ToLocalTime().ToString("yyyy-MM-dd HH:mm", Invariant));
                w.WriteString("built", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", Invariant));
                w.WriteBoolean("inWorld", body is { Deleted: false });
                w.WriteNumber("count", count);
                w.WriteNumber("lived", lived);
                w.WriteNumber("notes", book.Notes.Count);
                w.WriteBoolean("wanted", _wanted.Contains(book.Name));
                w.WritePropertyName("chapters");
                w.WriteStartArray();

                for (var s = 0; s < _chapters.Length; s++)
                {
                    var chapter = _chapters[s];

                    if (!chapter.Present)
                    {
                        continue;
                    }

                    w.WriteStartObject();
                    w.WriteString("stage", BotStages.Key(chapter.Stage));
                    w.WriteString("title", BotStages.Title(chapter.Stage));
                    w.WriteBoolean("lived", chapter.Lived);
                    BotJson.StringOrNull(w, "text", chapter.Text);
                    BotJson.StringOrNull(w, "voiced", chapter.Voiced);
                    BotJson.StringOrNull(w, "tally", chapter.Tally);
                    w.WritePropertyName("facts");
                    w.WriteStartArray();

                    for (var i = 0; i < chapter.Facts.Count; i++)
                    {
                        w.WriteStringValue(chapter.Facts[i]);
                    }

                    w.WriteEndArray();
                    w.WriteEndObject();
                }

                w.WriteEndArray();
            }
        );

    /// <summary>A name asked for on the page (posted from the listener): built now, handed over now, and its chapters put first for the model.</summary>
    private static void Wanted(string name)
    {
        if (!Running || _sealed)
        {
            return;
        }

        try
        {
            Requests++;

            var body = BotLives.Body(name);
            var real = body?.Name ?? Real(name);

            if (real == null || BotLives.Find(real) == null)
            {
                Unknown++;

                return;
            }

            var book = Book(real, body);

            if (book == null)
            {
                Unknown++;

                return;
            }

            _wanted.Remove(real);
            _wanted.Add(real);

            while (_wanted.Count > Math.Max(1, MostWanted))
            {
                _wanted.RemoveAt(0);
            }

            Build(book, BotLives.Find(real), body, true);
            Publish(Core.TickCount, true);
        }
        catch (Exception ex)
        {
            Fault(ex, "a story asked for");
        }
    }

    /// <summary>A name as the page typed it, matched to a book's without regard to case.</summary>
    private static string Real(string name)
    {
        if (_books.ContainsKey(name))
        {
            return name;
        }

        foreach (var (key, _) in _books)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }
        }

        return null;
    }

    /// <summary>
    /// The snapshot the page reads: a copy of every story's JSON and the index, swapped in whole. A story asked for on the page
    /// forces it, but never twice within a second — the copy and the index are a few hundred microseconds at 1500 books, and a
    /// page left open, or a script asking in a loop, must not turn them into a cost per request; a force refused is owed, and the
    /// story's next tick pays it.
    /// </summary>
    private static void Publish(long now, bool force)
    {
        force |= _owed;

        if (!_changed || _publishedOnce && now - _publishedTick < (force ? 1000 : Math.Max(1000, PublishEveryMs)))
        {
            _owed = force && _changed;

            return;
        }

        _owed = false;
        _changed = false;
        _publishedOnce = true;
        _publishedTick = now;
        Published++;

        _shelf = new Shelf(new Dictionary<string, string>(_json, StringComparer.OrdinalIgnoreCase), Index());
    }

    /// <summary>The stories' index for the page: every book with its chapters, the fullest first, at most two hundred.</summary>
    private static string Index()
    {
        var list = new List<BotStoryBook>(_books.Count);

        foreach (var (_, book) in _books)
        {
            if (book.Chapters > 0)
            {
                list.Add(book);
            }
        }

        list.Sort(static (a, b) => b.Lived != a.Lived ? b.Lived.CompareTo(a.Lived) : b.Notes.Count.CompareTo(a.Notes.Count));

        return BotJson.Object(
            w =>
            {
                w.WriteString("at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", Invariant));
                w.WriteNumber("books", _books.Count);
                w.WritePropertyName("stories");
                w.WriteStartArray();

                for (var i = 0; i < list.Count && i < 200; i++)
                {
                    var book = list[i];

                    w.WriteStartObject();
                    w.WriteString("bot", book.Name);
                    BotJson.StringOrNull(w, "class", book.Class);
                    BotJson.StringOrNull(w, "guild", book.SeenGuild);
                    w.WriteNumber("chapters", book.Chapters);
                    w.WriteNumber("lived", book.Lived);
                    w.WriteNumber("notes", book.Notes.Count);
                    w.WriteBoolean("born", book.Born);
                    w.WriteEndObject();
                }

                w.WriteEndArray();
            }
        );
    }

    // ------------------------------------------------------------------ the model

    /// <summary>
    /// The model's turn: when it is allowed (on, wired, not asking, under <see cref="VoicePerHour"/> in the last hour, spaced
    /// evenly unless a reader asked), the next chapter or day it should retell, asked through <see cref="Ask"/>. Game loop only.
    /// </summary>
    private static void Voice(long now)
    {
        if (_asking && now - _askedTick >= 600000)
        {
            // An answer is always posted back, even a refusal; ten minutes without one means it was lost with a reload.
            _asking = false;
            VoiceLost++;
        }

        // Not while a save is pending, taken or written (CLAUDE.md rule 3): the call itself touches nothing, but it waits its turn.
        if (!Voiced || _asking || Ask is not { } ask || World.WorldState != WorldState.Running || VoicePerHour <= 0)
        {
            return;
        }

        while (_asks.Count > 0 && now - _asks.Peek() >= 3600000)
        {
            _asks.Dequeue();
        }

        if (_asks.Count >= VoicePerHour)
        {
            return;
        }

        if (_wanted.Count == 0 && _asks.Count > 0 && now - _askedTick < 3600000 / VoicePerHour)
        {
            return;
        }

        if (!Next(out var q, out var key, out var stage, out var hash))
        {
            return;
        }

        var generation = _generation;

        // Marked before the call, not after: an answering side that calls back at once (a refusal) clears the mark itself, and a
        // mark set after it would stand for ten minutes over nothing.
        var before = _askedTick;

        _asking = true;
        _askedTick = now;

        if (!ask(q, (text, waited) => Answered(generation, q, key, stage, hash, text, waited)))
        {
            _asking = false;
            _askedTick = before;
            VoiceBusy++;

            return;
        }

        _asks.Enqueue(now);
        VoiceAsked++;
    }

    /// <summary>What to retell next: a chapter of a story somebody asked for, else the newest closed day, else the next living bot's.</summary>
    private static bool Next(out BotStoryAsk q, out string key, out int stage, out ulong hash)
    {
        // Asked for on the page, the newest ask first.
        for (var i = _wanted.Count - 1; i >= 0; i--)
        {
            var name = _wanted[i];

            if (_books.TryGetValue(name, out var book) && Chapter(book, out q, out stage, out hash))
            {
                key = name;

                return true;
            }

            _wanted.RemoveAt(i);
        }

        if (BotChronicle.Unvoiced() is { } day)
        {
            var facts = BotChronicle.Facts(day);

            key = day.Key;
            stage = -1;
            hash = BotStoryWriter.Hash(facts);
            q = new BotStoryAsk
            {
                Kind = "day",
                Subject = BotChronicle.Title(day),
                Title = "The Chronicle of the island",
                Who = $"the island, on {BotChronicle.Title(day)}",
                Facts = string.Join('\n', facts),
                MostWords = VoiceMostWords * 2,
                Model = VoiceModel
            };

            return true;
        }

        // The living, in turn, fifty a look: a book the last build found with a chapter to retell.
        var bots = BotPopulation.Bots;

        for (var n = 0; n < 50 && bots.Count > 0; n++)
        {
            if (_voiceCursor >= bots.Count)
            {
                _voiceCursor = 0;
            }

            var bot = bots[_voiceCursor++];

            if (bot?.Name != null && _books.TryGetValue(bot.Name, out var book) && book.Unvoiced > 0 && Chapter(book, out q, out stage, out hash))
            {
                key = book.Name;

                return true;
            }
        }

        q = null;
        key = null;
        stage = -1;
        hash = 0;

        return false;
    }

    /// <summary>The first chapter of this book the model may retell, built fresh so its facts are the present ones.</summary>
    private static bool Chapter(BotStoryBook book, out BotStoryAsk q, out int stage, out ulong hash)
    {
        var body = BotLives.Body(book.Name);
        var life = BotLives.Find(book.Name);

        BotStoryWriter.Build(book, life, body, DateTime.UtcNow, _chapters);

        for (var s = 0; s < _chapters.Length; s++)
        {
            var chapter = _chapters[s];

            if (!Voiceable(book, chapter))
            {
                continue;
            }

            stage = s;
            hash = chapter.Hash;

            // Who without the guild: the guild named here is the present one, and told it, the model could retell a chapter about
            // a former guild with the present one and the check would let it pass (a3 review B, 3.4b). A guild the chapter is
            // about is in its facts.
            q = new BotStoryAsk
            {
                Kind = "chapter",
                Subject = book.Name,
                Title = BotStages.Title(chapter.Stage),
                Who = $"{book.Name}, {BotStoryWriter.A(Kind(book))}",
                Facts = string.Join('\n', chapter.Facts),
                MostWords = VoiceMostWords,
                Model = VoiceModel
            };

            return true;
        }

        book.Unvoiced = 0;
        q = null;
        stage = -1;
        hash = 0;

        return false;
    }

    /// <summary>The model's answer, back on the loop: checked, and kept against the facts' hash, or thrown away with the reason.</summary>
    private static void Answered(int generation, BotStoryAsk q, string key, int stage, ulong hash, string text, long waited)
    {
        // An answer asked for before a reload or a wipe frees nothing: the slot mark belongs to whatever was asked since.
        if (generation != _generation)
        {
            VoiceStale++;

            return;
        }

        _asking = false;

        try
        {
            VoiceWaitedMs += waited;

            if (text == null)
            {
                // Unreachable, timed out or unreadable: nothing is marked, so the same facts may be asked again in a later turn.
                VoiceNone++;

                return;
            }

            VoiceAnswered++;

            // What the model was told is what it may say: the facts, who they are about, and the chapter's title.
            var told = $"{q.Facts}\n{q.Who}\n{q.Title}\n{q.Subject}";
            var kept = BotStoryWriter.Check(text, told, q.Kind == "chapter" ? q.Subject : null, VoiceMostChars, out var why, out var word);

            if (kept == null)
            {
                _thrown[why] = _thrown.GetValueOrDefault(why) + 1;

                logger.Information(
                    "Story: the model's retelling of {Subject} ({Title}) was thrown away for {Why}{Word} after {Ms}ms; the template stands",
                    q.Subject,
                    q.Title,
                    why,
                    word != null ? $" (\"{word}\")" : "",
                    waited
                );
            }
            else
            {
                VoiceKept++;

                logger.Information("Story: the model retold {Subject} ({Title}) in {Ms}ms: \"{Text}\"", q.Subject, q.Title, waited, kept);
            }

            // A refusal is kept as a marker against the same hash: the same facts are not asked again.
            var prose = new BotStoryProse { Hash = hash, Text = kept, At = DateTime.UtcNow };

            if (stage < 0)
            {
                BotChronicle.Voice(key, hash, kept);

                return;
            }

            if (_books.TryGetValue(key, out var book))
            {
                book.Voiced[stage] = prose;
                Build(book, BotLives.Find(key), BotLives.Body(key), false);
            }
        }
        catch (Exception ex)
        {
            Fault(ex, "the model's answer");
        }
    }

    // ------------------------------------------------------------------ words

    /// <summary>
    /// A sentence of the event stream made fit for a story: coordinates taken out (" at (1214, 632)", " at 1424,1687",
    /// " (1214, 632)"), which the log needs and a reader does not.
    /// </summary>
    public static string Plain(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf(',') < 0)
        {
            return text;
        }

        using var sb = ValueStringBuilder.Create(text.Length);

        var i = 0;

        while (i < text.Length)
        {
            var skip = Coordinates(text, i);

            if (skip > 0)
            {
                i += skip;

                continue;
            }

            sb.Append(text[i]);
            i++;
        }

        return sb.ToString();
    }

    /// <summary>The length of a coordinate fragment starting at <paramref name="i"/>, or 0.</summary>
    private static int Coordinates(string text, int i)
    {
        var j = i;

        if (string.CompareOrdinal(text, j, " at ", 0, 4) == 0)
        {
            j += 4;
        }
        else if (text[j] == ' ' && j + 1 < text.Length && text[j + 1] == '(')
        {
            j += 1;
        }
        else
        {
            return 0;
        }

        var open = j < text.Length && text[j] == '(';

        if (open)
        {
            j++;
        }

        var digits = j;

        while (j < text.Length && char.IsDigit(text[j]))
        {
            j++;
        }

        if (j == digits || j >= text.Length || text[j] != ',')
        {
            return 0;
        }

        j++;

        if (j < text.Length && text[j] == ' ')
        {
            j++;
        }

        digits = j;

        while (j < text.Length && char.IsDigit(text[j]))
        {
            j++;
        }

        if (j == digits)
        {
            return 0;
        }

        // A third number (z) when there is one.
        if (open && j < text.Length && text[j] == ',')
        {
            var k = j + 1;

            if (k < text.Length && text[k] == ' ')
            {
                k++;
            }

            var z = k;

            while (k < text.Length && (char.IsDigit(text[k]) || text[k] == '-'))
            {
                k++;
            }

            if (k > z)
            {
                j = k;
            }
        }

        if (open)
        {
            if (j >= text.Length || text[j] != ')')
            {
                return 0;
            }

            j++;
        }

        return j - i;
    }

    private static void Fault(Exception ex, string what)
    {
        Faults++;

        if (Faults <= 3 || Faults % 100 == 0)
        {
            logger.Error(ex, "Story: {What} could not be taken in ({Times} times so far)", what, Faults);
        }
    }

    // ------------------------------------------------------------------ the line and the page

    /// <summary>The <c>Stories:</c> line.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "the stories are off";
        }

        var three = 0;
        var threeLived = 0;
        var born = 0;
        Span<int> stages = stackalloc int[BotStages.Count];
        var voiced = 0;
        var voiceable = 0;

        foreach (var (_, book) in _books)
        {
            three += book.Chapters >= 3 ? 1 : 0;
            threeLived += book.Lived >= 3 ? 1 : 0;
            born += book.Born ? 1 : 0;
            voiceable += book.Unvoiced;

            for (var s = 0; s < book.Voiced.Length; s++)
            {
                voiced += book.Voiced[s] is { Text: not null } ? 1 : 0;
            }
        }

        Stages(stages);

        var hour = 0;
        var now = Core.TickCount;

        foreach (var at in _asks)
        {
            hour += now - at < 3600000 ? 1 : 0;
        }

        var thrown = "";

        foreach (var (why, n) in _thrown)
        {
            thrown += (thrown.Length == 0 ? "" : ", ") + $"{why} {n}";
        }

        var mean = Builds == 0 ? 0.0 : BuildTicks * 1000000.0 / Stopwatch.Frequency / Builds;
        var most = BuildMostTicks * 1000000.0 / Stopwatch.Frequency;
        var books = Math.Max(1, _books.Count);

        return $"{_books.Count} stories ({born} born under this chronicle, {_books.Count - born} found living); {three} with 3 chapters or more ({100.0 * three / books:F0}%), "
               + $"{threeLived} with 3 or more of things seen to happen ({100.0 * threeLived / books:F0}%); chapters: youth {stages[0]}, apprenticeship {stages[1]}, guild {stages[2]}, deeds {stages[3]}, "
               + $"friends {stages[4]}, home {stages[5]}, family {stages[6]}, old age {stages[7]}; notes: {BornNoted} births, {FromEvents} from events, {FromWalk} from the walk (by kind: {ByKind()}), "
               + $"{Trimmed} trimmed, {Repeats} repeats left out; {Looks} looks, built {Builds} times at {mean:F0}µs a story ({most:F0}µs the longest), "
               + $"handed to the page {Published} times, asked for there {Requests} times ({Unknown} unknown); "
               + $"the model: {(Voiced ? Ask == null ? "on but not wired" : $"on, at most {VoicePerHour} an hour" : "off")}, {hour} in the last hour, asked {VoiceAsked}, answered {VoiceAnswered}, "
               + $"kept {VoiceKept}, thrown away {VoiceAnswered - VoiceKept} ({(thrown.Length == 0 ? "none" : thrown)}), no answer {VoiceNone}, slot busy {VoiceBusy}, stale {VoiceStale}, lost {VoiceLost}, "
               + $"{VoiceWaitedMs / Math.Max(1, VoiceAnswered + VoiceNone)}ms a call; {voiced} chapters retold, {voiceable} waiting; read on the page {CacheHits} in the model's words and {CacheMisses} in the template's; "
               + $"the Chronicle: {BotChronicle.Days.Count} days, {BotChronicle.Today()} entries today, {BotChronicle.Recorded} recorded ({BotChronicle.Kinds()}), {BotChronicle.Repeats} repeats, "
               + $"{BotChronicle.Dropped} past a full page, {BotChronicle.Voiced()} days retold; {TestCrowns} test crowns left out; {Faults} faults, {Interlocked.Read(ref _offLoop)} events heard off the loop";
    }

    /// <summary>"office 3, guild 12, opened:shop 40": the notes from events and the walk by kind, the heaviest count first.</summary>
    private static string ByKind()
    {
        if (_byKind.Count == 0)
        {
            return "none";
        }

        var kinds = new List<KeyValuePair<string, long>>(_byKind);

        kinds.Sort(static (a, b) => b.Value.CompareTo(a.Value));

        using var sb = ValueStringBuilder.Create(256);

        for (var i = 0; i < kinds.Count; i++)
        {
            sb.Append(i == 0 ? "" : ", ");
            sb.Append($"{kinds[i].Key} {kinds[i].Value}");
        }

        return sb.ToString();
    }

    private static void Stages(Span<int> stages)
    {
        // Counted off the notes and the last build's chapter count would not say which stage: rebuilt here, on the five-minute line,
        // from the notes alone (tales and ties are counted by the stage they would make).
        Span<bool> has = stackalloc bool[BotStages.Count];

        foreach (var (_, book) in _books)
        {
            has.Clear();

            for (var i = 0; i < book.Notes.Count; i++)
            {
                has[(int)book.Notes[i].Stage] = true;
            }

            if (BotLives.Find(book.Name) is { } life)
            {
                for (var i = 0; i < life.Tales.Count; i++)
                {
                    if (BotStoryWriter.StageOf(life.Tales[i]) is { } stage)
                    {
                        has[(int)stage] = true;
                    }
                }

                var (friend, enemy) = BotTies.Strongest(life);

                has[(int)BotStage.Friends] |= friend != null || enemy != null;
            }

            has[(int)BotStage.OldAge] |= (DateTime.UtcNow - book.FirstSeen).TotalDays >= Math.Max(1, OldAgeDays);

            for (var s = 0; s < BotStages.Count; s++)
            {
                stages[s] += has[s] ? 1 : 0;
            }
        }
    }

    /// <summary>The <c>stories</c> object of <c>/api/state</c>: the same counters as the line.</summary>
    public static void Web(Utf8JsonWriter w)
    {
        var three = 0;
        var threeLived = 0;
        var born = 0;

        foreach (var (_, book) in _books)
        {
            three += book.Chapters >= 3 ? 1 : 0;
            threeLived += book.Lived >= 3 ? 1 : 0;
            born += book.Born ? 1 : 0;
        }

        w.WritePropertyName("stories");
        w.WriteStartObject();
        w.WriteBoolean("running", Running);
        w.WriteNumber("books", _books.Count);
        w.WriteNumber("born", born);
        w.WriteNumber("threeChapters", three);
        w.WriteNumber("threeLived", threeLived);
        w.WriteNumber("births", BornNoted);
        w.WriteNumber("fromEvents", FromEvents);
        w.WriteNumber("fromWalk", FromWalk);
        w.WriteNumber("trimmed", Trimmed);
        w.WriteNumber("builds", Builds);
        w.WriteNumber("buildMeanUs", Builds == 0 ? 0 : Math.Round(BuildTicks * 1000000.0 / Stopwatch.Frequency / Builds, 1));
        w.WriteNumber("buildMostUs", Math.Round(BuildMostTicks * 1000000.0 / Stopwatch.Frequency, 1));
        w.WriteNumber("requests", Requests);
        w.WriteBoolean("voiced", Voiced);
        w.WriteBoolean("wired", Ask != null);
        w.WriteNumber("voicePerHour", VoicePerHour);
        w.WriteNumber("voiceAsked", VoiceAsked);
        w.WriteNumber("voiceAnswered", VoiceAnswered);
        w.WriteNumber("voiceKept", VoiceKept);
        w.WriteNumber("voiceNone", VoiceNone);
        w.WriteNumber("voiceBusy", VoiceBusy);
        w.WriteNumber("cacheHits", CacheHits);
        w.WriteNumber("cacheMisses", CacheMisses);
        w.WriteNumber("chronicleDays", BotChronicle.Days.Count);
        w.WriteNumber("chronicleToday", BotChronicle.Today());
        w.WriteNumber("chronicleRecorded", BotChronicle.Recorded);
        w.WriteNumber("faults", Faults);
        w.WriteEndObject();
    }

    // ------------------------------------------------------------------ keeping

    /// <summary>The layout <see cref="Save"/> writes, first in the file; <see cref="BotStoryStore"/> peeks it before the engine reads.</summary>
    internal const int Shape = 1;

    public static void Save(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Shape);
        writer.WriteEncodedInt(_books.Count);

        foreach (var (_, book) in _books)
        {
            writer.Write(book.Name);
            writer.Write(book.FirstSeen);
            writer.Write(book.LastSeen);
            writer.Write(book.Born);
            writer.Write(book.Female);
            writer.Write(book.Class);
            writer.Write(book.People);
            writer.Write(book.Town);
            writer.Write(book.SeenGuild);
            writer.Write(book.SeenRank);
            writer.Write(book.SeenHouse);
            writer.Write(book.SeenChampion);
            writer.Write(book.SeenVeteran);

            writer.WriteEncodedInt(book.Notes.Count);

            for (var i = 0; i < book.Notes.Count; i++)
            {
                var note = book.Notes[i];

                writer.WriteEncodedInt((int)note.Stage);
                writer.Write(note.Kind);
                writer.Write(note.Text);
                writer.Write(note.At);
                writer.Write(note.Weight);
            }

            var voiced = 0;

            for (var s = 0; s < book.Voiced.Length; s++)
            {
                voiced += book.Voiced[s] != null ? 1 : 0;
            }

            writer.WriteEncodedInt(voiced);

            for (var s = 0; s < book.Voiced.Length; s++)
            {
                if (book.Voiced[s] is not { } prose)
                {
                    continue;
                }

                writer.WriteEncodedInt(s);
                writer.Write(prose.Hash);
                writer.Write(prose.Text);
                writer.Write(prose.At);
            }
        }

        BotChronicle.Save(writer);
    }

    public static void Load(IGenericReader reader)
    {
        var shape = reader.ReadEncodedInt();

        if (shape != Shape)
        {
            // Not reached: BotStoryStore.Deserialize peeks the shape in the file and does not hand a file of another shape to the
            // engine at all. A seek to the end cannot stand in for that: the reader's end is the memory-mapped view's length, which
            // Windows rounds up to a page (a 100-byte file reads 4096), and the engine compares the position with the file's own
            // length and stops the boot on its console prompt when they differ (a3 review A, F1).
            logger.Error("The saved stories are shape {Found} and this build reads {Wanted}, past the store's own check", shape, Shape);

            return;
        }

        var count = reader.ReadEncodedInt();
        var notes = 0;
        var voiced = 0;

        for (var n = 0; n < count; n++)
        {
            var book = new BotStoryBook(reader.ReadString())
            {
                FirstSeen = reader.ReadDateTime(),
                LastSeen = reader.ReadDateTime(),
                Born = reader.ReadBool(),
                Female = reader.ReadBool(),
                Class = reader.ReadString(),
                People = reader.ReadString(),
                Town = reader.ReadString(),
                SeenGuild = reader.ReadString(),
                SeenRank = reader.ReadString(),
                SeenHouse = reader.ReadString(),
                SeenChampion = reader.ReadBool(),
                SeenVeteran = reader.ReadBool()
            };

            var noteCount = reader.ReadEncodedInt();

            for (var i = 0; i < noteCount; i++)
            {
                var stage = reader.ReadEncodedInt();

                book.Notes.Add(
                    new BotStoryNote
                    {
                        Stage = (BotStage)Math.Clamp(stage, 0, BotStages.Count - 1),
                        Kind = reader.ReadString(),
                        Text = reader.ReadString(),
                        At = reader.ReadDateTime(),
                        Weight = reader.ReadDouble()
                    }
                );
            }

            var voicedCount = reader.ReadEncodedInt();

            for (var i = 0; i < voicedCount; i++)
            {
                var stage = reader.ReadEncodedInt();
                var prose = new BotStoryProse { Hash = reader.ReadULong(), Text = reader.ReadString(), At = reader.ReadDateTime() };

                if (stage >= 0 && stage < BotStages.Count)
                {
                    book.Voiced[stage] = prose;
                    voiced += prose.Text != null ? 1 : 0;
                }
            }

            if (!string.IsNullOrEmpty(book.Name))
            {
                _books[book.Name] = book;
                notes += book.Notes.Count;
            }
        }

        var days = BotChronicle.Load(reader);

        logger.Information(
            "Stories read back: {Count} books with {Notes} notes and {Voiced} chapters in the model's words; the Chronicle's {Days} days",
            count,
            notes,
            voiced,
            days
        );
    }

    /// <summary>The counters and what is in flight, for a world reload; the books are what the save holds and stay.</summary>
    public static void Forget()
    {
        _generation++;
        _asking = false;
        _wanted.Clear();
        _asks.Clear();
        _thrown.Clear();
        _byKind.Clear();
        BornNoted = 0;
        Opened = 0;
        FromEvents = 0;
        FromWalk = 0;
        Trimmed = 0;
        Repeats = 0;
        Looks = 0;
        Builds = 0;
        BuildTicks = 0;
        BuildMostTicks = 0;
        Published = 0;
        Requests = 0;
        Unknown = 0;
        VoiceAsked = 0;
        VoiceAnswered = 0;
        VoiceNone = 0;
        VoiceKept = 0;
        VoiceBusy = 0;
        VoiceStale = 0;
        VoiceLost = 0;
        VoiceWaitedMs = 0;
        CacheHits = 0;
        CacheMisses = 0;
        Faults = 0;
        TestCrowns = 0;
        _publishedOnce = false;
        _owed = false;
        _changed = true;
        BotChronicle.Forget();
    }

    /// <summary>
    /// Patrick's reset (<c>do reset</c>): a new world has no history, so every book and every page of the Chronicle is forgotten
    /// and the save this reset makes writes the store empty. A name born again starts a new book. Returns the books forgotten.
    /// </summary>
    public static int Wipe()
    {
        var books = _books.Count;

        _books.Clear();
        _json.Clear();
        _cursor = 0;
        _voiceCursor = 0;
        Forget();
        BotChronicle.Wipe();
        _shelf = Shelf.Empty;

        // Until the restart: the guilds disbanded and the bodies purged by the same reset, or a clock that fires before the save,
        // would otherwise write the old world's last minutes into the new world's first page.
        _sealed = true;

        return books;
    }

    /// <summary>True between <c>do reset</c> and the next start: nothing is written. See <see cref="Wipe"/>.</summary>
    public static bool Sealed => _sealed;

    /// <summary>
    /// A world reload in the same process (<c>BotModules.Rewind</c>, <see cref="BotStoryModule.Reset"/>) ends a reset's seal too, not
    /// only this module's start: the population starts first and raises the new world's bots, and with the seal standing their births
    /// (<see cref="Born"/>) would be lost and every one of them opened later as "found living" (a3 review A, F2).
    /// </summary>
    public static void Unseal() => _sealed = false;
}
