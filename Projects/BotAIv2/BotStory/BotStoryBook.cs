using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// One bot's book: when the chronicle first saw it, what it was then, its notes, and the chapters the model retold.
/// Whether it was seen born, what the slow walk last saw of it (so a change is a note and a restart is not). Keyed by name, as
/// <see cref="BotLife"/> is: a bot is raised with a new serial at every boot.
/// </summary>
public sealed class BotStoryBook
{
    public BotStoryBook(string name) => Name = name;

    public string Name { get; }

    public DateTime FirstSeen;

    public DateTime LastSeen;

    /// <summary>Seen born under this chronicle (a character drawn fresh), as against found already living.</summary>
    public bool Born;

    public bool Female;

    /// <summary>The class, the people and the town as they were when the book was opened: what the youth chapter says.</summary>
    public string Class;

    public string People;

    public string Town;

    /// <summary>What the walk saw last. A difference on the next look is a note; the same after a restart is nothing.</summary>
    public string SeenGuild;

    public string SeenRank;

    public string SeenHouse;

    public bool SeenChampion;

    public bool SeenVeteran;

    public readonly List<BotStoryNote> Notes = new(8);

    /// <summary>The model's retelling of each stage's chapter, or null. See <see cref="BotStoryProse"/>.</summary>
    public readonly BotStoryProse[] Voiced = new BotStoryProse[BotStages.Count];

    /// <summary>
    /// When this book was opened in this run, a tick, until the first look after <see cref="BotStory.SettleMs"/> has taken where the
    /// bot stands; null for a settled book and for one read back from the save. Not saved.
    /// </summary>
    public long? OpenedTick;

    /// <summary>Chapters at the last build, and of them the ones holding something seen to happen. Not saved.</summary>
    public int Chapters;

    public int Lived;

    /// <summary>Chapters at the last build the model may retell and has not for their present facts. Not saved.</summary>
    public int Unvoiced;

    public bool Has(string kind)
    {
        for (var i = 0; i < Notes.Count; i++)
        {
            if (Notes[i].Kind == kind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether this very thing was written into the book in the last day: an event said twice is one thing that happened. Compared
    /// by its key when it has one (the sentence with its coordinates), else by its words.
    /// </summary>
    public bool Said(string text, string key, DateTime now)
    {
        var self = key ?? text;

        for (var i = Notes.Count - 1; i >= 0; i--)
        {
            var note = Notes[i];

            if ((now - note.At).TotalHours < 24 && string.Equals(note.Key ?? note.Text, self, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The notes of one stage, oldest first, into the list given.</summary>
    public void Of(BotStage stage, List<BotStoryNote> into)
    {
        into.Clear();

        for (var i = 0; i < Notes.Count; i++)
        {
            if (Notes[i].Stage == stage)
            {
                into.Add(Notes[i]);
            }
        }

        // Kept in the order written, which is the order they happened but for a note read back from an older save; a stable
        // insertion sort on a handful of notes is cheaper than any allocation a comparer would make.
        for (var i = 1; i < into.Count; i++)
        {
            var note = into[i];
            var j = i - 1;

            while (j >= 0 && into[j].At > note.At)
            {
                into[j + 1] = into[j];
                j--;
            }

            into[j + 1] = note;
        }
    }
}

/// <summary>
/// One thing the story keeps about a bot: a sentence in the third person with the real names, which stage of its life it
/// belongs to, when it happened and how much it weighs when the book is full.
/// </summary>
public sealed class BotStoryNote
{
    public BotStage Stage;

    /// <summary>born, seen, guild, founded, office, rank, opened, relic, champion, bout, crime, home, house, veteran…</summary>
    public string Kind;

    /// <summary>The sentence, beginning with the bot's name or the guild's, with no date: the writer adds the date.</summary>
    public string Text;

    /// <summary>What tells this note from another with the same words (the event's sentence with its coordinates), or null. Not saved.</summary>
    public string Key;

    /// <summary>When it happened, in UTC, as the biography's tales are kept.</summary>
    public DateTime At;

    public double Weight;

    /// <summary>Where the bot stood when this chronicle first found it, as against a thing seen to happen to it.</summary>
    public bool Baseline => Kind == "seen";
}

/// <summary>A chapter as the model retold it, kept for the facts it was told from (their hash) and shown only while they stand.</summary>
public sealed class BotStoryProse
{
    public ulong Hash;

    public string Text;

    public DateTime At;
}

/// <summary>A stage of a bot's life, in the order its chapters are told. A stage with nothing in it is no chapter at all.</summary>
public enum BotStage
{
    Youth,
    Apprenticeship,
    Guild,
    Deeds,
    Friends,
    Home,
    Family,
    OldAge
}

/// <summary>The stages' names on the page and in the log.</summary>
public static class BotStages
{
    public const int Count = 8;

    public static string Key(BotStage stage) =>
        stage switch
        {
            BotStage.Youth          => "youth",
            BotStage.Apprenticeship => "apprenticeship",
            BotStage.Guild          => "guild",
            BotStage.Deeds          => "deeds",
            BotStage.Friends        => "friends",
            BotStage.Home           => "home",
            BotStage.Family         => "family",
            _                       => "old age"
        };

    public static string Title(BotStage stage) =>
        stage switch
        {
            BotStage.Youth          => "Youth",
            BotStage.Apprenticeship => "Apprenticeship",
            BotStage.Guild          => "The guild",
            BotStage.Deeds          => "Deeds",
            BotStage.Friends        => "Friends and foes",
            BotStage.Home           => "Home",
            BotStage.Family         => "Family",
            _                       => "Old age"
        };
}

/// <summary>One chapter of a bot's story as the writer built it: its facts, the template's prose, and the model's when it is current.</summary>
public sealed class BotStoryChapter
{
    public BotStage Stage;

    /// <summary>The dated lines the template and the model are both told from, oldest first. Their hash keys the model's prose.</summary>
    public readonly List<string> Facts = new(8);

    /// <summary>The template's paragraph. Always there when the chapter is.</summary>
    public string Text;

    /// <summary>
    /// A closing sentence of counts that move by the minute (kills, duels, days lived), kept out of the facts' hash so that a
    /// chapter retold by the model is not made stale by its hundred-and-first kill; said after either prose.
    /// </summary>
    public string Tally;

    public ulong Hash;

    /// <summary>Holds something seen to happen, as against only where the bot stood when the chronicle found it.</summary>
    public bool Lived;

    /// <summary>The model's paragraph, only when it was told exactly these facts. See <see cref="BotStoryProse"/>.</summary>
    public string Voiced;

    public bool Present => Facts.Count > 0 || Tally != null;

    public void Clear(BotStage stage)
    {
        Stage = stage;
        Facts.Clear();
        Text = null;
        Tally = null;
        Hash = 0;
        Lived = false;
        Voiced = null;
    }
}

/// <summary>
/// What the story asks of a model: one chapter of one bot's life, or one closed day of the Chronicle, as the dated facts the
/// template was written from — strings only, made on the loop, so that whatever answers it on another thread holds nothing of
/// the world. See <see cref="BotStory.Ask"/>.
/// </summary>
public sealed class BotStoryAsk
{
    /// <summary><c>chapter</c> or <c>day</c>.</summary>
    public string Kind { get; init; }

    /// <summary>The bot's name, or the day's title.</summary>
    public string Subject { get; init; }

    /// <summary>The chapter's title ("Deeds"), or "The Chronicle of the island".</summary>
    public string Title { get; init; }

    /// <summary>Who the chapter is about, in words: "Brokk, a dwarf warrior of The Anchor".</summary>
    public string Who { get; init; }

    /// <summary>The facts, one a line, dated: the whole of what may be said.</summary>
    public string Facts { get; init; }

    public int MostWords { get; init; }

    /// <summary>The model to ask; null or empty leaves it to the answering side (the duke's).</summary>
    public string Model { get; init; }
}

/// <summary>One line of the island's Chronicle: when (local time, as the event stream dates its days), what kind, and the words.</summary>
public sealed class BotChronicleEntry
{
    public DateTime At;

    /// <summary>war, guild, champion, relic, contest, diplomacy, politics, town, crime, road.</summary>
    public string Kind;

    public string Text;

    /// <summary>What tells this entry from another with the same words (the event's sentence with its coordinates), or null. Not saved.</summary>
    public string Key;

    public double Weight;
}

/// <summary>One day's page of the Chronicle: its entries in the order they happened, and the model's retelling once the day is over.</summary>
public sealed class BotChronicleDay
{
    public string Key;

    public DateTime Date;

    public readonly List<BotChronicleEntry> Entries = new(16);

    public BotStoryProse Voiced;

    /// <summary>Entries that came to a full page and were too light to keep. Not saved.</summary>
    public int Dropped;

    /// <summary>The hash of the facts the model would be told, kept until an entry is added. Not saved.</summary>
    public ulong? Hashed;
}
