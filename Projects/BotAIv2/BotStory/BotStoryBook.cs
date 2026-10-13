using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

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

/// <summary>
/// One bot's story as this chronicle keeps it: when it first saw the bot and whether it saw it born, what it was then, the notes
/// of its life since, what the slow walk last saw of it (so a change is a note and a restart is not), and the chapters the model
/// has retold. Keyed by name, as <see cref="BotLife"/> is: a bot is raised with a new serial at every boot.
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

    /// <summary>When this book was opened in this run, a tick; null for a book read back from the save. Not saved. See <see cref="BotStory.SettleMs"/>.</summary>
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

    /// <summary>Whether this very sentence was written into the book in the last day: an event said twice is one thing that happened.</summary>
    public bool Said(string text, DateTime now)
    {
        for (var i = Notes.Count - 1; i >= 0; i--)
        {
            var note = Notes[i];

            if ((now - note.At).TotalHours < 24 && string.Equals(note.Text, text, StringComparison.Ordinal))
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
