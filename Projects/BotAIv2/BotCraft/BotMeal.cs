using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A bot eating a cooked meal: what it does to the bot, and for how long.
///
/// <para>
/// <b>Patrick's order of 05.09.2026 — cooking lifts a bot's mood and quickens what it recovers.</b> Both
/// halves land here rather than in the cooking, because the cook is not the one who benefits: a trade whose
/// reward went to whoever made the thing would be a trade with no reason to sell any. The cook is paid in
/// coin like every other crafter on this island; the meal is paid for by whoever eats it.
/// </para>
///
/// <para>
/// <b>Regeneration is reached through the engine's own hooks, and this era leaves two of the three
/// empty.</b> <c>Mobile.HitsRegenRateHandler</c>, <c>StamRegenRateHandler</c> and
/// <c>ManaRegenRateHandler</c> are static entry points that content fills in; <c>RegenRates.Configure</c>
/// fills the mana one always and the other two <em>only under AOS</em>, and this shard is Renaissance. So
/// health and stamina were running at the flat defaults with nothing consulted at all, and mana had one
/// handler. Whatever was there is kept and called: this wraps rather than replaces, so a meal is the only
/// thing that changes and every mobile that has not eaten gets exactly the rate it got before.
/// </para>
///
/// <para>
/// <b>Ten minutes, by order, and kept per bot rather than on the item.</b> An effect written onto the food
/// would have to survive being sold, dropped, looted and re-listed; an effect written against the eater is a
/// stamp and a lookup, and it dies with the bot the way every other fact about a bot does.
/// </para>
/// </summary>
public static class BotMeal
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMeal));

    /// <summary>How long a meal lasts. Ten minutes, by order.</summary>
    public static int LastsMs { get; set; } = 600000;

    /// <summary>
    /// What a meal does to the interval between two ticks of recovery. Half.
    ///
    /// <para>
    /// The engine's numbers are intervals, not rates — eleven seconds a hit point, seven a point of stamina
    /// and of mana — so the quickening is a smaller number. A half is a doubling of what a bot recovers, and
    /// it is chosen to be plainly worth walking to a market for without making a fed bot a different kind of
    /// creature from a hungry one.
    /// </para>
    /// </summary>
    public static double Quickening { get; set; } = 0.5;

    /// <summary>
    /// What eating is worth to a bot's contentment, in the coin the urges are measured in.
    ///
    /// See <c>BotUrges.Paid</c>: relief is reckoned per hundred of takings, so this is the mood a meal is
    /// worth said in the only language that layer speaks. Deliberately small — a supper is a comfort, not an
    /// afternoon's work — and its whole job is that a bot with nothing to do and food in its pack has one
    /// thing it can do about the mood rather than nought.
    /// </para>
    /// </summary>
    public static double Cheer { get; set; } = 40.0;

    /// <summary>Meals eaten. For the summary.</summary>
    public static long Eaten { get; private set; }

    /// <summary>Bots that were already fed when they looked. For the summary.</summary>
    public static long Fed { get; private set; }

    /// <summary>Bots with no meal on them at all. For the summary.</summary>
    public static long Empty { get; private set; }

    /// <summary>Bots the ratchet was wound back for. See the note in <see cref="Keep"/>.</summary>
    public static long Emptied { get; private set; }

    /// <summary>Meals the engine refused for a reason that is not fullness. Named rather than silent.</summary>
    public static long Refused { get; private set; }

    private static readonly Dictionary<Serial, long> _until = new();

    /// <summary>Whether this bot is inside the ten minutes after a meal.</summary>
    public static bool IsFed(Mobile body)
    {
        if (body == null || !_until.TryGetValue(body.Serial, out var until))
        {
            return false;
        }

        // By subtraction against a stamp that was itself a real tick, never against a nought default.
        if (Core.TickCount - until < 0)
        {
            return true;
        }

        _until.Remove(body.Serial);

        return false;
    }

    /// <summary>
    /// Eats one cooked meal out of the pack, if there is one and the last has worn off.
    ///
    /// <para>
    /// A condition rather than an errand, like banking and dressing: eating takes no journey and no time a
    /// bot could have spent working, and an errand that scored against the auction would lose to everything
    /// and never happen. See <c>BotMobile</c>, where the rest of that family is called.
    /// </para>
    /// </summary>
    public static void Keep(IBotWilful bot)
    {
        var body = bot?.Self;
        var pack = body?.Backpack;

        if (body is not { Deleted: false, Alive: true } || pack == null)
        {
            return;
        }

        if (IsFed(body))
        {
            Fed++;

            return;
        }

        Item meal = null;

        for (var i = 0; i < pack.Items.Count; i++)
        {
            var item = pack.Items[i];

            if (item is { Deleted: false, Movable: true } && BotOven.IsMeal(item.GetType()))
            {
                meal = item;

                break;
            }
        }

        if (meal == null)
        {
            Empty++;

            return;
        }

        // <b>Nothing on this shard ever takes hunger back, so the engine's meter is a one-way ratchet.</b>
        // <c>Mobile.Hunger</c> is written in exactly one place — <c>Food.FillHunger</c>, which adds to it and
        // refuses at twenty — and there is no timer anywhere in this fork that subtracts any of it. A supper
        // fills three to five, so a bot could eat five of them in its whole life and then be too full for
        // ever. And the refusal is silent: FillHunger answers a player with a line on their screen, and a bot
        // has no screen. The trade ordered on 05.09.2026 would have worked for an hour and then stopped, with
        // nothing anywhere on the shard saying why.
        //
        // The ten minutes are the clock. Execution only reaches here when the last meal has worn off, and a
        // bot whose meal has worn off is hungry — that is what wearing off means. So the meter is wound back
        // rather than a second hunger timer being invented to sit beside this one.
        if (body.Hunger >= 20)
        {
            body.Hunger = 0;
            Emptied++;
        }

        // The engine's own eating, so the sound and the consuming of the stack are its business and not this
        // file's. Anything it still refuses is counted: with fullness handled above there is no known reason
        // left for it to say no, which is exactly when a silent return would cost the most.
        if (meal is not Food food)
        {
            Refused++;

            return;
        }

        if (!food.Eat(body))
        {
            Refused++;

            return;
        }

        _until[body.Serial] = Core.TickCount + LastsMs;
        Eaten++;

        bot.Resolve?.Urges?.Paid(Cheer);

        // Said once a meal, at information, because a population of fifty eating every ten minutes is three
        // hundred lines an hour and this shard has twice had to stop writing a log like that. It is worth the
        // three hundred: what a bot ate and when is the only evidence that the trade below it is working.
        logger.Information(
            "{Name} ate {Meal} and will recover twice as fast for {Minutes} minutes",
            body.Name,
            meal.GetType().Name,
            LastsMs / 60000
        );
    }

    /// <summary>
    /// Installs the quickening on the engine's regeneration hooks, keeping whatever was there.
    ///
    /// <para>
    /// Called from <c>BotCore.Configure</c>, which runs after <c>RegenRates.Configure</c> — that one carries
    /// <c>CallPriority(10)</c> and this assembly has none, so the ordering is the loader's own and not a
    /// coincidence to be relied on quietly. If it ever inverts, the effect is that the handler captured below
    /// is null and the defaults are used, which is exactly what an unfed bot gets: wrong ordering makes the
    /// meal do nothing rather than makes anything break.
    /// </para>
    /// </summary>
    public static void Configure()
    {
        var hits = Mobile.HitsRegenRateHandler;
        var stam = Mobile.StamRegenRateHandler;
        var mana = Mobile.ManaRegenRateHandler;

        Mobile.HitsRegenRateHandler = m => Quicken(m, hits?.Invoke(m) ?? Mobile.DefaultHitsRate);
        Mobile.StamRegenRateHandler = m => Quicken(m, stam?.Invoke(m) ?? Mobile.DefaultStamRate);
        Mobile.ManaRegenRateHandler = m => Quicken(m, mana?.Invoke(m) ?? Mobile.DefaultManaRate);
    }

    /// <summary>The rate a fed bot recovers at. Everything else gets back exactly what it came in with.</summary>
    private static TimeSpan Quicken(Mobile m, TimeSpan rate) =>
        m is BotMobile && IsFed(m) ? TimeSpan.FromMilliseconds(rate.TotalMilliseconds * Quickening) : rate;

    public static string Describe() =>
        Eaten + Fed + Empty == 0
            ? "nobody has been offered a meal yet"
            : $"{Eaten} meals eaten, {Fed} looks found a bot still fed from the last one, {Empty} had nothing cooked on them, "
              + $"{Emptied} were too full until their last meal wore off, {Refused} were refused by the engine for something else";

    /// <summary>Forgotten with the world, like every store in this assembly that is keyed by serial.</summary>
    public static void Forget()
    {
        _until.Clear();
        Eaten = 0;
        Fed = 0;
        Empty = 0;
        Emptied = 0;
        Refused = 0;
    }
}
