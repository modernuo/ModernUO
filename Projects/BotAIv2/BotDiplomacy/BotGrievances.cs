using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// What each guild holds against each other, by cause: the book an envoy reads its grievances from and the duke judges by.
///
/// <para>
/// <b>An opinion is one number, and a quarrel is not.</b> <c>BotRegard</c> keeps what The Blade thinks of The Needle as a
/// single figure, which is right for deciding <em>when</em> the two should meet and useless for what is said when they do:
/// −47 is the same number whether it came of twelve pieces of work on somebody's land or of one killing and a border. Patrick's
/// order of 29.09.2026 wants the envoys to state their claims — "one side says the other attacks it too often, the other says
/// the first crosses its borders" — and the claims are exactly the causes <c>BotRegard.Move</c> already names. So every move of
/// an opinion is also written here, under its cause, with how often it happened and how far it moved things.
/// </para>
///
/// <para>
/// <b>Since the last settlement, not since the beginning of the world.</b> <c>BotRegard.Settle</c> puts both opinions back to
/// nought when a war ends or a meeting settles the quarrel, and the book of that pair is closed with it: a grievance the duke
/// has already judged is not brought before him again. Time's slow forgetting (<c>BotRegard.Mend</c>) is not a cause and is
/// not written. Kept across restarts by <see cref="BotDiplomacyStore"/>, as the opinions are by <c>BotWarStore</c>.
/// </para>
/// </summary>
public static class BotGrievances
{
    /// <summary>One cause in one guild's book about another: how often, and how far it moved the opinion in all.</summary>
    public sealed class Entry
    {
        public int Count;

        public double Moved;
    }

    /// <summary>The cause <c>BotRegard</c> writes for time's own mending, which is not a grievance and is kept out of the book.</summary>
    public const string Forgetting = "time";

    private static readonly Dictionary<(string Of, string About), Dictionary<string, Entry>> _books = [];

    /// <summary>Moves written into the books, all told.</summary>
    public static long Written { get; private set; }

    /// <summary>Books closed by a settlement.</summary>
    public static long Closed { get; private set; }

    /// <summary>
    /// An opinion moved, and why. Called from <c>BotRegard.Move</c> after the opinion has changed, with the change actually
    /// made (after the scale's floor and ceiling), so the book adds up to the opinion it explains.
    /// </summary>
    public static void Noted(string of, string about, double moved, string why)
    {
        if (of == null || about == null || of == about || why == null || moved == 0.0 || why == Forgetting)
        {
            return;
        }

        var key = (of, about);

        if (!_books.TryGetValue(key, out var book))
        {
            _books[key] = book = new Dictionary<string, Entry>(StringComparer.Ordinal);
        }

        if (!book.TryGetValue(why, out var entry))
        {
            book[why] = entry = new Entry();
        }

        entry.Count++;
        entry.Moved += moved;
        Written++;
    }

    /// <summary>A quarrel settled: both books of the pair closed. Called from <c>BotRegard.Settle</c>.</summary>
    public static void Settled(string one, string other)
    {
        if (one == null || other == null)
        {
            return;
        }

        var any = _books.Remove((one, other));
        any |= _books.Remove((other, one));

        if (any)
        {
            Closed++;
        }
    }

    /// <summary>What <paramref name="of"/> holds against <paramref name="about"/>, worst first, or an empty list.</summary>
    public static List<(string Why, int Count, double Moved)> Of(string of, string about)
    {
        List<(string, int, double)> found = [];

        if (of == null || about == null || !_books.TryGetValue((of, about), out var book))
        {
            return found;
        }

        foreach (var (why, entry) in book)
        {
            found.Add((why, entry.Count, entry.Moved));
        }

        found.Sort((a, b) => a.Item3.CompareTo(b.Item3));

        return found;
    }

    /// <summary>How many times <paramref name="of"/> wrote <paramref name="why"/> against <paramref name="about"/>.</summary>
    public static int Count(string of, string about, string why) =>
        of != null && about != null && why != null && _books.TryGetValue((of, about), out var book) && book.TryGetValue(why, out var entry)
            ? entry.Count
            : 0;

    /// <summary>One guild's book about another in one phrase, for the duke and the dashboard: "blood ×2 (−50), trade ×8 (+4)".</summary>
    public static string Tell(string of, string about)
    {
        var book = Of(of, about);

        if (book.Count == 0)
        {
            return "nothing written";
        }

        using var say = Server.Text.ValueStringBuilder.Create(128);

        for (var i = 0; i < book.Count; i++)
        {
            var (why, count, moved) = book[i];

            if (i > 0)
            {
                say.Append(", ");
            }

            say.Append($"{why} ×{count} ({moved:+0;-0})");
        }

        return say.ToString();
    }

    /// <summary>Every book, for the store.</summary>
    public static IEnumerable<KeyValuePair<(string Of, string About), Dictionary<string, Entry>>> Books => _books;

    /// <summary>One cause read back off the save.</summary>
    public static void Restore(string of, string about, string why, int count, double moved)
    {
        if (string.IsNullOrEmpty(of) || string.IsNullOrEmpty(about) || of == about || string.IsNullOrEmpty(why) || count <= 0)
        {
            return;
        }

        if (!_books.TryGetValue((of, about), out var book))
        {
            _books[(of, about)] = book = new Dictionary<string, Entry>(StringComparer.Ordinal);
        }

        book[why] = new Entry { Count = count, Moved = moved };
    }

    public static string Describe() => $"{_books.Count} books of grievances open, {Written} moves written into them, {Closed} closed by a settlement";

    /// <summary>The session's counters; the books are kept, being the store's.</summary>
    public static void Forget()
    {
        Written = 0;
        Closed = 0;
    }

    /// <summary>Every book burnt, for Patrick's reset.</summary>
    public static int Wipe()
    {
        var gone = _books.Count;

        _books.Clear();
        Forget();

        return gone;
    }
}
