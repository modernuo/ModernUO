using System;
using Server.Items;
using Server.Spells;

namespace Server.BotAI.V2;

/// <summary>
/// Casting at something, as opposed to casting at somebody who is hurt.
///
/// <para>
/// <b>The half of magic this project never wrote.</b> A caster's book filled up, its Inscribe climbed, its
/// reagents were bought and spent — on writing. In a fight it walked up and hit things with a stick, which is
/// what a mage is worst at and what its whole build is arranged to avoid. Watched from a client it reads
/// exactly as it is: mages and healers being beaten while holding a full spellbook.
/// </para>
///
/// <para>
/// <b>Distance is not a nicety here, it is the mechanic.</b> A blow disturbs a cast whenever the caster is a
/// player, and every bot is one — so a caster in contact does not cast slowly, it does not cast at all. That
/// is why this pairs with the stand-off: a caster fights at the range its spells reach and gives ground when
/// something closes, exactly as an archer does, and for a sharper reason.
/// </para>
///
/// <para>
/// What it does not do is choose cleverly. The strongest thing the book holds and the pool can pay for, and
/// nothing else — no resistances, no target types, no combinations. Those are worth having and they are worth
/// having <em>after</em> a caster stops losing fights it should win.
/// </para>
/// </summary>
public static class BotStrike
{
    /// <summary>
    /// Attack spells, weakest first, by the engine's own spell id.
    ///
    /// Only the ones that simply do damage to one thing. Fields, curses and summons are all reasonable in a
    /// fight and every one of them needs its own judgement about when it helps; a bot that can reliably throw
    /// a fireball is already an enormous step from a bot swinging a stick.
    /// </summary>
    private static readonly int[] Ladder =
    [
        BotArsenal.SpellMagicArrow, // 1st circle
        SpellHarm,                  // 2nd
        SpellFireball,              // 3rd
        SpellLightning,             // 4th
        SpellMindBlast,             // 5th
        SpellEnergyBolt             // 6th
    ];

    /// <summary>The strongest attack spell this bot's book holds, whatever its mana and herbs, or -1. See BotProvision.</summary>
    public static int Strongest(Mobile bot)
    {
        for (var i = Ladder.Length - 1; i >= 0; i--)
        {
            if (BotGrimoire.Holds(bot, Ladder[i]))
            {
                return Ladder[i];
            }
        }

        return -1;
    }

    public const int SpellHarm = 11;

    public const int SpellFireball = 17;

    public const int SpellLightning = 29;

    public const int SpellMindBlast = 36;

    public const int SpellEnergyBolt = 41;

    /// <summary>
    /// How far a caster fights from.
    ///
    /// Eight, inside the engine's own ten to twelve for a targeted spell, and the same number mending uses for
    /// the same reason: standing at the edge of your range means a target that takes one step is out of it.
    /// </summary>
    public static int Range { get; set; } = 8;

    /// <summary>
    /// How long to leave between attempts at starting a spell.
    ///
    /// <para>
    /// <b>The first version's casters threw spells faster than the engine would take them, and so threw
    /// none.</b> A cast has a recovery — <c>NextSpellTime</c> — and calling for another inside it is refused
    /// with a message to a client that a bot has not got; under AOS rules a fresh cast disturbs the one in
    /// flight outright. A bot beating on it every two hundred milliseconds therefore produces a great deal of
    /// nothing. A second and a half is longer than any first-circle recovery and short enough that a caster
    /// is not idling between spells.
    /// </para>
    /// </summary>
    public static int CastMs { get; set; } = 1500;

    /// <summary>
    /// Mana a circle costs, read off the era's own ladder — the numbers inscription charges to write one.
    ///
    /// The engine keeps the true cost inside the spell where nothing outside can ask it. If this is wrong the
    /// price is a cast that fizzles and a bot that swings instead, which is the failure it already has.
    /// </summary>
    private static readonly int[] ManaByCircle = [4, 6, 9, 11, 14, 20, 40, 50];

    /// <summary>
    /// The Magery a scroll of each circle wants before the engine will let it go off, taken from
    /// <c>MagerySpell.GetCastSkills</c> rather than guessed.
    ///
    /// <para>
    /// The engine keeps one table and reads it at two offsets: a spell cast from the book is asked for the
    /// entry two circles higher than its own, and a spell cast <em>from a scroll</em> is asked for its own.
    /// That gap is what a scroll is for. These are the scroll numbers, so circle one is free to anybody and
    /// circle eight wants fifty — and full reliability comes forty points above each, which is what
    /// <see cref="SkillMargin"/> is measured against.
    /// </para>
    /// </summary>
    private static readonly double[] ScrollSkillByCircle = [-50.0, -30.0, 0.0, 10.0, 20.0, 30.0, 40.0, 50.0];

    /// <summary>
    /// How far above the bare requirement a bot must be before a circle is worth stocking.
    ///
    /// Twenty of the forty points between "might work" and "always works", so a bot buys into a circle at
    /// about even odds rather than at the first roll it could theoretically pass. Scrolls cost money and a
    /// coin flip is not a supply.
    /// </summary>
    public static double SkillMargin { get; set; } = 20.0;

    /// <summary>
    /// How many casts of a circle a bot's whole pool must cover before that circle is worth buying.
    ///
    /// Two. A scroll the bot can throw exactly once on a full pool is a scroll it throws at the start of one
    /// fight a day, which is not a weapon — it is an ornament that cost twelve gold.
    /// </summary>
    public static int PoolCasts { get; set; } = 2;

    /// <summary>
    /// The strongest attack this bot should be <em>stocking</em>, or -1 if none.
    ///
    /// <para>
    /// <b>A different question from <see cref="Best"/>, and conflating them is why every bot bought magic
    /// arrows for ever.</b> Best asks what can be thrown this instant: it reads current mana and what is
    /// actually in the pack, because that is a decision inside a fight. This asks what is worth owning, which
    /// depends on the pool rather than the moment, and on the skill the engine will roll when the scroll is
    /// used. The shopping side used to skip the question altogether and name one spell as a constant —
    /// reasonably, since a ladder that ignored either fact would have had warriors buying energy bolts — but
    /// the answer to "a warrior cannot afford this" is to ask about the warrior, not to freeze everybody at
    /// the bottom rung for life. A mage that has doubled its Magery since morning should be buying something
    /// better than it bought as a novice, and until now there was nothing better it was allowed to want.
    /// </para>
    /// </summary>
    public static int Stock(Mobile bot)
    {
        if (bot == null)
        {
            return -1;
        }

        var magery = bot.Skills[SkillName.Magery].Value;

        for (var i = Ladder.Length - 1; i >= 0; i--)
        {
            var spell = Ladder[i];
            var circle = BotGrimoire.Circle(spell);

            if (circle < 1 || circle > ManaByCircle.Length || circle > ScrollSkillByCircle.Length)
            {
                continue;
            }

            // The pool rather than what is left this instant: this is shopping, and a fight it cannot pay for
            // twice is a fight it should not be equipping for.
            if (bot.ManaMax < ManaByCircle[circle - 1] * PoolCasts)
            {
                continue;
            }

            if (magery >= ScrollSkillByCircle[circle - 1] + SkillMargin)
            {
                return spell;
            }
        }

        return -1;
    }

    /// <summary>
    /// The highest attack this bot's book holds at or below <see cref="Stock"/>'s circle, or -1 — and -1 whenever Stock is, so a
    /// bot whose Magery or pool supports no attack is asked to carry no herbs for one. What <c>BotProvision.Armed</c> asks a
    /// healer or a warrior-mage to keep the herbs of (08.10.2026, cast30 review A3): <see cref="Strongest"/> ignores the skill and
    /// the pool, and a book keeps filling, so a low-Magery healer would in time have been sent after mind blast's four herbs.
    /// </summary>
    public static int Stocked(Mobile bot)
    {
        var stock = Stock(bot);

        if (stock < 0)
        {
            return -1;
        }

        var ceiling = BotGrimoire.Circle(stock);

        for (var i = Ladder.Length - 1; i >= 0; i--)
        {
            if (BotGrimoire.Circle(Ladder[i]) <= ceiling && BotGrimoire.Holds(bot, Ladder[i]))
            {
                return Ladder[i];
            }
        }

        return -1;
    }

    /// <summary>
    /// Whether this bot has anything at all it could throw — a book with an attack in it, or a scroll.
    ///
    /// <para>
    /// <b>It used to mean "is a mage", and that is a different question.</b> A book was required before
    /// anything else was looked at, so a warrior carrying five magic arrow scrolls answered no and was
    /// treated as a bot with a stick — it never chose a spell, never stood off, and the scrolls sat in its
    /// pack until it died. Being a caster is a fact about a class; having something to throw is a fact about
    /// a pack, and only the second one decides anything here.
    /// </para>
    ///
    /// <para>
    /// Note what this deliberately does <em>not</em> change: how far the bot stands off. That is worked out
    /// from whether a spell can actually be paid for this instant — see the <c>armed</c> test in
    /// <see cref="BotSlay"/> — so a warrior down to its last scroll closes and swings like a warrior rather
    /// than keeping a mage's distance on the strength of one arrow.
    /// </para>
    /// </summary>
    public static bool Can(Mobile bot)
    {
        var booked = BotGrimoire.Book(bot) != null;

        for (var i = 0; i < Ladder.Length; i++)
        {
            if (booked && BotGrimoire.Holds(bot, Ladder[i]))
            {
                return true;
            }

            if (Scroll(bot, Ladder[i]) != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The strongest attack this bot could actually get off right now, or -1.
    ///
    /// Strongest first and downwards, so a mage with the mana for lightning throws lightning and the same mage
    /// at the end of its pool still throws magic arrow rather than reaching for a stick.
    /// </summary>
    public static int Best(Mobile bot)
    {
        for (var i = Ladder.Length - 1; i >= 0; i--)
        {
            if (Ready(bot, Ladder[i]))
            {
                return Ladder[i];
            }
        }

        return -1;
    }

    /// <summary>
    /// A scroll for this spell sitting in the bot's pack, or null.
    ///
    /// <para>
    /// <b>The whole of what makes a scroll different, and it is the engine's rule rather than one of
    /// ours.</b> <c>Spell.ConsumeReagents</c> returns true the moment a scroll is attached, and nothing
    /// anywhere asks a scroll-caster for a spellbook — so a warrior with no Magery, no book and no reagents
    /// can throw the thing in its pack, and it is spent doing it. That last part is what makes this worth
    /// building at all: every cast destroys a scroll, so the scribe who writes them has customers for as
    /// long as anybody fights.
    /// </para>
    /// </summary>
    public static SpellScroll Scroll(Mobile bot, int spell)
    {
        var pack = bot?.Backpack;
        var kind = BotGrimoire.ScrollFor(spell);

        if (pack == null || kind == null)
        {
            return null;
        }

        return pack.FindItemByType(kind) as SpellScroll;
    }

    /// <summary>
    /// Whether this bot can throw this spell right now, by book or by scroll.
    ///
    /// <para>
    /// Two routes and one of them costs nothing but the scroll: a book wants reagents in the pack, a scroll
    /// wants only the mana. Mana is asked either way, because the pool is the one thing neither route can
    /// borrow.
    /// </para>
    /// </summary>
    public static bool Ready(Mobile bot, int spell)
    {
        var scroll = Scroll(bot, spell);

        if (scroll == null && !BotGrimoire.Holds(bot, spell))
        {
            return false;
        }

        var circle = BotGrimoire.Circle(spell);

        if (circle < 1 || circle > ManaByCircle.Length || bot.Mana < ManaByCircle[circle - 1])
        {
            return false;
        }

        // From here the book is the only route that can still be refused, and it is refused on herbs.
        if (scroll != null)
        {
            return true;
        }

        var made = SpellRegistry.NewSpell(spell, bot, null);

        if (made == null)
        {
            return false;
        }

        var herbs = made.Reagents;
        var pack = bot.Backpack;

        for (var i = 0; herbs != null && i < herbs.Length; i++)
        {
            if ((pack?.GetAmount(herbs[i]) ?? 0) < 1)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Which gate closed on the whole attack ladder, in words, for whoever has to read the log.
    ///
    /// <para>
    /// <b>Written because the refusal was reported by its symptom.</b> The complaint said "best -1, 35 of 35
    /// mana, book holds 5" — the pool full, the book stocked, and no mention anywhere of the third thing
    /// <see cref="Ready"/> checks. There are exactly three ways to be refused and the answer is always one of
    /// them, so it costs nothing to say which.
    /// </para>
    ///
    /// <para>
    /// The gates are reported in the order they are checked, and the <em>least</em> excuse across the ladder
    /// is the honest one: a bot that owns no attack spell at all is in a different position from one that
    /// owns three and is out of herbs for all of them.
    /// </para>
    /// </summary>
    public static string Why(Mobile bot) => Why(bot, out _);

    /// <summary>The gate <see cref="Why"/> names, as a key for a counter: <see cref="NoSpell"/>, <see cref="NoMana"/>, <see cref="NoHerbs"/> or <see cref="Refused"/>.</summary>
    public const string NoSpell = "no attack spell";

    public const string NoMana = "no mana";

    public const string NoHerbs = "no reagents";

    public const string Refused = "refused by the engine";

    /// <summary>
    /// <see cref="Why"/>, and which of the four gates it was.
    ///
    /// <para>
    /// <b>The third gate was never asked, and it was blamed for the fourth (08.10.2026, cast30).</b> This read "is out of reagents
    /// for every one" for anything that got past the book and the pool, without looking at a single herb — and the complaint it
    /// fills in prints <see cref="Best"/> beside it. On 07.10.2026 from 22:20 to 01:24, 80 of the 82 "could not throw a spell"
    /// lines in BotSlay read "out of reagents … best 17" (or 29, 11, 4): <see cref="Ready"/> had found the herbs, and the
    /// refusal was the engine's at <c>Spell.Cast</c> — in the hunt's own run of it a fireball comes off the recovery
    /// (<c>NextSpellTime</c>, cast delay and 0.75 s) half a second after <see cref="CastMs"/> lets the next one be asked for,
    /// and Meliora, Jocelyn and Annora went on to throw 2 and 3 spells at the same quarry. Now the herbs are counted, a scroll
    /// counts as an owned spell (it needs no herbs), and what is left when all three are open is the engine's refusal, named.
    /// </para>
    /// </summary>
    public static string Why(Mobile bot, out string gate)
    {
        gate = NoSpell;

        if (bot == null)
        {
            return "no body";
        }

        var owned = 0;
        var affordable = 0;
        var stocked = 0;
        Type lacked = null;

        // The cheapest spell held that is blocked, and by what: a pool short of a high circle while a low one lacks only its herb
        // is a bot out of herbs, not out of mana (cast30 review B F5). Ladder order is circle order, so the first blocked is the cheapest.
        var cheapest = (string)null;

        for (var i = 0; i < Ladder.Length; i++)
        {
            var spell = Ladder[i];
            var scroll = Scroll(bot, spell) != null;

            if (!scroll && !BotGrimoire.Holds(bot, spell))
            {
                continue;
            }

            owned++;

            var circle = BotGrimoire.Circle(spell);

            if (circle < 1 || circle > ManaByCircle.Length)
            {
                continue;
            }

            if (bot.Mana < ManaByCircle[circle - 1])
            {
                cheapest ??= NoMana;

                continue;
            }

            affordable++;

            if (scroll)
            {
                stocked++;

                continue;
            }

            // A spell the registry cannot build is not cast by Ready either: not stocked, and not a herb short (cast30 review A6).
            var herbs = SpellRegistry.NewSpell(spell, bot, null)?.Reagents;

            if (herbs == null)
            {
                continue;
            }

            var lacking = (Type)null;

            for (var h = 0; h < herbs.Length; h++)
            {
                if ((bot.Backpack?.GetAmount(herbs[h]) ?? 0) < 1)
                {
                    lacking = herbs[h];

                    break;
                }
            }

            if (lacking == null)
            {
                stocked++;
            }
            else
            {
                lacked ??= lacking;
                cheapest ??= NoHerbs;
            }
        }

        if (owned == 0)
        {
            return $"its book holds none of the {Ladder.Length} attack spells and it carries no scroll of one";
        }

        if (stocked == 0 && (affordable == 0 || cheapest == NoMana))
        {
            gate = NoMana;

            return affordable == 0
                ? $"it owns {owned} attack spells and has the mana for none of them ({bot.Mana} of {bot.ManaMax})"
                : $"it owns {owned} attack spells, lacks the mana for the cheapest ({bot.Mana} of {bot.ManaMax}) and the herbs for the rest (no {lacked?.Name ?? "herbs"})";
        }

        if (stocked == 0)
        {
            gate = NoHerbs;

            return $"it owns {owned} attack spells, can pay for {affordable}, and is out of reagents for every one (no {lacked?.Name ?? "herbs"})";
        }

        gate = Refused;

        if (bot.Spell != null)
        {
            return $"it has the herbs and the mana for {stocked}, and a spell of its own is still going up";
        }

        if (bot.Paralyzed || bot.Frozen)
        {
            return $"it has the herbs and the mana for {stocked}, and is held fast";
        }

        var recovering = bot.NextSpellTime - Core.TickCount;

        if (recovering > 0)
        {
            return $"it has the herbs and the mana for {stocked}, and is still recovering from its last spell ({recovering}ms)";
        }

        return $"it has the herbs and the mana for {stocked}, and the engine would not begin the cast";
    }

    /// <summary>
    /// Begins a cast. Returns whether it started; what it is aimed at is settled on a later beat.
    ///
    /// Two beats, because that is how the engine casts: the cast starts a delay, the delay puts a target on
    /// the caster, and somebody has to fill that target in. A bot has no client to click with, so
    /// <see cref="Aim"/> is the click.
    /// </summary>
    public static bool Begin(Mobile bot, int spell)
    {
        if (bot == null || bot.Spell != null)
        {
            return false;
        }

        // The scroll is handed to the spell, not merely checked for. That reference is what tells the engine
        // to skip the reagents and to spend the scroll when the cast lands — passing null here with a scroll
        // in the pack would ask for herbs the bot has not got and fail for a reason nothing would report.
        var made = SpellRegistry.NewSpell(spell, bot, Scroll(bot, spell));

        return made != null && made.Cast();
    }

    /// <summary>Points a finished cast at the quarry. Returns whether there was one to point.</summary>
    public static bool Aim(Mobile bot, Mobile at)
    {
        var aiming = bot?.Target;

        if (aiming == null || at == null || at.Deleted)
        {
            return false;
        }

        aiming.Invoke(bot, at);

        return true;
    }
}
