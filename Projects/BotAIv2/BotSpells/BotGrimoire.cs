using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What a book holds, what it is short of, and how a scroll becomes a spell in it.
///
/// <para>
/// <b>The book is the one thing a caster owns that grows.</b> It is bound at birth — weightless, kept through
/// death, never merchandise — and it starts with the two or three spells that make the class function at all.
/// Everything above that has to come from somewhere, and this is the file that knows where.
/// </para>
///
/// <para>
/// <b>The engine draws a line across the middle of the spell list, and the whole economy of magic on this
/// shard is that line.</b> A mage vendor stocks the first three circles — twenty-four scrolls, at twelve,
/// twenty-two and thirty-two gold — and stops. The other forty spells are sold by nobody at any price: they
/// come from a monster's corpse or from somebody with the Inscribe skill, a blank scroll and a handful of
/// herbs. There is no hunting in this version, so on this shard, today, they come from exactly one place —
/// another bot. That is not a rule anybody wrote; it is the shape of the content, and it is the reason a
/// caster's ambition is a market rather than a shopping list.
/// </para>
/// </summary>
public static class BotGrimoire
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGrimoire));

    /// <summary>Spells in a regular book, and the number the engine's own bitmask holds.</summary>
    public const int Spells = 64;

    /// <summary>Spells to a circle. Eight, and it is what makes an id readable as a circle.</summary>
    public const int PerCircle = 8;

    /// <summary>
    /// The highest circle a shopkeeper will sell. Read off <c>SBMage</c>, not chosen.
    ///
    /// Everything above this is the part of the market that only a bot can supply, and it is where every
    /// interesting price on this shard is going to be found.
    /// </summary>
    public const int ShopCircles = 3;

    private static readonly Dictionary<Type, int> _idOf = [];

    private static readonly Type[] _scrollOf = new Type[Spells];

    /// <summary>How many of the sixty-four the map actually resolved. Zero before <see cref="Read"/>.</summary>
    public static int Known { get; private set; }

    /// <summary>
    /// Builds the map between a scroll type and the spell it writes, once.
    ///
    /// <para>
    /// <b>Neither direction of this is guessable, and one of them is a trap.</b> The engine's spell ids come
    /// from <c>Spells/Initializer.cs</c> — Clumsy is 0, Heal is 3, Magic Arrow is 4 — while
    /// <c>Loot.RegularScrollTypes</c> is in the client's art order, where Reactive Armor comes first. The two
    /// orders agree on which spells are in which circle and disagree about everything else, so a map built by
    /// index would be wrong for the whole of the first circle and look right.
    /// </para>
    ///
    /// <para>
    /// So it is read off the objects themselves: one of each scroll is made, asked what spell it is, and
    /// destroyed. Sixty-four items once in the life of the process, which is cheaper than being wrong.
    /// </para>
    /// </summary>
    public static void Read()
    {
        if (Known > 0)
        {
            return;
        }

        var types = Loot.RegularScrollTypes;

        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];

            if (type == null || _idOf.ContainsKey(type))
            {
                continue;
            }

            var sample = type.CreateInstance<SpellScroll>();

            if (sample == null)
            {
                continue;
            }

            var id = sample.SpellID;

            sample.Delete();

            if (id < 0 || id >= Spells)
            {
                continue;
            }

            _idOf[type] = id;
            _scrollOf[id] = type;

            Known++;
        }

        logger.Information(
            "Read {Known} of {Total} scrolls; circles 1 to {Shop} can be bought and the rest have to be written",
            Known,
            Spells,
            ShopCircles
        );
    }

    /// <summary>Which circle a spell belongs to, from one to eight.</summary>
    public static int Circle(int spellId) => spellId / PerCircle + 1;

    /// <summary>The scroll that writes this spell, or null if the map does not have it.</summary>
    public static Type ScrollFor(int spellId) =>
        spellId >= 0 && spellId < Spells ? _scrollOf[spellId] : null;

    /// <summary>Which spell this kind of scroll writes, or -1.</summary>
    public static int SpellOf(Type scroll) =>
        scroll != null && _idOf.TryGetValue(scroll, out var id) ? id : -1;

    /// <summary>Whether a shopkeeper anywhere in this era sells this spell as a scroll.</summary>
    public static bool Sold(int spellId) => Circle(spellId) <= ShopCircles;

    /// <summary>
    /// What one of these costs at a counter, and the opening offer for one that has no counter.
    ///
    /// <para>
    /// Twelve for the first circle and ten more for each one after is the engine's own ladder, read off
    /// <c>SBMage</c> for the three circles it sells. <b>Continuing it past the third is the one number in
    /// this subsystem that is extrapolated rather than read</b>, and it is only ever an opening offer: a want
    /// that nobody fills raises itself fifteen per cent at a time up to four times what it opened at, so what
    /// an eighth-circle scroll is really worth is settled by whether anybody writes one, not here.
    /// </para>
    /// </summary>
    public static int ShopPrice(int circle) => 12 + 10 * (Math.Max(1, circle) - 1);

    /// <summary>
    /// This bot's regular spellbook, or null.
    ///
    /// Read straight out of the pack rather than through the engine's <c>Spellbook.Find</c>, which keeps a
    /// static table keyed by mobile — the same idiom the other trades use to find a sewing kit or a pickaxe,
    /// and one fewer cache holding references to a population that is rebuilt on every world load.
    /// </summary>
    public static Spellbook Book(Mobile bot)
    {
        if (bot == null)
        {
            return null;
        }

        // <b>The first spellbook in the pack is not necessarily this bot's spellbook.</b>
        // FindItemByType returns whichever it meets first, and this population goes through every corpse it
        // makes — so one looted necromancer's book, sitting anywhere in the pack, would answer this question
        // for ever after and the caster behind it would stop having a book at all, by every test that asks.
        //
        // <b>This was not the cause of anything, and that is worth saying.</b> It was written while chasing
        // 233 of 430 scroll purchases ending "the book would not take it", on a theory that turned out to be
        // wrong — the real answer was that most of those bots were warriors stocking scrolls to throw and had
        // never wanted a book at all. See BotAcquire.Purpose. Kept because the hazard is real and costs one
        // loop to close; recorded as not-the-cause because a comment claiming a defect it did not fix is how
        // the next person gets sent down the same road.
        //
        // Every spellbook is looked at, and the bot's own is the Regular one.
        var pack = bot.Backpack;

        if (pack != null)
        {
            foreach (var book in pack.FindItemsByType<Spellbook>())
            {
                if (book is { Deleted: false, SpellbookType: SpellbookType.Regular })
                {
                    return book;
                }
            }
        }

        // And in a hand, because a tool that the bot itself put on stops being in the pack — the rule the
        // axe taught this project, kept here so it never has to be learned twice.
        return bot.FindItemOnLayer(Layer.OneHanded) as Spellbook is { SpellbookType: SpellbookType.Regular } held
            ? held
            : bot.FindItemOnLayer(Layer.TwoHanded) as Spellbook is { SpellbookType: SpellbookType.Regular } other
                ? other
                : null;
    }

    /// <summary>Whether this bot's book already has that spell.</summary>
    public static bool Holds(Mobile bot, int spellId) => Book(bot)?.HasSpell(spellId) == true;

    /// <summary>How many spells this bot's book holds.</summary>
    public static int Count(Mobile bot) => Book(bot)?.SpellCount ?? 0;

    /// <summary>
    /// The next spell this bot's book is short of, or -1 when it is short of nothing.
    ///
    /// First whatever its class learns first (<see cref="BotClass.BookFirst"/>), in that class's order; then the
    /// lowest id, which is lowest circle first, because the ids are laid out by circle — so a caster works up the
    /// rest of its book the way anybody learns anything.
    /// </summary>
    public static int Missing(Mobile bot)
    {
        var book = Book(bot);

        if (book == null)
        {
            return -1;
        }

        if (bot is BotMobile { Class.BookFirst: { Length: > 0 } first })
        {
            for (var i = 0; i < first.Length; i++)
            {
                var wanted = first[i];

                if (wanted >= 0 && wanted < Spells && _scrollOf[wanted] != null && !book.HasSpell(wanted))
                {
                    return wanted;
                }
            }
        }

        // Runes (29.09.2026): a rune-mage learns Recall, Mark and Gate Travel next, not after thirty other spells. See BotRunes.Wanted.
        var travel = BotRunes.Wanted(bot, book);

        if (travel >= 0 && _scrollOf[travel] != null)
        {
            return travel;
        }

        for (var id = 0; id < Spells; id++)
        {
            if (_scrollOf[id] != null && !book.HasSpell(id))
            {
                return id;
            }
        }

        return -1;
    }

    /// <summary>Whether this bot's book is short of the spell this kind of scroll writes.</summary>
    public static bool Wants(Mobile bot, Type scroll)
    {
        var id = SpellOf(scroll);

        return id >= 0 && !Holds(bot, id);
    }

    /// <summary>
    /// Writes one scroll into the bot's own book and says whether the spell is now in it.
    ///
    /// <para>
    /// <b>The engine's own answer to this question is wrong for a stack, and this is the whole reason this
    /// method exists.</b> <c>Spellbook.OnDragDrop</c> writes the spell, consumes one scroll and then returns
    /// <c>scroll.Deleted</c> — and <c>Item.Consume</c> only deletes when the stack runs out. So dropping two
    /// scrolls on a book writes the spell perfectly well and reports failure, and a caller that believes the
    /// return value will try again for ever against a book that already has it. Exactly the same shape as the
    /// loot flag on a merged stack of arrows: the engine's answer is about the object, and the question is
    /// about the contents.
    /// </para>
    ///
    /// <para>
    /// So the book is asked afterwards instead. That is the only reliable question, and it costs one bit test.
    /// </para>
    /// </summary>
    public static bool Write(Mobile bot, SpellScroll scroll) => Write(bot, scroll, out _);

    /// <summary>
    /// The same, saying which of the four ways it can fail actually happened.
    ///
    /// <para>
    /// <b>Written because 233 of 430 scroll purchases in one session ended "the book would not take it" and
    /// nothing anywhere said why.</b> Every one of the reasons below is a silent refusal — three of them are
    /// this method's own and the fourth is <c>Spellbook.OnDragDrop</c>, which answers a scroll of the wrong
    /// book's school, or an id outside the book's range, by returning false and sending a line to a screen
    /// the bot has not got. Half the money a caster spends was going somewhere unaccounted for, and the only
    /// way to find out which way was to make the sentence say it.
    /// </para>
    /// </summary>
    public static bool Write(Mobile bot, SpellScroll scroll, out string why)
    {
        why = null;

        var book = Book(bot);

        if (book == null)
        {
            why = "it is not carrying a spellbook";

            return false;
        }

        if (scroll is not { Deleted: false })
        {
            why = "the scroll went";

            return false;
        }

        var id = scroll.SpellID;

        if (book.HasSpell(id))
        {
            why = "the book already had it";

            return false;
        }

        book.OnDragDrop(bot, scroll);

        if (book.HasSpell(id))
        {
            return true;
        }

        // The engine's two refusals, told apart here because they want opposite answers: a scroll of another
        // school is a buying mistake, and an id outside the book is a mapping mistake.
        why = Spellbook.GetTypeForSpell(id) != book.SpellbookType
            ? $"spell {id} belongs to a {Spellbook.GetTypeForSpell(id)} book, not this one"
            : $"the engine refused spell {id} for a {book.SpellbookType} book";

        return false;
    }

    /// <summary>Everything forgotten. The map is about types and survives a world reload; the count is not.</summary>
    public static string Describe() =>
        $"{Known} scrolls mapped, circles 1-{ShopCircles} on shop shelves and {Spells - ShopCircles * PerCircle} that have to be written";
}
